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
using Microsoft.Extensions.Options;
using SharpOnvifCommon.Security;
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
            public ServiceDispatcher Dispatcher;
            public Type ImplementationType;
        }

        /// <summary>Adds a service implementation to this endpoint.</summary>
        public OnvifEndpoint Add(ServiceDispatcher dispatcher, Type implementationType)
        {
            _services.Add(new Registration { Dispatcher = dispatcher, ImplementationType = implementationType });
            return this;
        }

        /// <summary>Handles one SOAP request.</summary>
        public async Task HandleAsync(HttpContext context)
        {
            ILogger logger = Logger(context);

            if (!await IsAuthorizedAsync(context).ConfigureAwait(false)) return;

            byte[] body;
            try
            {
                // Kept from authentication when it had to read the body, so it is read once.
                body = await OnvifRequestBody.ReadAsync(context).ConfigureAwait(false);
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

            // The client went away before sending all of it.
            if (body == null) return;

            string action = OnvifRequestAction.FromContentType(context.Request.ContentType);

            // Implementations read this to build the absolute URIs Onvif responses carry.
            OnvifOperationContext.Set(context);

            CaptureSubscriptionId(context);

            try
            {
                DispatchResult result;

                // One reader for the whole request. It passes the Header on the way to the Body,
                // which is where the action is when the Content-Type did not carry one, and goes
                // on to deserialise the Body it stopped at.
                using (XmlReader xml = SoapEnvelope.CreateReader(new MemoryStream(body, false)))
                {
                    bool hasBody = OnvifRequestAction.ReadToBody(xml, out string headerAction);
                    if (string.IsNullOrEmpty(action)) action = headerAction;

                    Registration registration = string.IsNullOrEmpty(action) ? null : Find(action);

                    // The body element names the operation too, and where the two disagree the
                    // body is the one that describes what was sent. 0.9.x sent the DeviceIO
                    // operations with the device service's actions, which on an address hosting
                    // both services named the wrong one. It also covers a client that sends no
                    // action at all.
                    //
                    // Authentication admitted the request for its action, though, and an action no
                    // service here handles is no exception: a PRE_AUTH device action sent to an
                    // address that hosts only Media is exactly how a body that needs a password
                    // would get past it.
                    if (hasBody)
                    {
                        Registration bodyRegistration = FindByBody(xml.NamespaceURI, xml.LocalName, out string bodyAction);
                        if (bodyRegistration != null && bodyAction != action && MayRunInstead(context, bodyAction))
                        {
                            registration = bodyRegistration;
                            action = bodyAction;
                        }
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

                    if (!hasBody)
                    {
                        await WriteFaultAsync(context, "Sender", "WellFormed", "The SOAP body is empty.")
                            .ConfigureAwait(false);
                        return;
                    }

                    result = await registration.Dispatcher
                        .InvokeAsync(service, action, xml, context.RequestAborted)
                        .ConfigureAwait(false);
                }

                string reply = SoapEnvelope.Write(OnvifXmlNamespaces.EnvelopePrologue, null, writer =>
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
                await WriteFaultAsync(context, fault.Fault.Code, fault.Fault.Subcodes,
                    fault.Fault.Reason, fault.SubcodeNamespace, fault.StatusCode).ConfigureAwait(false);
            }
            catch (XmlException)
            {
                // What the caller sent is not XML. Their mistake rather than ours, and not worth an
                // error in the log for every request someone malforms on purpose.
                await WriteFaultAsync(context, "Sender", "WellFormed", "The request is not well-formed XML.")
                    .ConfigureAwait(false);
            }
            catch (SoapFaultException fault)
            {
                IList<string> subcodes = fault.Fault != null && fault.Fault.Subcodes.Count > 0
                    ? fault.Fault.Subcodes
                    : new[] { "InvalidArgVal" };
                await WriteFaultAsync(context, "Sender", subcodes, fault.Message, OnvifErrors.Namespace,
                    System.Net.HttpStatusCode.BadRequest).ConfigureAwait(false);
            }
            catch (Exception error)
            {
                // The action is whatever the caller put in the request, so it is rendered as one
                // line of printable text before it is logged. A newline in it would otherwise
                // begin what reads as a new log entry.
                logger?.LogError(error, "Onvif operation {Action} failed.", UntrustedText.Printable(action));

                // The exception stays in the log. Its message is written for whoever maintains the
                // device - a path, a connection string, a type name - and not for whoever sent
                // the request. A fault the implementation meant a caller to read is an
                // OnvifServerFaultException, answered above with its own reason.
                await WriteFaultAsync(context, "Receiver", "Action", "The device could not complete the operation.",
                    OnvifErrors.Namespace, System.Net.HttpStatusCode.InternalServerError).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The logger, created on the first request that needs one rather than on every request.
        /// </summary>
        private ILogger Logger(HttpContext context)
        {
            return _logger ??= context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger<OnvifEndpoint>();
        }

        private ILogger _logger;

        /// <summary>The service that handles an action, if one here does.</summary>
        private Registration Find(string action)
        {
            foreach (Registration registration in _services)
            {
                if (registration.Dispatcher.CanHandle(action)) return registration;
            }
            return null;
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

        /// <summary>
        /// Whether the caller may run the operation the body names in place of the one its action
        /// names. Authentication admitted the request for the action, and an action the
        /// specification lets anyone call must not carry in a body that needs a password.
        /// </summary>
        private static bool MayRunInstead(HttpContext context, string bodyAction)
        {
            var identity = context.User?.Identity;
            if (identity == null || !identity.IsAuthenticated) return true;   // no Onvif authentication here
            if (identity.Name != DigestAuthenticationHandler.ANONYMOUS_USER) return true;   // a real user

            var options = context.RequestServices.GetService<IOptionsMonitor<DigestAuthenticationSchemeOptions>>()?
                .Get(OnvifAuthenticationDefaults.AuthenticationScheme);
            if (options == null) return false;

            return options.Onvif.Authentication == DigestAuthentication.None || options.Onvif.IsPreAuth(bodyAction);
        }

        /// <summary>The service whose operation the body element is, and that operation's action.</summary>
        private Registration FindByBody(string ns, string localName, out string action)
        {
            foreach (Registration registration in _services)
            {
                if (registration.Dispatcher.TryResolveAction(ns, localName, out action)) return registration;
            }

            action = null;
            return null;
        }

        private static Task WriteFaultAsync(HttpContext context, string code, string subcode, string reason)
        {
            return WriteFaultAsync(context, code, new[] { subcode }, reason, OnvifErrors.Namespace,
                System.Net.HttpStatusCode.BadRequest);
        }

        private static Task WriteFaultAsync(
            HttpContext context, string code, string subcode, string reason,
            string subcodeNamespace, System.Net.HttpStatusCode statusCode)
        {
            return WriteFaultAsync(context, code, new[] { subcode }, reason, subcodeNamespace, statusCode);
        }

        /// <param name="subcodes">
        /// The subcode chain, outermost first. Each one after the first is nested in the Subcode
        /// before it, the way SOAP 1.2 refines a fault: ter:InvalidArgVal, then ter:NoProfile.
        /// </param>
        private static async Task WriteFaultAsync(
            HttpContext context, string code, IList<string> subcodes, string reason,
            string subcodeNamespace, System.Net.HttpStatusCode statusCode)
        {
            string envelope = SoapEnvelope.Write(OnvifXmlNamespaces.EnvelopePrologue, null, writer =>
            {
                XmlWriter xml = writer.Xml;
                xml.WriteStartElement("SOAP-ENV", "Fault", OnvifXmlNamespaces.SoapEnvelope);

                xml.WriteStartElement("SOAP-ENV", "Code", OnvifXmlNamespaces.SoapEnvelope);
                xml.WriteElementString("SOAP-ENV", "Value", OnvifXmlNamespaces.SoapEnvelope, "SOAP-ENV:" + code);

                int open = 0;
                foreach (string subcode in subcodes)
                {
                    if (string.IsNullOrEmpty(subcode)) continue;

                    xml.WriteStartElement("SOAP-ENV", "Subcode", OnvifXmlNamespaces.SoapEnvelope);
                    // Declared on the outermost Subcode, so that it is in scope for every nested
                    // Value as well.
                    if (open == 0) xml.WriteAttributeString("xmlns", "ter", OnvifXmlNamespaces.Xmlns, subcodeNamespace);
                    xml.WriteElementString("SOAP-ENV", "Value", OnvifXmlNamespaces.SoapEnvelope, "ter:" + subcode);
                    open++;
                }
                for (; open > 0; open--) xml.WriteEndElement();

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
