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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpOnvifCommon.Security;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipelines;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Buffers;

namespace SharpOnvifServer.Security
{
    public class DigestAuthenticationHandler : AuthenticationHandler<DigestAuthenticationSchemeOptions>
    {
        private const string CONTEXT_AUTHENTICATE_WEB_DIGEST_RESULT = "authenticateWebDigestResult_07E740A9-0079-42CF-9FDE-510FDAB3A1D9";
        private const string CONTEXT_OPAQUE = "opaque_E768DBA5-D7A8-4735-BD34-FE9F0D65DE54";

        /// <summary>
        /// Holds this request's HTTP Digest once it has been checked and held up - and nothing at
        /// all until then.
        /// </summary>
        /// <remarks>
        /// What proves the device knows the password is computed from it, so it is the checked
        /// token that is kept rather than the header it came from. Reading that header a second
        /// time would mean the values used to prove identity were never themselves the values
        /// that were verified.
        /// </remarks>
        internal const string CONTEXT_VALIDATED_DIGEST = "validatedDigest_5C1B0B0E-4E51-4A2E-9E63-0B2D9C6C5E44";

        private const int NONCE_SALT_LENGTH = 12;
        private const string NONCE_HASH_ALGORITHM = "SHA-256";
        private const BinarySerializationType PREFERRED_SERIALIZATION = BinarySerializationType.Hex;

        public const string ANONYMOUS_USER = "Anonymous";

        private readonly IUserRepository _userRepository;

        public DigestAuthenticationHandler(
            IOptionsMonitor<DigestAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IUserRepository userRepository) :
           base(options, logger, encoder)
        {
            _userRepository = userRepository;
        }

        protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if(Options.Onvif.Authentication == DigestAuthentication.None || await AllowAnonymousAccessAsync().ConfigureAwait(false))
            {
                // use Anonymous user either when auth is turned off, or for selected Onvif actions that do not require authentication
                var identity = new GenericIdentity(ANONYMOUS_USER);
                var claimsPrincipal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }

