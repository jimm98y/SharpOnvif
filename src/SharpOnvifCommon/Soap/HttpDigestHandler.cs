using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using SharpOnvifCommon.Security;

namespace SharpOnvifCommon.Soap
{
    /// <summary>
    /// Performs HTTP Digest authentication (RFC 7616, and the RFC 2617 forms devices still send)
    /// as an <see cref="HttpClient"/> handler.
    /// <para>
    /// The first request to a device goes out unauthenticated; the 401 challenge it comes back
    /// with is cached and reused, so only the first call per endpoint costs a round trip. A
    /// challenge marked stale, or a fresh 401 after a successful exchange, re-negotiates.
    /// </para>
    /// <para>
    /// Where the previous WCF implementation had to reach into the message with private
    /// reflection to get the bytes for an auth-int digest, the request and response bodies are
    /// directly available here.
    /// </para>
    /// </summary>
    public sealed class HttpDigestHandler : DelegatingHandler
    {
        private readonly NetworkCredential _credentials;
        private readonly OnvifAuthenticationOptions _settings;
        private readonly object _sync = new object();

        // Challenge state, valid for the endpoint this handler serves.
        private string _challenge;
        private string _nextNonce;
        private string _primeNonce;
        private string _primeClientNonce;

        // The nonce the count belongs to, and the count itself. A nonce count states how many
        // requests have been sent with that nonce, so it restarts whenever the nonce does.
        private string _countedNonce;
        private int _nonceCount;

        public HttpDigestHandler(NetworkCredential credentials, OnvifAuthenticationOptions settings, HttpMessageHandler inner)
            : base(inner)
        {
            _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            _settings = settings ?? new OnvifAuthenticationOptions();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Buffer the body: an auth-int digest covers it, and a retry has to send it again.
            byte[] body = request.Content == null
                ? null
                : await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            string authorization = BuildAuthorization(request, body);
            if (authorization != null) request.Headers.TryAddWithoutValidation("Authorization", authorization);

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                if (authorization != null)
                    await ValidateAuthenticationInfoAsync(response, authorization, RequestUri(request)).ConfigureAwait(false);
                return response;
            }

            // Unauthorized: adopt the challenge and try once more. A second 401 means the
            // credentials are wrong, and is returned to the caller as-is.
            if (!AdoptChallenge(response)) return response;

            response.Dispose();

            HttpRequestMessage retry = CloneRequest(request, body);
            string retryAuthorization = BuildAuthorization(retry, body);
            if (retryAuthorization != null) retry.Headers.TryAddWithoutValidation("Authorization", retryAuthorization);

            HttpResponseMessage retried = await base.SendAsync(retry, cancellationToken).ConfigureAwait(false);
            if (retryAuthorization != null && retried.StatusCode != HttpStatusCode.Unauthorized)
            {
                await ValidateAuthenticationInfoAsync(retried, retryAuthorization, RequestUri(retry)).ConfigureAwait(false);
            }

            return retried;
        }

        /// <summary>
        /// Picks the first challenge whose algorithm this client supports and stores it. Returns
        /// false when the device offered nothing usable.
        /// </summary>
        private bool AdoptChallenge(HttpResponseMessage response)
        {
            foreach (AuthenticationHeaderValue header in response.Headers.WwwAuthenticate)
            {
                if (!string.Equals(header.Scheme, "Digest", StringComparison.OrdinalIgnoreCase)) continue;

                string challenge = header.Parameter;
                if (string.IsNullOrEmpty(challenge)) continue;

                // The Onvif core specification does not permit the qop-less RFC 2069 form.
                string qop = HttpDigestAuthentication.GetValueFromHeader(challenge, "qop", true);
                if (string.IsNullOrEmpty(qop)) continue;

                string nonce = HttpDigestAuthentication.GetValueFromHeader(challenge, "nonce", true);
                if (string.IsNullOrEmpty(nonce)) continue;

                string algorithm = HttpDigestAuthentication.GetValueFromHeader(challenge, "algorithm", false) ?? "";
                bool supported = algorithm.Length == 0
                    ? _settings.HttpDigestAlgorithms.Contains("") || _settings.HttpDigestAlgorithms.Contains("MD5")
                    : _settings.HttpDigestAlgorithms.Contains(algorithm);

                if (!supported) continue;

                lock (_sync)
                {
                    bool stale = string.Equals(
                        HttpDigestAuthentication.GetValueFromHeader(challenge, "stale", false), "true",
                        StringComparison.OrdinalIgnoreCase);

                    // A challenge that is not merely stale starts a new session, and the -sess
                    // algorithms derive their secret from the first nonce and cnonce seen in it.
                    if (_challenge == null || !stale || NonceOf(_challenge) != nonce)
                    {
                        _primeNonce = null;
                        _primeClientNonce = null;
                    }

                    _challenge = challenge;
                    _nextNonce = null;
                }

                return true;
            }

            return false;
        }

