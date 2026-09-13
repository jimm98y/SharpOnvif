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
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharpOnvifCommon.Security;

namespace SharpOnvifServer.Security
{
    /// <summary>
    /// Answers an authenticated request with the <c>Authentication-Info</c> header.
    /// <para>
    /// The header carries <c>rspauth</c>, a digest the device computes over its own response. It
    /// proves to the client that the device knows the password too, which is the half of HTTP
    /// Digest that authenticates the server rather than the client. A client that checks it - and
    /// SharpOnvif's does - cannot otherwise tell a real device from something answering in its
    /// place.
    /// </para>
    /// </summary>
    public static class DigestAuthenticationInfo
    {
        /// <summary>
        /// This request's HTTP Digest, if one was checked and held up, and null otherwise.
        /// </summary>
        private static WebDigestAuth ValidatedDigest(HttpContext context)
        {
            return context.Items.TryGetValue(
                DigestAuthenticationHandler.CONTEXT_VALIDATED_DIGEST, out object digest)
                    ? digest as WebDigestAuth
                    : null;
        }

        /// <summary>
        /// Appends the header for a request that authenticated with HTTP Digest. Does nothing for
        /// a request that did not, or when the device has no credentials to prove.
        /// </summary>
        /// <param name="context">The request being answered.</param>
        /// <param name="responseBody">
        /// The response about to be written, needed only when the client chose <c>auth-int</c>,
        /// whose digest covers it.
        /// </param>
        public static async Task AppendAsync(HttpContext context, byte[] responseBody)
        {
            // Doing nothing quietly would hide a caller's mistake, and there is no request to
            // answer without one.
            if (context == null) throw new ArgumentNullException(nameof(context));

            // The digest this request was admitted on, taken from where the check left it rather
            // than read out of the request again.
            //
            // Both halves of that matter. rspauth is computed with the password over the nonce,
            // the cnonce, the count and the realm - all of them the caller's - so a request that
            // merely carried a digest, and was let through for asking something that needs no
            // password, would otherwise be handed a digest of the real password over values it
            // chose. And proving identity with values that were never the ones verified is how
            // the two readings of one header drift apart, which is a bug this codebase has had
            // once already.
            WebDigestAuth webToken = ValidatedDigest(context);
            if (webToken == null) return;

            var users = context.RequestServices.GetService<IUserRepository>();
            if (users == null) return;

            UserInfo user = await users.GetUserAsync(webToken).ConfigureAwait(false);
            if (user == null) return;

            // The -sess algorithms derive their secret from the first nonce and cnonce of the
            // session, which the device remembers against the opaque it issued.
            string noncePrime = webToken.Nonce;
            string cnoncePrime = webToken.CNonce;
            if (!string.IsNullOrEmpty(webToken.Opaque))
            {
                var prime = HttpDigestAuthentication.GetNoncePrime(webToken.Opaque);
                if (prime != null)
                {
                    noncePrime = prime.Value.nonce;
                    cnoncePrime = prime.Value.cnonce;
                }
            }

            byte[] entityBody =
                string.Equals(webToken.Qop, "auth-int", StringComparison.OrdinalIgnoreCase) ? responseBody : null;

            string authenticationInfo = HttpDigestAuthentication.CreateAuthenticationInfoRFC7616(
                webToken.Algorithm,
                user.UserName,
                webToken.Realm,
                user.Password,
                user.IsPasswordAlreadyHashed,
                webToken.Nonce,
                webToken.Uri,
                HttpDigestAuthentication.ConvertNCToInt(webToken.Nc),
                webToken.CNonce,
                webToken.Qop,
                entityBody,
                null,
                noncePrime,
                cnoncePrime);

            context.Response.Headers.Append("Authentication-Info", authenticationInfo);
        }

        /// <summary>Appends the header for a response whose body is already a string.</summary>
        public static Task AppendAsync(HttpContext context, string responseBody)
        {
            return AppendAsync(context, responseBody == null ? null : Encoding.UTF8.GetBytes(responseBody));
        }
    }
}