            if (Options.Onvif.Authentication.HasFlag(DigestAuthentication.HttpDigest))
            {
                // according to the Onvif specification, we must first authenticate the Digest if it's present
                WebDigestAuth webToken = Request.GetSecurityHeaderFromHeaders();

                if (webToken != null)
                {
                    if (string.Compare(webToken.Realm, Options.HttpDigestRealm) != 0)
                    {
                        return AuthenticateResult.Fail("HTTP Digest has invalid realm.");
                    }

                    if (!IsOfferedAlgorithm(webToken.Algorithm))
                    {
                        // The algorithm arrives in the request, so without this a client picks it
                        // regardless of what the device offered - and an unrecognised name used to
                        // fall through to MD5. Configuring SHA-256 only has to mean something.
                        return AuthenticateResult.Fail($"HTTP Digest algorithm '{webToken.Algorithm}' was not offered.");
                    }

                    // store the opaque for the duration of this request
                    if (HttpDigestAuthentication.ValidateOpaque(PREFERRED_SERIALIZATION, webToken.Opaque) == 0)
                    {
                        Context.Items[CONTEXT_OPAQUE] = webToken.Opaque;
                    }

                    try
                    {
                        byte[] body = null;
                        if (string.Compare("auth-int", webToken.Qop, true) == 0)
                        {
                            body = await ReadRequestBodyAsync().ConfigureAwait(false);
                        }

                        int authenticateWebDigestResult = await AuthenticateWebDigestAsync(Options.HttpDigestRealm, Request.Method, webToken, body).ConfigureAwait(false);
                        if (authenticateWebDigestResult == 0)
                        {
                            // A request that authenticated one way may carry credentials the other
                            // way too, and then the two have to agree: digest as one user while
                            // the header claims another is not something to let through.
                            //
                            // Only when this device takes the older scheme, though. A client that
                            // knows both sends the token whether or not the device wants it,
                            // having no way to find out but to be refused - so on a device that
                            // has switched it off the token is not a credential at all, nothing
                            // here reads it, and the identity comes from the digest that just
                            // succeeded. Failing the request for carrying it locks out every
                            // client that has not been told which schemes this device kept.
                            SoapDigestAuth token =
                                Options.Onvif.Authentication.HasFlag(DigestAuthentication.WsUsernameToken)
                                    ? await GetSecurityHeaderFromSoapEnvelopeAsync(Request).ConfigureAwait(false)
                                    : null;

                            if (token != null)
                            {
                                try
                                {
                                    if (await AuthenticateSoapDigestAsync(token.UserName, token.Password, token.Nonce, token.Created).ConfigureAwait(false) == 0)
                                    {
                                        UserInfo user = await _userRepository.GetUserAsync(webToken).ConfigureAwait(false);

                                        if (string.Compare(token.UserName, user.UserName, false, CultureInfo.InvariantCulture) != 0)
                                        {
                                            return AuthenticateResult.Fail("HTTP Digest and WsUsernameToken users do not match.");
                                        }
                                    }
                                    else
                                    {
                                        return AuthenticateResult.Fail("HTTP Digest authentication succeeded, but WsUsernameToken authentication has failed.");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    return AuthenticateResult.Fail($"HTTP Digest authentication succeeded, but WsUsernameToken authentication has failed: {ex.Message}");
                                }
                            }

                            if (!string.IsNullOrEmpty(webToken.Opaque))
                            {
                                HttpDigestAuthentication.TrySetNoncePrime(webToken.Opaque, (webToken.Nonce, webToken.CNonce));
                            }

                            Context.Items[CONTEXT_VALIDATED_DIGEST] = webToken;

                            var identity = new GenericIdentity(webToken.UserName);
                            var claimsPrincipal = new ClaimsPrincipal(identity);
                            var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
                            return AuthenticateResult.Success(ticket);
                        }
                        else if (authenticateWebDigestResult == HttpDigestAuthentication.ERROR_NONCE_EXPIRED)
                        {
                            // using the Fail(, properties) parameter does not work, the information is lost in ASP.NET
                            Context.Items[CONTEXT_AUTHENTICATE_WEB_DIGEST_RESULT] = authenticateWebDigestResult;
                            return AuthenticateResult.Fail("HTTP Digest nonce has expired.");
                        }
                        else
                        {
                            // A digest that does not hold up is a refusal, not something to fall
                            // past. Credentials presented both ways have to be right both ways,
                            // and the specification has the digest checked first - so a bad
                            // digest must not be excused by a good token further down the
                            // request.
                            return AuthenticateResult.Fail("HTTP Digest authentication failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        return AuthenticateResult.Fail($"HTTP Digest authentication failed: {ex.Message}");
                    }
                }
            }
            
            if(Options.Onvif.Authentication.HasFlag(DigestAuthentication.WsUsernameToken))
            {
                SoapDigestAuth token = await GetSecurityHeaderFromSoapEnvelopeAsync(Request).ConfigureAwait(false);
                if (token != null)
                {
                    try
                    {
                        if (await AuthenticateSoapDigestAsync(token.UserName, token.Password, token.Nonce, token.Created).ConfigureAwait(false) == 0)
                        {
                            var identity = new GenericIdentity(token.UserName);
                            var claimsPrincipal = new ClaimsPrincipal(identity);
                            var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
                            return AuthenticateResult.Success(ticket);
                        }
                        else
                        {
                            return AuthenticateResult.Fail("WsUsernameToken authentication failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        return AuthenticateResult.Fail($"WsUsernameToken authentication failed: {ex.Message}");
                    }
                }
            }

            return AuthenticateResult.Fail("No authentication found");
        }

        /// <summary>
        /// True when the algorithm is one this device advertised. An absent algorithm means MD5,
        /// which RFC 7616 defines as the default, so it is accepted only when MD5 was offered.
        /// </summary>
        private bool IsOfferedAlgorithm(string algorithm)
        {
            if (!HttpDigestAuthentication.IsSupportedAlgorithm(algorithm)) return false;

            var offered = Options.Onvif.HttpDigestAlgorithms;
            if (offered == null || offered.Count == 0) return string.IsNullOrEmpty(algorithm) || algorithm == "MD5";

            string requested = string.IsNullOrEmpty(algorithm) ? "MD5" : algorithm;
            foreach (string candidate in offered)
            {
                string offeredName = string.IsNullOrEmpty(candidate) ? "MD5" : candidate;
                if (string.Equals(offeredName, requested, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        /// <summary>
        /// Whether this request names one of the operations the Onvif specification puts in its
        /// PRE_AUTH class, which a device answers without credentials.
        /// </summary>
        /// <remarks>
        /// The action is read by the same code that dispatches on it, and compared whole rather
        /// than looked for inside the header. Anything else lets a request authenticate as one
        /// operation and run as another: naming an unquoted operation first and a quoted PRE_AUTH
        /// one second used to satisfy this check while the endpoint dispatched the first.
        /// </remarks>
        private async Task<bool> AllowAnonymousAccessAsync()
        {
            if (Options.Onvif.PreAuthActions == null) return false;

            string action = Dispatch.OnvifRequestAction.FromContentType(Request.ContentType);

            if (action == null)
            {
                // Onvif Device Manager sends the action as a wsa:Action header instead, and the
                // endpoint falls back to reading it there - so this has to fall back the same way
                // and in the same order, or an operation the specification says needs no password
                // is asked for one.
                byte[] body = await ReadRequestBodyAsync().ConfigureAwait(false);
                if (body == null || body.Length == 0) return false;

                action = Dispatch.OnvifRequestAction.FromEnvelope(Encoding.UTF8.GetString(body));
            }

            return action != null && Options.Onvif.PreAuthActions.Contains(action);
        }

        /// <summary>
        /// The request body, without consuming it, or null when there is more of it than this
        /// endpoint would answer.
        /// </summary>
        /// <remarks>
        /// Read to the end rather than taking whatever the first read happens to return: a body
        /// that arrives in more than one piece was being digested as its first piece, which fails
        /// an auth-int request for no reason the sender can see.
        /// <para>
        /// Capped at what the endpoint will accept, because this runs before the endpoint does -
        /// so without a cap here, a request too large to ever be dispatched would be buffered in
        /// full before anything refused it. Nothing is consumed either way: the endpoint still
        /// reads the same body afterwards.
        /// </para>
        /// </remarks>
        private async Task<byte[]> ReadRequestBodyAsync()
        {
            long maximum = Dispatch.OnvifEndpoint.MaxRequestBytes;

            while (true)
            {
                ReadResult read = await Request.BodyReader.ReadAsync().ConfigureAwait(false);
                ReadOnlySequence<byte> buffer = read.Buffer;

                try
                {
                    if (read.IsCanceled) return null;
                    if (buffer.Length > maximum) return null;

                    // The bytes themselves, not a round trip through a string: an auth-int digest
                    // covers what was sent, and a body that is not valid UTF-8 does not survive
                    // being decoded and re-encoded.
                    if (read.IsCompleted) return buffer.ToArray();
                }
                finally
                {
                    // Consumed nothing, examined everything - so the next read waits for more,
                    // and the whole body is still there for the endpoint.
                    Request.BodyReader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
        }

        public async Task<int> AuthenticateSoapDigestAsync(string userName, string digest, string nonce, string created)
        {
            var user = await _userRepository.GetUserAsync(userName).ConfigureAwait(false);
            if (user != null)
            {
                if (user.IsPasswordAlreadyHashed)
                    throw new NotSupportedException("WsUsernameToken is not compatible with pre-hashed (HA1) passwords.");

                string calculatedDigest = WsDigestAuthentication.CreateSoapDigest(nonce, created, user.Password);                
                DateTime createdDateTime;

                // All times MUST be in UTC format as specified https://docs.oasis-open.org/wss-m/wss/v1.1.1/os/wss-SOAPMessageSecurity-v1.1.1-os.html
                if (DateTime.TryParse(created, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out createdDateTime))
                {
                    if (Options.WsUsernameTokenMaxTimeDeltaInMilliseconds < 0 || 
                        Math.Abs(DateTime.UtcNow.Subtract(createdDateTime.ToUniversalTime()).TotalMilliseconds) < Options.WsUsernameTokenMaxTimeDeltaInMilliseconds)
                    {
                        if (calculatedDigest.CompareTo(digest) == 0)
                        {
                            return 0;
                        }
                    }
                }
            }

            return 1;
        }

        private async Task<int> AuthenticateWebDigestAsync(string realm, string method, WebDigestAuth webToken, byte[] body = null)
        {
            UserInfo user = await _userRepository.GetUserAsync(webToken).ConfigureAwait(false);

            if (user != null)
            {
                int nonceValidationResult = await HttpDigestAuthentication.ValidateServerNonceAsync(
                    NONCE_HASH_ALGORITHM,
                    PREFERRED_SERIALIZATION,
                    webToken.Nonce,
                    HttpDigestAuthentication.ConvertNCToInt(webToken.Nc),
                    DateTimeOffset.UtcNow, 
                    null, 
                    NONCE_SALT_LENGTH,
                    Options.HttpDigestNonceLifetimeMilliseconds,
                    true,
                    Options.HttpDigestNonceReplayStore,
                    Context.RequestAborted).ConfigureAwait(false);
                if (nonceValidationResult == 0)
                {
                    string digest;

                    // legacy, not officially supported by Onvif Core specs
                    /*
                    digest = DigestAuthentication.CreateWebDigestRFC2069(
                        webToken.Algorithm, 
                        user.UserName, 
                        realm, 
                        user.Password,
                        user.IsPasswordAlreadyHashed,
                        webToken.Nonce, 
                        method,
                        webToken.Uri);
                    */

                    string noncePrime = webToken.Nonce;
                    string cnoncePrime = webToken.CNonce;
                    if (!string.IsNullOrEmpty(webToken.Opaque))
                    {
                        var prime = HttpDigestAuthentication.GetNoncePrime(webToken.Opaque);
                        if(prime != null)
                        {
                            noncePrime = prime.Value.nonce;
                            cnoncePrime = prime.Value.cnonce;
                        }
                    }

                    digest = HttpDigestAuthentication.CreateWebDigestRFC7616(
                        webToken.Algorithm,
                        user.UserName,
                        realm,
                        user.Password,
                        user.IsPasswordAlreadyHashed,
                        webToken.Nonce,
                        method,
                        webToken.Uri,
                        HttpDigestAuthentication.ConvertNCToInt(webToken.Nc),
                        webToken.CNonce,
                        webToken.Qop,
                        body,
                        noncePrime,
                        cnoncePrime);

                    return HttpDigestAuthentication.FixedTimeEquals(digest, webToken.Response) ? 0 : 2;
                }
                else
                {
                    return nonceValidationResult;
                }
            }

            return 1;
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;

            if (Options.Onvif.Authentication.HasFlag(DigestAuthentication.HttpDigest))
            {
                object authenticateWebDigestResult = Context.Items[CONTEXT_AUTHENTICATE_WEB_DIGEST_RESULT];
                string opaque = Context.Items[CONTEXT_OPAQUE]?.ToString();

                if (!string.IsNullOrEmpty(opaque))
                {
                    // remove it from the cache, but keep opaque value (use the same session)
                    HttpDigestAuthentication.RemoveNoncePrime(opaque);
                }
                else
                {
                    opaque = HttpDigestAuthentication.GenerateOpaque(PREFERRED_SERIALIZATION);
                }

                bool isStale =
                    authenticateWebDigestResult != null &&
                    (int)authenticateWebDigestResult == HttpDigestAuthentication.ERROR_NONCE_EXPIRED;

                string wwwAuth;
                // legacy, not officially supported by Onvif Core spec
                /*
                wwwAuth = DigestAuthentication.CreateWwwAuthenticateRFC2069(
                        NONCE_HASH_ALGORITHM,
                        PREFERRED_SERIALIZATION, 
                        DateTimeOffset.UtcNow, 
                        "MD5",
                        null, 
                        DigestAuthentication.CreateNonceSessionSalt(NONCE_SALT_LENGTH), 
                        Options.Realm);
                */

                var now = DateTimeOffset.UtcNow;
                var hashingAlgorithms = Options.Onvif.HttpDigestAlgorithms == null ? new List<string>() { "MD5" } : Options.Onvif.HttpDigestAlgorithms.ToList();
                var allowedQop = Options.Onvif.HttpDigestQop == null ? "auth" : string.Join(", ", Options.Onvif.HttpDigestQop.ToList());

                foreach (var algorithm in hashingAlgorithms)
                {
                    wwwAuth = HttpDigestAuthentication.CreateWwwAuthenticateRFC7616(
                            NONCE_HASH_ALGORITHM,
                            PREFERRED_SERIALIZATION,
                            now,
                            algorithm,
                            null,
                            HttpDigestAuthentication.CreateNonceSessionSalt(NONCE_SALT_LENGTH),
                            Options.HttpDigestRealm,
                            opaque,
                            allowedQop,
                            "",
                            Options.Onvif.HttpDigestUserHash,
                            isStale);
                    Response.Headers.Append("WWW-Authenticate", wwwAuth);
                }

                await Context.Response.WriteAsync("You are not logged in via Digest auth").ConfigureAwait(false);
            }
            else
            {
                await Context.Response.WriteAsync("You are not logged in").ConfigureAwait(false);
            }
        }

        private static async Task<SoapDigestAuth> GetSecurityHeaderFromSoapEnvelopeAsync(HttpRequest request)
        {
            ReadResult requestBodyInBytes = await request.BodyReader.ReadAsync().ConfigureAwait(false);
            string body = Encoding.UTF8.GetString(requestBodyInBytes.Buffer.ToArray());
            request.BodyReader.AdvanceTo(requestBodyInBytes.Buffer.Start, requestBodyInBytes.Buffer.End);

            SoapDigestAuth security = null;
            if (body?.Contains(@"http://www.w3.org/2003/05/soap-envelope") == true)
            {
                XNamespace ns = "http://www.w3.org/2003/05/soap-envelope";
                var soapEnvelope = XDocument.Parse(body);
                var headers = soapEnvelope.Descendants(ns + "Header").ToList();

                foreach (var header in headers)
                {
                    var securityElement = header.Descendants().FirstOrDefault(x => x.Name.LocalName == "Security");
                    if (securityElement != null)
                    {
                        var userNameTokenElement = securityElement.Descendants().FirstOrDefault(x => x.Name.LocalName == "UsernameToken");
                        if (userNameTokenElement != null)
                        {
                            var serializer = new XmlSerializer(typeof(UsernameToken));
                            using (var str = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(userNameTokenElement.ToString())))
                            {
                                UsernameToken xml = (UsernameToken)serializer.Deserialize(str);
                                security = new SoapDigestAuth(xml.Username, xml.Password.Text, xml.Nonce.Text, xml.Created);
                            }
                            break;
                        }
                    }
                }
            }

            return security;
        }
    }
}
