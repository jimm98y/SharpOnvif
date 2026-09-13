// SharpOnvif
// Copyright (C) 2026 Lukas Volf
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.

using System;
using System.Net.Http;
using SharpOnvifCommon.Soap;
using SharpOnvifCommon.Xml;

namespace SharpOnvifCommon.Security
{
    /// <summary>
    /// Authenticates a client the way Onvif does: HTTP Digest at the transport, the WS-Security
    /// UsernameToken in the message, or both.
    /// </summary>
    /// <remarks>
    /// Onvif's answer to <see cref="IClientAuthentication"/>, which is all a generated client
    /// knows. What it does is here; what it was told to do is <see cref="Options"/>, which
    /// describes a negotiation rather than performing one and is the same object a device is
    /// configured with.
    /// </remarks>
    public sealed class OnvifAuthenticationSettings : IClientAuthentication
    {
        /// <summary>What this authenticates with: the schemes, algorithms and the rest.</summary>
        public OnvifAuthenticationOptions Options { get; private set; }

        /// <summary>Authenticates with everything Onvif defines.</summary>
        public OnvifAuthenticationSettings()
            : this(new OnvifAuthenticationOptions())
        {
        }

        /// <summary>Authenticates with the given schemes, and the defaults for the rest.</summary>
        public OnvifAuthenticationSettings(DigestAuthentication authentication)
            : this(new OnvifAuthenticationOptions(authentication))
        {
        }

        public OnvifAuthenticationSettings(OnvifAuthenticationOptions options)
        {
            Options = options ?? new OnvifAuthenticationOptions();
        }

        /// <summary>
        /// True when HTTP Digest is in play, which is answered by a handler in the client's own
        /// pipeline and so cannot be arranged on an HttpClient somebody else built.
        /// </summary>
        public bool RequiresOwnTransport(IClientSettings settings)
        {
            return settings != null
                && settings.Credentials != null
                && (Options.Authentication & DigestAuthentication.HttpDigest) != 0;
        }

        /// <summary>Puts the digest handler in the pipeline when HTTP Digest is in play.</summary>
        public HttpMessageHandler CreateTransport(HttpMessageHandler inner, IClientSettings settings)
        {
            return RequiresOwnTransport(settings)
                ? new HttpDigestHandler(settings.Credentials, Options, inner)
                : inner;
        }

        /// <summary>
        /// Writes the WS-Security UsernameToken, unless there is nothing to write: no credentials,
        /// the scheme switched off, or an action the device answers without them.
        /// </summary>
        public Action<IXmlWriter> CreateSecurityHeader(string action, IClientSettings settings)
        {
            if (settings == null || settings.Credentials == null) return null;
            if ((Options.Authentication & DigestAuthentication.WsUsernameToken) == 0) return null;
            if (Options.IsPreAuth(action)) return null;

            return writer => WsUsernameToken.Write(
                writer,
                settings.Credentials.UserName,
                settings.Credentials.Password,
                settings.UtcNowOffset);
        }
    }
}
