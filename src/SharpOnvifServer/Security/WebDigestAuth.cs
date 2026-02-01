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

namespace SharpOnvifServer.Security
{
    public class WebDigestAuth
    {
        public string Response { get; }
        public string UserName { get; }
        public string Realm { get; }
        public string Nonce { get; }
        public string Uri { get; }
        public string Algorithm { get; }
        public string Opaque { get; }

        public string Qop { get; }
        public string CNonce { get; }
        public string Nc { get; }
        public string UserHash { get; }

        public WebDigestAuth(
            string algorithm,
            string userName,
            string realm,
            string nonce,
            string uri,
            string response,
            string qop,
            string cnonce,
            string nc,
            string userHash,
            string opaque)
        {
            Algorithm = string.IsNullOrEmpty(algorithm) ? "" : algorithm;
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
            Realm = realm ?? throw new ArgumentNullException(nameof(realm));
            Nonce = nonce ?? throw new ArgumentNullException(nameof(nonce));
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            Response = response ?? throw new ArgumentNullException(nameof(response));
            Opaque = opaque;

            Qop = qop;
            CNonce = cnonce;
            Nc = nc;
            UserHash = userHash;
        }
    }
}
