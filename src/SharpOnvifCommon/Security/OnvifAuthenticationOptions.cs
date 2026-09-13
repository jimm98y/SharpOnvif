using System;
using System.Collections.Generic;

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
    /// What a client and a device have to agree on to authenticate: which schemes, which hashing
    /// algorithms and qualities of protection, whether the username is hashed, and which
    /// operations need no credentials.
    /// </summary>
    /// <remarks>
    /// A description, and only a description. Read from a device's side it says what it offers
    /// and will accept; from a client's, what it understands and will send - which is why the two
    /// are configured with the same type and why this one performs nothing. What acts on it is
    /// <see cref="OnvifAuthenticationSettings"/> on the client, and the authentication handler on
    /// the device.
    /// </remarks>
    public class OnvifAuthenticationOptions
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
        public OnvifAuthenticationOptions(OnvifAuthenticationOptions other)
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

        public OnvifAuthenticationOptions()
        {
        }

        public OnvifAuthenticationOptions(DigestAuthentication authentication)
        {
            Authentication = authentication;
        }
    }
}
