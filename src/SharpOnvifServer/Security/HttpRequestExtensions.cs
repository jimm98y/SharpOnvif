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
        public static WebDigestAuth GetSecurityHeaderFromHeaders(this HttpRequest request)
        {
            if (request.Headers.Authorization.Count > 0)
            {
                string auth = request.Headers.Authorization[0];
                if (auth.StartsWith("Digest ", StringComparison.OrdinalIgnoreCase))
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

                    // some implementations put quotes around qop (ODM)
                    string qop = HttpDigestAuthentication.GetValueFromHeader(auth, "qop", false);
                    if (!string.IsNullOrEmpty(qop) && qop.Contains("\""))
                    {
                        // Note that this is against RFC 7616 that says: For historical reasons, a sender MUST NOT
                        //  generate the quoted string syntax for the following parameters: qop and nc
                        qop = qop.Replace("\"", "");
                    }

                    string cnonce = HttpDigestAuthentication.GetValueFromHeader(auth, "cnonce", true);
                    string nc = HttpDigestAuthentication.GetValueFromHeader(auth, "nc", false);
                    if (!string.IsNullOrEmpty(nc) && nc.Contains("\""))
                    {
                        // Note that this is against RFC 7616 that says: For historical reasons, a sender MUST NOT
                        //  generate the quoted string syntax for the following parameters: qop and nc
                        nc = nc.Replace("\"", "");
                    }

                    string userHash = HttpDigestAuthentication.GetValueFromHeader(auth, "userhash", false);

                    return new WebDigestAuth(algorithm, userName, realm, nonce, uri, response, qop, cnonce, nc, userHash, opaque);
                }
            }
            return null;
        }
    }
}
