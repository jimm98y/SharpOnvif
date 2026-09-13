using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using __RUNTIME__.Xml;

namespace __RUNTIME__.Soap
{
    /// <summary>
    /// Base class of the generated service clients. Sends a document/literal SOAP 1.2 request over
    /// <see cref="HttpClient"/> and reads the body of the reply.
    /// <para>
    /// One client owns one endpoint and one connection's worth of authentication state, so reuse
    /// it: a challenge is answered once and the answer reused for every later call.
    /// </para>
    /// </summary>
    public abstract class SoapClientBase : IDisposable
    {
        private readonly HttpClient _http;
        private readonly bool _ownsHttpClient;
        private readonly IClientSettings _settings;
        private readonly IMessageCodec _codec;
        private bool _disposed;

        protected SoapClientBase(string endpointUri, IClientSettings settings)
        {
            if (string.IsNullOrWhiteSpace(endpointUri))
                throw new ArgumentNullException(nameof(endpointUri));

            if (!endpointUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !endpointUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The Onvif endpoint must be an http:// or https:// URI.", nameof(endpointUri));
            }

            if (settings == null) throw new ArgumentNullException(nameof(settings));

            if (settings.Codec == null)
                throw new ArgumentException("The settings carry nothing to write a message with.", nameof(settings));

            EndpointUri = endpointUri;
            _settings = settings;
            _codec = settings.Codec;

            if (_settings.HttpClient != null)
            {
                // Answering a challenge takes a handler in the pipeline, which cannot be added to
                // a client that is already built. Refusing is the only honest option: the
                // alternative is sending every request unauthenticated without saying so.
                if (_settings.Authentication != null && _settings.Authentication.RequiresOwnTransport(_settings))
                {
                    throw new InvalidOperationException(
                        "HttpClient cannot be combined with an authentication scheme that answers a challenge, " +
                        "because the handler that answers it has to sit in the client's pipeline. Set Transport " +
                        "instead of HttpClient, or put that handler in your own pipeline and leave the scheme out " +
                        "of Authentication.");
                }

                _http = _settings.HttpClient;
                _ownsHttpClient = false;
            }
            else
            {
                _http = CreateHttpClient(_settings);
                _ownsHttpClient = true;
            }
        }

        /// <summary>The device endpoint this client sends to.</summary>
        public string EndpointUri { get; private set; }

        /// <summary>The settings the client was created with.</summary>
        protected IClientSettings Settings { get { return _settings; } }

        /// <summary>Where this client reports what it could not do, or null for nowhere.</summary>
        protected ILog Logger { get { return _settings.Logger; } }

        private static HttpClient CreateHttpClient(IClientSettings settings)
        {
            HttpMessageHandler transport = settings.Transport ?? new HttpClientHandler();

            if (settings.Authentication != null)
                transport = settings.Authentication.CreateTransport(transport, settings) ?? transport;

            var client = new HttpClient(transport, disposeHandler: true);
            client.Timeout = settings.Timeout;
            if (settings.MaxResponseContentBytes > 0)
                client.MaxResponseContentBufferSize = settings.MaxResponseContentBytes;

            return client;
        }

        /// <summary>
        /// Resolves an xsi:type to an instance. Overridden by the generated client for its own
        /// assembly, because each carries its own copy of the shared schema types.
        /// </summary>
        protected virtual OnvifContract ResolveXmlType(string ns, string name)
        {
            return null;
        }

        /// <summary>
        /// Sends an operation and reads its reply into <typeparamref name="TResponse"/>.
        /// </summary>
        /// <param name="action">SOAP action, sent in the Content-Type and as a wsa:Action header.</param>
        /// <param name="bodyNamespace">Namespace of the request's body element.</param>
        /// <param name="bodyElement">Local name of the request's body element.</param>
        /// <param name="request">The request wrapper, whose members become the body's children.</param>
        /// <param name="createResponse">Creates the response wrapper to read into.</param>
        protected async Task<TResponse> InvokeAsync<TResponse>(
            string action,
            string bodyNamespace,
            string bodyElement,
            OnvifContract request,
            Func<TResponse> createResponse,
            CancellationToken cancellationToken)
            where TResponse : OnvifContract
        {
            string envelope = BuildEnvelope(action, bodyNamespace, bodyElement, request);

            using (HttpResponseMessage response = await SendAsync(action, envelope, cancellationToken).ConfigureAwait(false))
            using (Stream stream = await ReadContentAsync(response).ConfigureAwait(false))
            {
                TResponse result = createResponse();

                // A reply carrying a fault is raised from in here, including for the 500 status
                // code devices use to carry one.
                _codec.ReadEnvelopeBody(stream, result, ResolveXmlType);
                return result;
            }
        }

