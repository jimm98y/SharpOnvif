using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpOnvifCommon.Xml;
using SharpOnvifServer.Events;
using SharpOnvifServer.Security;

namespace SharpOnvifServer.Dispatch
{
    /// <summary>
    /// One Onvif URL, and the services reachable through it. Several services can share an
    /// endpoint: the action decides which one handles a request.
    /// </summary>
    public sealed class OnvifEndpoint
    {
        private readonly List<Registration> _services = new List<Registration>();

        public OnvifEndpoint(string path)
        {
            Path = path;
        }

        public string Path { get; private set; }

        /// <summary>
        /// Route value carrying the trailing segment of a subscription manager address. Onvif
        /// hands a client a reference like <c>/onvif/Events/PullPointSubscription/3/</c>, and the
        /// segment identifies which subscription the request belongs to.
        /// </summary>
        internal const string SubscriptionRouteValue = "onvifSubscriptionId";

        /// <summary>
        /// The largest request this endpoint reads. Onvif requests are small - the largest real
        /// one is a configuration being written - and Kestrel's own default of 30 MB is a great
        /// deal of XML to hold as a string and parse more than once.
        /// </summary>
        public static int MaxRequestBytes { get; set; } = 2 * 1024 * 1024;

        private sealed class Registration
        {
            public OnvifServiceDispatcher Dispatcher;
            public Type ImplementationType;
        }

        /// <summary>Adds a service implementation to this endpoint.</summary>
        public OnvifEndpoint Add(OnvifServiceDispatcher dispatcher, Type implementationType)
        {
            _services.Add(new Registration { Dispatcher = dispatcher, ImplementationType = implementationType });
            return this;
        }