        private static string NonceOf(string challenge)
        {
            return HttpDigestAuthentication.GetValueFromHeader(challenge, "nonce", true);
        }

        private string BuildAuthorization(HttpRequestMessage request, byte[] body)
        {
            string challenge;
            string nonce;
            string clientNonce;
            string primeNonce;
            string primeClientNonce;
            int nonceCount;

            lock (_sync)
            {
                if (_challenge == null) return null;

                challenge = _challenge;
                nonce = _nextNonce ?? NonceOf(_challenge);

                // The count is of requests sent with this nonce, so a nonce we have not used
                // before starts at one - whether it came from a challenge or from a nextnonce.
                if (!string.Equals(nonce, _countedNonce, StringComparison.Ordinal))
                {
                    _countedNonce = nonce;
                    _nonceCount = 0;
                }

                nonceCount = ++_nonceCount;
                clientNonce = HttpDigestAuthentication.GenerateClientNonce(BinarySerializationType.Hex);

                if (_primeNonce == null)
                {
                    _primeNonce = nonce;
                    _primeClientNonce = clientNonce;
                }

                primeNonce = _primeNonce;
                primeClientNonce = _primeClientNonce;
            }

            string realm = HttpDigestAuthentication.GetValueFromHeader(challenge, "realm", true);
            string opaque = HttpDigestAuthentication.GetValueFromHeader(challenge, "opaque", true);
            string algorithm = HttpDigestAuthentication.GetValueFromHeader(challenge, "algorithm", false) ?? "";
            bool userHash = string.Equals(
                HttpDigestAuthentication.GetValueFromHeader(challenge, "userhash", false), "true",
                StringComparison.OrdinalIgnoreCase);

            string qop = SelectQop(HttpDigestAuthentication.GetValueFromHeader(challenge, "qop", true));
            if (qop == null) return null;

            string uri = RequestUri(request);
            string method = request.Method.Method;
            byte[] entity = string.Equals(qop, "auth-int", StringComparison.OrdinalIgnoreCase) ? body : null;

            string response = HttpDigestAuthentication.CreateWebDigestRFC7616(
                algorithm, _credentials.UserName, realm, _credentials.Password, false,
                nonce, method, uri, nonceCount, clientNonce, qop, entity, primeNonce, primeClientNonce);

            string userName = userHash && _settings.HttpDigestUserHash
                ? HttpDigestAuthentication.CreateUserNameHashRFC7616(algorithm, _credentials.UserName, realm)
                : _credentials.UserName;

            return HttpDigestAuthentication.CreateAuthorizationRFC7616(
                userName, realm, nonce, uri, response, opaque, algorithm, qop,
                nonceCount, clientNonce, userHash && _settings.HttpDigestUserHash);
        }

        /// <summary>Chooses the first offered quality of protection this client also supports.</summary>
        private string SelectQop(string offered)
        {
            if (string.IsNullOrEmpty(offered)) return null;

            foreach (string candidate in offered.Split(','))
            {
                string trimmed = candidate.Trim().Trim('"');
                foreach (string supported in _settings.HttpDigestQop)
                {
                    if (string.Equals(trimmed, supported.Trim(), StringComparison.OrdinalIgnoreCase))
                        return supported.Trim();
                }
            }

            return null;
        }