        /// <summary>Sends an operation whose reply carries nothing worth reading.</summary>
        protected async Task InvokeAsync(
            string action,
            string bodyNamespace,
            string bodyElement,
            OnvifContract request,
            CancellationToken cancellationToken)
        {
            string envelope = BuildEnvelope(action, bodyNamespace, bodyElement, request);

            using (HttpResponseMessage response = await SendAsync(action, envelope, cancellationToken).ConfigureAwait(false))
            using (Stream stream = await ReadContentAsync(response).ConfigureAwait(false))
            {
                // Nothing worth reading is not nothing worth looking at: the reply still has to be
                // read far enough to find a fault in it.
                _codec.ReadEnvelopeBody(stream, null, ResolveXmlType);
            }
        }

        private string BuildEnvelope(string action, string bodyNamespace, string bodyElement, OnvifContract request)
        {
            // Null when this action carries no credentials, which is what leaves the message with
            // no header element rather than an empty one.
            Action<IXmlWriter> headers = _settings.Authentication == null
                ? null
                : _settings.Authentication.CreateSecurityHeader(action, _settings);

            return _codec.WriteEnvelope(_settings.EnvelopePrologue, headers, writer =>
            {
                writer.WriteStartElement(bodyNamespace, bodyElement);
                writer.WriteContent(request);
                writer.WriteEndElement();
            });
        }

        private async Task<HttpResponseMessage> SendAsync(string action, string envelope, CancellationToken cancellationToken)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, EndpointUri);
            message.Content = new StringContent(envelope, new UTF8Encoding(false));

            // Onvif carries the action as a Content-Type parameter on application/soap+xml.
            var contentType = new MediaTypeHeaderValue(_codec.ContentType);
            contentType.CharSet = "utf-8";
            contentType.Parameters.Add(new NameValueHeaderValue("action", "\"" + action + "\""));
            message.Content.Headers.ContentType = contentType;

            if (_settings.DisableExpect100Continue) message.Headers.ExpectContinue = false;

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(message, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                // The device could not be reached, or dropped the connection - which is what
                // stopping a device does to a client waiting on a pull. Raised as one Onvif
                // exception so that an application does not have to know which of the HTTP
                // stack's exceptions means "the camera went away".
                throw new SoapTransportException(
                    "The Onvif request to " + EndpointUri + " did not reach the device: " + ex.Message, ex);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // Not the caller's cancellation - the client's own timeout. It arrives as a
                // TaskCanceledException, which says nothing about what went wrong.
                throw new SoapTransportException(
                    "The Onvif request to " + EndpointUri + " timed out after " + _settings.Timeout + ".", ex)
                {
                    TimedOut = true,
                };
            }

            // A fault comes back as a SOAP envelope with a non-success status: the Onvif core
            // specification uses 400 for a sender fault and 500 for a receiver one. Whenever the
            // body is SOAP, parse it so the caller gets the fault code rather than a bare status.
            if (!response.IsSuccessStatusCode && !IsSoap(response))
            {
                string body = response.Content == null
                    ? string.Empty
                    : await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                response.Dispose();
                throw new SoapFaultException(
                    "The device returned HTTP " + (int)response.StatusCode + " " + response.ReasonPhrase +
                    (body.Length == 0 ? "." : ": " + Truncate(body, 512)));
            }

            return response;
        }

        /// <summary>True when the response body is a SOAP envelope worth parsing.</summary>
        private static bool IsSoap(HttpResponseMessage response)
        {
            var contentType = response.Content == null ? null : response.Content.Headers.ContentType;
            if (contentType == null) return false;

            return contentType.MediaType.IndexOf("soap+xml", StringComparison.OrdinalIgnoreCase) >= 0
                || contentType.MediaType.IndexOf("text/xml", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static async Task<Stream> ReadContentAsync(HttpResponseMessage response)
        {
            if (response.Content == null) return new MemoryStream();
            return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        private static string Truncate(string text, int max)
        {
            return text.Length <= max ? text : text.Substring(0, max) + "...";
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing && _ownsHttpClient) _http.Dispose();
        }
    }
}
