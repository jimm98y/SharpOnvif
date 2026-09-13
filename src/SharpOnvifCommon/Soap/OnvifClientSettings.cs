using System;
using System.Net;
using System.Net.Http;
using SharpOnvifCommon.Security;

namespace SharpOnvifCommon.Soap
{
    /// <summary>How a generated service client talks to a device.</summary>
    public class OnvifClientSettings
    {
        /// <summary>Credentials, or null for an unauthenticated client.</summary>
        public NetworkCredential Credentials { get; set; }

        /// <summary>
        /// Where this client reports what it could not do. Null - the default - reports nowhere.
        /// </summary>
        /// <remarks>
        /// Per client rather than per process, so that two clients in one application can report
        /// to different places, or one of them to nowhere at all.
        /// </remarks>
        public IOnvifLogger Logger { get; set; }

        /// <summary>Which authentication schemes to use, and how.</summary>
        public OnvifAuthenticationSettings Authentication { get; set; } = new OnvifAuthenticationSettings();

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Suppresses the Expect: 100-continue request header.
        /// <para>
        /// Many devices do not answer the continuation and simply stall, so this defaults to on.
        /// </para>
        /// </summary>
        public bool DisableExpect100Continue { get; set; } = true;

        /// <summary>
        /// Largest response the client will buffer, in bytes. Zero means no limit.
        /// <para>
        /// The WCF bindings this replaces capped messages at 1 MB, which some devices exceed when
        /// returning a large profile or event batch. The cap here is high enough not to be hit in
        /// practice while still bounding what a misbehaving device can make the client allocate.
        /// </para>
        /// </summary>
        public long MaxResponseContentBytes { get; set; } = 16L * 1024 * 1024;

        /// <summary>
        /// The transport handler to send through. Supply one to control TLS validation, proxying,
        /// or connection pooling; leave null to let the client create its own.
        /// </summary>
        public HttpMessageHandler Transport { get; set; }

        /// <summary>
        /// An HttpClient to send through, instead of one built from the settings above, for a
        /// caller who manages their own clients. Timeout and Transport are ignored, and the
        /// client is not disposed with the service client.
        /// <para>
        /// HTTP Digest cannot be arranged on a client that is already built, because answering a
        /// challenge takes a handler in its pipeline. Supplying a client while HTTP Digest is
        /// requested is refused rather than quietly sending unauthenticated requests: use
        /// <see cref="Transport"/> instead, or put an <see cref="HttpDigestHandler"/> in the
        /// client's own pipeline and leave <see cref="DigestAuthentication.HttpDigest"/> out of
        /// <see cref="Authentication"/>.
        /// </para>
        /// <para>
        /// WS-UsernameToken is unaffected: it travels in the SOAP header, which this client
        /// writes either way.
        /// </para>
        /// </summary>
        public HttpClient HttpClient { get; set; }

        public OnvifClientSettings()
        {
        }

        public OnvifClientSettings(string userName, string password)
        {
            if (!string.IsNullOrEmpty(userName))
                Credentials = new NetworkCredential(userName, password);
            else
                Authentication = new OnvifAuthenticationSettings(DigestAuthentication.None);
        }
    }
}
