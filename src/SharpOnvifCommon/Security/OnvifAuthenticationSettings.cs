using System;
using System.Collections.Generic;
using System.Net.Http;
using SharpOnvifCommon.Soap;
using SharpOnvifCommon.Xml;

namespace SharpOnvifCommon.Security
{
    /// <summary>
    /// The authentication schemes Onvif defines. They are flags because a device may accept
    /// either, and both are offered by default so a client works against the widest range of
    /// hardware without being told which to use.
    /// </summary>
    [Flags]
    public enum DigestAuthentication
    {
        None = 0,

        /// <summary>WS-UsernameToken, carried in the SOAP security header.</summary>
        WsUsernameToken = 1,

        /// <summary>HTTP Digest, negotiated through a 401 challenge.</summary>
        HttpDigest = 2,
    }

    /// <summary>
    /// How a client authenticates to a device. Shared by the simple client and the generated
    /// service clients.
    /// </summary>
    /// <remarks>
    /// This is Onvif's answer to <see cref="IClientAuthentication"/>, which is all the generated
    /// client knows about: the two schemes the core specification defines, HTTP Digest at the
    /// transport and the WS-Security UsernameToken in the message. The same object describes what
    /// a device offers, which is why it is a description rather than a mechanism - what does the
    /// work is <see cref="HttpDigestHandler"/> and <see cref="WsUsernameToken"/>.
    /// </remarks>
    public class OnvifAuthenticationSettings : IClientAuthentication
    {
        public DigestAuthentication Authentication { get; set; } =
            DigestAuthentication.WsUsernameToken | DigestAuthentication.HttpDigest;

        /// <summary>
        /// Hashing algorithms, in the order they are offered. Accepted values are "MD5",
        /// "MD5-sess", "SHA-256", "SHA-256-sess", "SHA-512-256", and "SHA-512-256-sess".
        /// <para>
        /// RFC 7616 asks for server-preference order, but the Onvif core specification lists MD5
        /// first and some tools fail to connect when anything else leads, so MD5 stays first.
        /// </para>
        /// </summary>
        public List<string> HttpDigestAlgorithms { get; set; } = new List<string>
        {
            "MD5", "MD5-sess", "SHA-256", "SHA-256-sess", "SHA-512-256", "SHA-512-256-sess",
        };

        /// <summary>Offered quality of protection levels: "auth" and "auth-int".</summary>
        public List<string> HttpDigestQop { get; set; } = new List<string> { "auth", "auth-int" };

        /// <summary>Whether username hashing is offered.</summary>
        public bool HttpDigestUserHash { get; set; } = true;

        /// <summary>
        /// Actions the Onvif core specification places in the PRE_AUTH category, which a device
        /// must answer without credentials.
        /// <para>
        /// Some devices do demand authentication for these anyway. Removing an action from this
        /// list makes the client authenticate it like any other.
        /// </para>
        /// </summary>
        public List<string> PreAuthActions { get; set; } = new List<string>
        {
            "http://www.onvif.org/ver10/device/wsdl/GetWsdlUrl",
            "http://www.onvif.org/ver10/device/wsdl/GetServices",
            "http://www.onvif.org/ver10/device/wsdl/GetServiceCapabilities",
            "http://www.onvif.org/ver10/device/wsdl/GetCapabilities",
            "http://www.onvif.org/ver10/device/wsdl/GetHostname",
            "http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime",
            "http://www.onvif.org/ver10/device/wsdl/GetEndpointReference",
        };

        /// <summary>
        /// Copies another set of settings, lists included, so that changing one afterwards does
        /// not change the other.
        /// </summary>
        public OnvifAuthenticationSettings(OnvifAuthenticationSettings other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            Authentication = other.Authentication;
            HttpDigestUserHash = other.HttpDigestUserHash;

            HttpDigestAlgorithms = other.HttpDigestAlgorithms == null
                ? null : new List<string>(other.HttpDigestAlgorithms);
            HttpDigestQop = other.HttpDigestQop == null
                ? null : new List<string>(other.HttpDigestQop);
            PreAuthActions = other.PreAuthActions == null
                ? null : new List<string>(other.PreAuthActions);
        }

        public OnvifAuthenticationSettings()
        {
        }

        public OnvifAuthenticationSettings(DigestAuthentication authentication)
        {
            Authentication = authentication;
        }

        /// <summary>True when the given SOAP action may be sent without credentials.</summary>
        public bool IsPreAuth(string action)
        {
            return PreAuthActions != null && action != null && PreAuthActions.Contains(action);
        }

        /// <summary>
        /// True when HTTP Digest is in play, which is answered by a handler in the client's own
        /// pipeline and so cannot be arranged on an HttpClient somebody else built.
        /// </summary>
        public bool RequiresOwnTransport(OnvifClientSettings settings)
        {
            return settings != null
                && settings.Credentials != null
                && (Authentication & DigestAuthentication.HttpDigest) != 0;
        }

        /// <summary>Puts the digest handler in the pipeline when HTTP Digest is in play.</summary>
        public HttpMessageHandler CreateTransport(HttpMessageHandler inner, OnvifClientSettings settings)
        {
            return RequiresOwnTransport(settings)
                ? new HttpDigestHandler(settings.Credentials, this, inner)
                : inner;
        }

        /// <summary>
        /// Writes the WS-Security UsernameToken, unless there is nothing to write: no credentials,
        /// the scheme switched off, or an action the device answers without them.
        /// </summary>
        public Action<OnvifXmlWriter> CreateSecurityHeader(string action, OnvifClientSettings settings)
        {
            if (settings == null || settings.Credentials == null) return null;
            if ((Authentication & DigestAuthentication.WsUsernameToken) == 0) return null;
            if (IsPreAuth(action)) return null;

            return writer => WsUsernameToken.Write(
                writer,
                settings.Credentials.UserName,
                settings.Credentials.Password,
                settings.UtcNowOffset);
        }
    }
}
