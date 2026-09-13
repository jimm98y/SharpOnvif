using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Xml;

namespace SharpOnvifCommon.Soap
{
    /// <summary>
    /// How a generated service client talks to a device: Onvif's answer to
    /// <see cref="IClientSettings"/>, which is all the client itself knows about.
    /// </summary>
    /// <remarks>
    /// The defaults are the Onvif ones, and they are here rather than in the generated client
    /// because they are what talking to cameras taught rather than anything WSDL says.
    /// </remarks>
    public class OnvifClientSettings : IClientSettings
    {
        /// <summary>
        /// What writes a message and reads the reply. SOAP 1.2, as every Onvif binding uses.
        /// </summary>
        public IMessageCodec Codec { get; set; } = SoapMessageCodec.Instance;

        /// <summary>Credentials, or null for an unauthenticated client.</summary>
        public NetworkCredential Credentials { get; set; }

        /// <summary>
        /// Where this client reports what it could not do. Null - the default - reports nowhere.
        /// </summary>
        /// <remarks>
        /// Per client rather than per process, so that two clients in one application can report
        /// to different places, or one of them to nowhere at all.
        /// </remarks>
        public ILog Logger { get; set; }

        /// <summary>
        /// How far the device's clock is ahead of this machine's, for a device whose clock is
        /// wrong.
        /// </summary>
        /// <remarks>
        /// A WS-UsernameToken carries the time it was made, and a device refuses one that drifts
        /// too far from its own clock. Setting the difference here stamps the token in the
        /// device's time rather than in this machine's, which is what gets a client past a camera
        /// nobody can reach to correct.
        /// <para>
        /// It belongs to the client and to nothing else, which is why it is here rather than on
        /// the settings a device shares with it.
        /// </para>
        /// </remarks>
        public TimeSpan UtcNowOffset { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// How the client proves who it is, or null to send no credentials at all.
        /// </summary>
        /// <remarks>
        /// It starts as <see cref="OnvifClientAuthentication"/> with the pair of schemes the Onvif
        /// specification defines. A service that authenticates some other way is served by another
        /// implementation of <see cref="IClientAuthentication"/>.
        /// </remarks>
        public IClientAuthentication Authentication { get; set; } = new OnvifClientAuthentication();

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
        /// A scheme that answers a challenge cannot be arranged on a client that is already built,
        /// because answering takes a handler in its pipeline. Supplying a client while such a
        /// scheme is asked for is refused rather than quietly sending unauthenticated requests:
        /// use <see cref="Transport"/> instead, or put that handler in the client's own pipeline
        /// and leave the scheme out of <see cref="Authentication"/>.
        /// </para>
        /// <para>
        /// A scheme that writes into the SOAP header is unaffected: that header is written by this
        /// client either way.
        /// </para>
        /// </summary>
        public HttpClient HttpClient { get; set; }

        /// <summary>
        /// Declared on the envelope of every message this client sends. Onvif's two conventional
        /// prefixes by default; assign to send something else, or null to send none.
        /// </summary>
        public IEnumerable<XmlNamespaceDeclaration> EnvelopePrologue { get; set; } =
            OnvifXmlNamespaces.EnvelopePrologue;

        public OnvifClientSettings()
        {
        }

        /// <summary>
        /// Copies another set of settings.
        /// </summary>
        /// <remarks>
        /// So that a client needing one thing different - a longer timeout for a long poll, say -
        /// is built from these rather than from a list of the members somebody remembered to
        /// write out. Such a list goes stale silently: what it forgets is not missing, it is
        /// whatever the default happens to be.
        /// </remarks>
        public OnvifClientSettings(OnvifClientSettings other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            Codec = other.Codec;
            Credentials = other.Credentials;
            Logger = other.Logger;
            UtcNowOffset = other.UtcNowOffset;
            Authentication = other.Authentication;
            Timeout = other.Timeout;
            DisableExpect100Continue = other.DisableExpect100Continue;
            MaxResponseContentBytes = other.MaxResponseContentBytes;
            Transport = other.Transport;
            HttpClient = other.HttpClient;
            EnvelopePrologue = other.EnvelopePrologue;
        }

        public OnvifClientSettings(string userName, string password)
        {
            // No name is not a name to authenticate under. Authentication is left as it is rather
            // than cleared, because it is what decides that credentials are missing and nothing
            // should be sent - which is not the same as refusing to authenticate at all.
            if (!string.IsNullOrEmpty(userName))
                Credentials = new NetworkCredential(userName, password);
        }
    }
}
