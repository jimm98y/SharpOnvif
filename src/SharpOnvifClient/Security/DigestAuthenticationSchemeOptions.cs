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
using System.Collections.Generic;

namespace SharpOnvifClient.Security
{
    [Flags]
    public enum DigestAuthentication
    {
        None = 0,
        WsUsernameToken = 1,
        HttpDigest = 2
    }

    public class DigestAuthenticationSchemeOptions
    {
        public DigestAuthentication Authentication { get; set; } = DigestAuthentication.WsUsernameToken | DigestAuthentication.HttpDigest;

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
        public List<string> HttpDigestAlgorithms { get; set; } = new List<string>()
        {
            "MD5",
            "MD5-sess",
            "SHA-256",
            "SHA-256-sess",
            "SHA-512-256",
            "SHA-512-256-sess",
        };

        /// <summary>
        /// Offered quality of protection levels. Valid values are "auth" and "auth-int".
        /// </summary>
        public List<string> HttpDigestQop { get; set; } = new List<string>()
        {
            "auth",
            "auth-int",
        };

        /// <summary>
        /// Indicates whether the server should offer User hashing.
        /// </summary>
        public bool HttpDigestUserHash { get; set; } = true;

        /// <summary> 
        /// List of actions that should be allowed without authentication. 
        /// This is needed for example for the GetCapabilities action, which is called by clients before they authenticate.
        /// </summary>
        /// <remarks>
        /// Some cameras (Vivotec) require authentication for GetServices, which is also a PRE_AUTH action according to the Onvif Core specification.
        /// This list allows to specify such exceptions.
        /// </remarks>
        public List<string> PreAuthActions { get; set; } = new List<string>()
        {
            "http://www.onvif.org/ver10/device/wsdl/GetWsdlUrl",
            "http://www.onvif.org/ver10/device/wsdl/GetServices",
            "http://www.onvif.org/ver10/device/wsdl/GetServiceCapabilities",
            "http://www.onvif.org/ver10/device/wsdl/GetCapabilities",
            "http://www.onvif.org/ver10/device/wsdl/GetHostname",
            "http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime",
            "http://www.onvif.org/ver10/device/wsdl/GetEndpointReference",
        };

        public DigestAuthenticationSchemeOptions()
        { }

        public DigestAuthenticationSchemeOptions(DigestAuthentication authentication)
        {
            this.Authentication = authentication;
        }
    }
}