        /// <summary>
        /// Verifies the server's rspauth when it sends one, which proves it knows the password
        /// too, and adopts any nextnonce it offers for the following request.
        /// </summary>
        private async Task ValidateAuthenticationInfoAsync(HttpResponseMessage response, string authorization, string uri)
        {
            if (!response.Headers.TryGetValues("Authentication-Info", out var values)) return;

            string info = null;
            foreach (string value in values) { info = value; break; }
            if (string.IsNullOrEmpty(info)) return;

            string rspauth = HttpDigestAuthentication.GetValueFromHeader(info, "rspauth", true);
            string cnonce = HttpDigestAuthentication.GetValueFromHeader(info, "cnonce", true);
            string nc = HttpDigestAuthentication.GetValueFromHeader(info, "nc", false);
            string qop = (HttpDigestAuthentication.GetValueFromHeader(info, "qop", false) ?? "").Replace("\"", "");
            string nextNonce = HttpDigestAuthentication.GetValueFromHeader(info, "nextnonce", true);

            if (string.IsNullOrEmpty(rspauth))
            {
                // Some devices send only a nextnonce. There is nothing to verify, but the nonce
                // is still worth taking.
                if (!string.IsNullOrEmpty(nextNonce)) lock (_sync) { _nextNonce = nextNonce; }
                return;
            }

            string sentCnonce = HttpDigestAuthentication.GetValueFromHeader(authorization, "cnonce", true);
            string sentNc = HttpDigestAuthentication.GetValueFromHeader(authorization, "nc", false);
            string sentQop = HttpDigestAuthentication.GetValueFromHeader(authorization, "qop", false);

            if (!string.IsNullOrEmpty(cnonce) && cnonce != sentCnonce)
                throw new AuthenticationException("The device echoed a different cnonce than the one sent.");
            if (!string.IsNullOrEmpty(nc) && nc != sentNc)
                throw new AuthenticationException("The device echoed a different nonce count than the one sent.");
            if (qop.Length > 0 && !string.Equals(qop, sentQop, StringComparison.OrdinalIgnoreCase))
                throw new AuthenticationException("The device echoed a different qop than the one sent.");

            string algorithm = HttpDigestAuthentication.GetValueFromHeader(authorization, "algorithm", false) ?? "";
            string nonce = HttpDigestAuthentication.GetValueFromHeader(authorization, "nonce", true);
            string realm = HttpDigestAuthentication.GetValueFromHeader(authorization, "realm", true);

            string primeNonce;
            string primeClientNonce;
            lock (_sync)
            {
                primeNonce = _primeNonce ?? nonce;
                primeClientNonce = _primeClientNonce ?? sentCnonce;
            }

            // auth-int covers the response body, so read it before verifying.
            byte[] entity = null;
            if (string.Equals(sentQop, "auth-int", StringComparison.OrdinalIgnoreCase))
            {
                entity = response.Content == null
                    ? null
                    : await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }

            string expected = HttpDigestAuthentication.CreateWebDigestRFC7616(
                algorithm, _credentials.UserName, realm, _credentials.Password, false,
                nonce, "", uri, HttpDigestAuthentication.ConvertNCToInt(sentNc), sentCnonce, sentQop,
                entity, primeNonce, primeClientNonce);

            if (!HttpDigestAuthentication.FixedTimeEquals(expected, rspauth))
                throw new AuthenticationException("The device's rspauth did not validate.");

            if (!string.IsNullOrEmpty(nextNonce)) lock (_sync) { _nextNonce = nextNonce; }
        }

        private static string RequestUri(HttpRequestMessage request)
        {
            return request.RequestUri.IsAbsoluteUri ? request.RequestUri.PathAndQuery : request.RequestUri.ToString();
        }

        private static HttpRequestMessage CloneRequest(HttpRequestMessage request, byte[] body)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);

            if (body != null)
            {
                clone.Content = new ByteArrayContent(body);
                if (request.Content != null)
                {
                    foreach (var header in request.Content.Headers)
                        clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            foreach (var header in request.Headers)
            {
                if (string.Equals(header.Key, "Authorization", StringComparison.OrdinalIgnoreCase)) continue;
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Version = request.Version;
            return clone;
        }
    }
}
