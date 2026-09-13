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
using SharpOnvifCommon.Security;

namespace SharpOnvifClient.Security
{
    /// <summary>
    /// How <see cref="SimpleOnvifClient"/> authenticates to a device.
    /// </summary>
    /// <remarks>
    /// The same shape as the device's own options: what the two sides have to agree on sits in
    /// <see cref="Onvif"/>, described once by a type they share, and what belongs to this side
    /// alone sits beside it. A client's clock is its own business, as a device's realm and nonce
    /// lifetime are the device's.
    /// </remarks>
    public class DigestAuthenticationSchemeOptions
    {
        private OnvifAuthenticationSettings _onvif = new OnvifAuthenticationSettings();

        /// <summary>
        /// What this client and a device have to agree on: which schemes to use, which hashing
        /// algorithms and qualities of protection, whether the username is hashed, and which
        /// operations are sent without credentials.
        /// </summary>
        /// <remarks>
        /// Read from the client's side these say what it understands and will send; from the
        /// device's, what it offers and will accept.
        /// </remarks>
        public OnvifAuthenticationSettings Onvif
        {
            get { return _onvif; }
            set { _onvif = value ?? new OnvifAuthenticationSettings(); }
        }

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
        /// The client's business and nothing to negotiate, which is why it is here rather than in
        /// <see cref="Onvif"/>.
        /// </para>
        /// </remarks>
        public TimeSpan UtcNowOffset { get; set; } = TimeSpan.Zero;

        public DigestAuthenticationSchemeOptions()
        {
        }

        public DigestAuthenticationSchemeOptions(DigestAuthentication authentication)
        {
            _onvif.Authentication = authentication;
        }
    }
}