        /// <summary>Handles one SOAP request.</summary>
        public async Task HandleAsync(HttpContext context)
        {
            var logger = context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger<OnvifEndpoint>();

            if (!await IsAuthorizedAsync(context).ConfigureAwait(false)) return;

            string envelope;
            try
            {
                envelope = await ReadEnvelopeAsync(context).ConfigureAwait(false);
            }
            catch (InvalidDataException)
            {
                // Refused before it was read, so the size of the request is not also the size of
                // what answering it costs.
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await WriteFaultAsync(context, "Sender", "WellFormed",
                    "The request is larger than this endpoint accepts.",
                    OnvifErrors.Namespace, System.Net.HttpStatusCode.RequestEntityTooLarge)
                    .ConfigureAwait(false);
                return;
            }

            string action = ActionFromContentType(context.Request.ContentType);

            // Implementations read this to build the absolute URIs Onvif responses carry.
            OnvifOperationContext.Set(context);

            CaptureSubscriptionId(context);

            try
            {
                // The body is read twice at most: once to find the action when the client did not
                // put it in the Content-Type header, and once to deserialise.
                if (string.IsNullOrEmpty(action)) action = ActionFromEnvelope(envelope);

                Registration registration = null;
                if (!string.IsNullOrEmpty(action))
                {
                    registration = _services.FirstOrDefault(r => r.Dispatcher.CanHandle(action));
                }

                if (registration == null)
                {
                    // Last resort: identify the operation from the body element itself, which is
                    // what a client that sends neither form of action leaves us.
                    registration = ResolveFromBody(envelope, ref action);
                }

                if (registration == null)
                {
                    await WriteFaultAsync(context, "Sender", "ActionNotSupported",
                        string.IsNullOrEmpty(action)
                            ? "The request does not name an Onvif operation."
                            : "The action '" + UntrustedText.Printable(action) + "' is not supported at this endpoint.")
                        .ConfigureAwait(false);
                    return;
                }

                object service = context.RequestServices.GetService(registration.ImplementationType);
                if (service == null)
                {
                    throw new InvalidOperationException(
                        "No instance of " + registration.ImplementationType.FullName +
                        " is registered. Add it to the service collection before mapping the endpoint.");
                }

                OnvifDispatchResult result;
                using (XmlReader xml = SoapEnvelope.CreateReader(new StringReader(envelope)))
                {
                    if (!SoapEnvelope.MoveToBody(xml))
                    {
                        await WriteFaultAsync(context, "Sender", "WellFormed", "The SOAP body is empty.")
                            .ConfigureAwait(false);
                        return;
                    }

                    result = await registration.Dispatcher
                        .InvokeAsync(service, action, xml, context.RequestAborted)
                        .ConfigureAwait(false);
                }

                string reply = SoapEnvelope.Write(null, writer =>
                {
                    if (result.ElementName == null) return;
                    writer.WriteStartElement(result.Namespace, result.ElementName);
                    writer.WriteContent(result.Response);
                    writer.WriteEndElement();
                });

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = SoapEnvelope.ContentType + "; charset=utf-8";

                // Proves to the client that this device knows the password as well, which is the
                // half of HTTP Digest that authenticates the device. Written before the body,
                // because a header cannot follow one.
                await DigestAuthenticationInfo.AppendAsync(context, reply).ConfigureAwait(false);

                await context.Response.WriteAsync(reply, Encoding.UTF8).ConfigureAwait(false);
            }
            catch (NotImplementedException)
            {
                // The service does not implement this operation. Onvif has a specific fault for
                // it, and reporting it accurately lets a client fall back rather than give up.
                await WriteFaultAsync(context, "Receiver", "ActionNotSupported",
                    "The device does not implement '" + UntrustedText.Printable(action) + "'.").ConfigureAwait(false);
            }
            catch (OnvifServerFaultException fault)
            {
                await WriteFaultAsync(context, fault.Fault.Code, fault.Fault.Subcode,
                    fault.Fault.Reason, fault.SubcodeNamespace, fault.StatusCode).ConfigureAwait(false);
            }
            catch (OnvifFaultException fault)
            {
                await WriteFaultAsync(context, "Sender",
                    fault.Fault?.Subcode ?? "InvalidArgVal", fault.Message).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                // The action is whatever the caller put in the request, so it is rendered as one
                // line of printable text before it is logged. A newline in it would otherwise
                // begin what reads as a new log entry.
                logger?.LogError(error, "Onvif operation {Action} failed.", UntrustedText.Printable(action));
                await WriteFaultAsync(context, "Receiver", "Action", error.Message,
                    OnvifErrors.Namespace, System.Net.HttpStatusCode.InternalServerError).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reads the request body, refusing one larger than <see cref="MaxRequestBytes"/>.
        /// </summary>
        /// <remarks>
        /// The envelope is held as a string and read more than once - to find the action, then to
        /// deserialise - so its size is paid for several times over. A caller must not be able to
        /// choose how much that is.
        /// </remarks>
        private async Task<string> ReadEnvelopeAsync(HttpContext context)
        {
            long? declared = context.Request.ContentLength;
            if (declared.HasValue && declared.Value > MaxRequestBytes)
                throw new InvalidDataException("The request declares more than this endpoint accepts.");

            var buffer = new MemoryStream(
                declared.HasValue ? (int)Math.Min(declared.Value, 64 * 1024) : 4096);

            byte[] chunk = new byte[8192];
            long total = 0;

            while (true)
            {
                int read = await context.Request.Body
                    .ReadAsync(chunk, 0, chunk.Length, context.RequestAborted).ConfigureAwait(false);

                if (read == 0) break;

                total += read;
                // A chunked request declares no length, so the limit is enforced as it arrives.
                if (total > MaxRequestBytes)
                    throw new InvalidDataException("The request is larger than this endpoint accepts.");

                buffer.Write(chunk, 0, read);
            }

            return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
        }

        /// <summary>
        /// Publishes the subscription segment of the address, when the request came in on the
        /// subscription form of the route, so an implementation can tell which subscription a
        /// request belongs to.
        /// </summary>
        private static void CaptureSubscriptionId(HttpContext context)
        {
            object raw = context.GetRouteValue(SubscriptionRouteValue);
            if (raw == null) return;

            // Published as it arrived, less the slashes Onvif puts around it. The subscription
            // manager decides what an ID means; this only carries it.
            string subscriptionId = raw.ToString().Trim('/');
            if (subscriptionId.Length > 0)
                context.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID] = subscriptionId;
        }

        /// <summary>
        /// Refuses the request unless it is authenticated, when Onvif digest authentication is
        /// configured. The authentication handler admits the PRE_AUTH operations the Onvif core
        /// specification lists as an anonymous user, so those still get through.
        /// <para>
        /// When no Onvif authentication scheme is registered the endpoint is open, which is what a
        /// host that never called AddOnvifDigestAuthentication asked for.
        /// </para>
        /// </summary>
        private static async Task<bool> IsAuthorizedAsync(HttpContext context)
        {
            var schemes = context.RequestServices.GetService<IAuthenticationSchemeProvider>();
            if (schemes == null) return true;

            var scheme = await schemes.GetSchemeAsync(OnvifAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            if (scheme == null) return true;

            if (context.User != null && context.User.Identity != null && context.User.Identity.IsAuthenticated)
                return true;

            // Produces the WWW-Authenticate challenge the client needs in order to try again.
            await context.ChallengeAsync(OnvifAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
            return false;
        }

        private Registration ResolveFromBody(string envelope, ref string action)
        {
            using (XmlReader xml = SoapEnvelope.CreateReader(new StringReader(envelope)))
            {
                if (!SoapEnvelope.MoveToBody(xml)) return null;

                foreach (Registration registration in _services)
                {
                    if (registration.Dispatcher.TryResolveAction(xml.NamespaceURI, xml.LocalName, out string resolved))
                    {
                        action = resolved;
                        return registration;
                    }
                }
            }

            return null;
        }

        /// <summary>Reads the action from the Content-Type header's action parameter.</summary>
        private static string ActionFromContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return null;

            int start = contentType.IndexOf("action=", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return null;

            string value = contentType.Substring(start + "action=".Length).Trim();

            // Devices and tools quote this inconsistently, with single or double quotes or none.
            int end = value.IndexOf(';');
            if (end >= 0) value = value.Substring(0, end);
            return value.Trim().Trim('"', '\'');
        }

        /// <summary>
        /// Reads a wsa:Action header out of the envelope. Onvif Device Manager sends the action
        /// this way for event subscriptions rather than in the Content-Type header.
        /// </summary>
        private static string ActionFromEnvelope(string envelope)
        {
            try
            {
                using (XmlReader xml = SoapEnvelope.CreateReader(new StringReader(envelope)))
                {
                    if (!SoapEnvelope.MoveToEnvelopeChild(xml, "Header")) return null;

                    int headerDepth = xml.Depth;
                    while (xml.Read())
                    {
                        if (xml.NodeType == XmlNodeType.EndElement && xml.Depth == headerDepth) return null;
                        if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "Action")
                            return xml.ReadElementContentAsString().Trim();
                    }
                }
            }
            catch (XmlException)
            {
                // Malformed envelopes are reported by the deserialisation path, which produces a
                // better message than anything this method could.
            }

            return null;
        }

        private static Task WriteFaultAsync(HttpContext context, string code, string subcode, string reason)
        {
            return WriteFaultAsync(context, code, subcode, reason, OnvifErrors.Namespace,
                System.Net.HttpStatusCode.BadRequest);
        }

        private static async Task WriteFaultAsync(
            HttpContext context, string code, string subcode, string reason,
            string subcodeNamespace, System.Net.HttpStatusCode statusCode)
        {
            string envelope = SoapEnvelope.Write(null, writer =>
            {
                XmlWriter xml = writer.Xml;
                xml.WriteStartElement("SOAP-ENV", "Fault", OnvifXmlNamespaces.SoapEnvelope);

                xml.WriteStartElement("SOAP-ENV", "Code", OnvifXmlNamespaces.SoapEnvelope);
                xml.WriteElementString("SOAP-ENV", "Value", OnvifXmlNamespaces.SoapEnvelope, "SOAP-ENV:" + code);
                xml.WriteStartElement("SOAP-ENV", "Subcode", OnvifXmlNamespaces.SoapEnvelope);
                xml.WriteStartElement("SOAP-ENV", "Value", OnvifXmlNamespaces.SoapEnvelope);
                xml.WriteAttributeString("xmlns", "ter", OnvifXmlNamespaces.Xmlns, subcodeNamespace);
                xml.WriteString("ter:" + subcode);
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteEndElement();

                xml.WriteStartElement("SOAP-ENV", "Reason", OnvifXmlNamespaces.SoapEnvelope);
                xml.WriteStartElement("SOAP-ENV", "Text", OnvifXmlNamespaces.SoapEnvelope);
                xml.WriteAttributeString("lang", OnvifXmlNamespaces.Xml, "en");
                xml.WriteString(reason ?? string.Empty);
                xml.WriteEndElement();
                xml.WriteEndElement();

                xml.WriteEndElement();
            });

            // The Onvif core specification answers a fault with 400 for a sender error and 500
            // for a receiver one; clients read the envelope either way.
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = SoapEnvelope.ContentType + "; charset=utf-8";
            await context.Response.WriteAsync(envelope, Encoding.UTF8).ConfigureAwait(false);
        }
    }
}
