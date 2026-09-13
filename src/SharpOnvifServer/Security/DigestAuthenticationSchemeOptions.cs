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

using Microsoft.AspNetCore.Authentication;
using SharpOnvifCommon.Security;
using System;
using System.Collections.Generic;

namespace SharpOnvifServer.Security
{
    public class DigestAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Default ctor.
        /// </summary>
        public DigestAuthenticationSchemeOptions()
        {  }

        private OnvifAuthenticationOptions _onvif = new OnvifAuthenticationOptions();

        /// <summary>
        /// What this device and a client have to agree on: which schemes are acceptable, which
        /// hashing algorithms and qualities of protection, whether usernames are hashed, and which
        /// operations need no credentials.
        /// </summary>
        /// <remarks>
        /// The same type the client is configured with, so the two sides of a negotiation are
        /// described once. The properties below read and write this object, so either way of
        /// setting them works and they cannot drift apart.
        /// <para>
        /// Read from the device's side these say what it offers and will accept;
        /// from the client's, what it understands and will send.
        /// </para>
        /// </remarks>
        public OnvifAuthenticationOptions Onvif
        {
            get { return _onvif; }
            set { _onvif = value ?? new OnvifAuthenticationOptions(); }
        }

        #region WsUsernameToken

        /// <summary>
        /// Maximum allowed time difference in between the client and the server in seconds. Negative value will disable the timestamp verification.
        /// </summary>
        public double WsUsernameTokenMaxTimeDeltaInMilliseconds { get; set; } = 300000;

        #endregion // WsUsernameToken

        #region HTTP Digest

        /// <summary>
        /// Realm.
        /// </summary>
        public string HttpDigestRealm { get; set; } = "IP Camera";

        /// <summary>
        /// Hashing algorithm(s). MD5 is the default when empty. Accepted values are: "MD5", "MD5-sess", "SHA-256", "SHA-256-sess", "SHA-512-256", "SHA-512-256-sess".
        /// 
        /// Onvif specific: According to the RFC 7616 we should add the algorithms in the order of server preference, starting
        ///  with the most preferred one. When the client receives the first challenge, it should use the first one it supports. 
        ///  
        /// However, in Onvif core specification section 5.9.2.2 we can see the challenges are listed with MD5 first. The current 
        ///  behavior in ODM is that when we offer SHA-256 as the first one, ODM fails to connect. When we offer MD5 as the first one,
        ///  ODM connects using MD5 which seems to be the only one supported.
        ///  
        /// WWW-Authenticate challenges will be generated in the same order they are listed here.
        /// </summary>
        /// <summary>
        /// Offered quality of protection levels. Valid values are "auth" and "auth-int".
        /// </summary>
        /// <summary>
        /// Indicates whether the server should offer User hashing.
        /// </summary>
        /// <summary>
        /// How long the server nonce is valid in milliseconds. Defaults to 30 seconds.
        /// </summary>
        public double HttpDigestNonceLifetimeMilliseconds { get; set; } = 30000;

        /// <summary>
        /// Where spent nonces are remembered, so that a captured request cannot be replayed. Left
        /// null, <see cref="HttpDigestAuthentication.NonceReplayStore"/> is used, which holds them
        /// in this process only.
        /// </summary>
        /// <remarks>
        /// A device or a single server instance needs nothing here. More than one instance behind
        /// one address does: set a store every instance can read, and give them all the same nonce
        /// private key with <see cref="HttpDigestAuthentication.SetNoncePrivateKey(byte[])"/>.
        /// Otherwise a request one instance refuses as a replay is accepted by the next.
        /// </remarks>
        public INonceReplayStore HttpDigestNonceReplayStore { get; set; }

        /// <summary>
        /// When true, forces the client to use the nonce from the last response. 
        /// This breaks pipelining of the client requests, but it lets the server enforce single-use nonces.
        /// </summary>
        //public bool HttpDigestIsUsingNextNonce { get; set; } = false;

        #endregion // HTTP Digest

        #region PreAuth actions

        /// <summary> 
        /// List of actions that should be allowed without authentication. This is needed for example for the GetCapabilities action, which is called by clients before they authenticate.
        /// </summary>
        #endregion // PreAuth actions
    }
}
