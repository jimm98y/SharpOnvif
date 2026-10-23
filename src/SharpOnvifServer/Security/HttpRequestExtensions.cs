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

using Microsoft.AspNetCore.Http;
using SharpOnvifCommon.Security;
using System;

namespace SharpOnvifServer.Security
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Whether the request carries an Authorization header with the Digest scheme, well-formed or not.
        /// </summary>
        public static bool HasDigestAuthorizationHeader(this HttpRequest request)
        {
            if (request.Headers.Authorization.Count == 0)
                return false;

            string auth = request.Headers.Authorization[0];
            return auth != null &&
                (auth.StartsWith("Digest ", StringComparison.OrdinalIgnoreCase) || string.Equals(auth.Trim(), "Digest", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Parses the Digest Authorization header. Returns null when there is none, or when it lacks
        /// any of username, realm, nonce, uri or response.
        /// </summary>
        public static WebDigestAuth GetSecurityHeaderFromHeaders(this HttpRequest request)
        {
            if (request.Headers.Authorization.Count > 0)
            {
                string auth = request.Headers.Authorization[0];
                if (auth != null && auth.StartsWith("Digest ", StringComparison.OrdinalIgnoreCase))
                {
                    string realm = HttpDigestAuthentication.GetValueFromHeader(auth, "realm", true);
                    string algorithm = HttpDigestAuthentication.GetValueFromHeader(auth, "algorithm", false) ?? "";
                    string userName = HttpDigestAuthentication.GetValueFromHeader(auth, "username", true);

                    // read username*
                    if (string.IsNullOrEmpty(userName))
                    {
                        userName = HttpDigestAuthentication.GetValueFromHeader(auth, "username\\*", false); // username*
                        if (!string.IsNullOrEmpty(userName) && userName.StartsWith("UTF-8''"))
                        {
                            userName = Uri.UnescapeDataString(userName.Substring("UTF-8''".Length));
                        }
                    }

                    string response = HttpDigestAuthentication.GetValueFromHeader(auth, "response", true);
                    string nonce = HttpDigestAuthentication.GetValueFromHeader(auth, "nonce", true);
                    string uri = HttpDigestAuthentication.GetValueFromHeader(auth, "uri", true);
                    string opaque = HttpDigestAuthentication.GetValueFromHeader(auth, "opaque", true);

                    // Some implementations (ODM) quote qop and nc, against RFC 7616. GetValueFromHeader
                    // reads either form.
                    string qop = HttpDigestAuthentication.GetValueFromHeader(auth, "qop", false);
                    string cnonce = HttpDigestAuthentication.GetValueFromHeader(auth, "cnonce", true);
                    string nc = HttpDigestAuthentication.GetValueFromHeader(auth, "nc", false);

                    string userHash = HttpDigestAuthentication.GetValueFromHeader(auth, "userhash", false);

                    if (string.IsNullOrEmpty(userName) ||
                        string.IsNullOrEmpty(realm) ||
                        string.IsNullOrEmpty(nonce) ||
                        string.IsNullOrEmpty(uri) ||
                        string.IsNullOrEmpty(response))
                    {
                        // the header is malformed
                        return null;
                    }

                    return new WebDigestAuth(algorithm, userName, realm, nonce, uri, response, qop, cnonce, nc, userHash, opaque);
                }
            }
            return null;
        }
    }
}
