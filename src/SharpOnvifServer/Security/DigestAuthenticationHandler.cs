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

namespace SharpOnvifServer.Security
{
    public class DigestAuthenticationHandler : AuthenticationHandler<DigestAuthenticationSchemeOptions>
    {
        private const string CONTEXT_AUTHENTICATE_WEB_DIGEST_RESULT = "authenticateWebDigestResult_07E740A9-0079-42CF-9FDE-510FDAB3A1D9";
        private const string CONTEXT_OPAQUE = "opaque_E768DBA5-D7A8-4735-BD34-FE9F0D65DE54";

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
            // according to the Onvif specification, we must first authenticate the Digest if it's present
            WebDigestAuth webToken = Request.GetSecurityHeaderFromHeaders();
            if (webToken != null)
            {
                if(string.Compare(webToken.Realm, OptionsMonitor.CurrentValue.HttpDigestRealm) != 0)
                {
                    return AuthenticateResult.Fail("HTTP Digest has invalid realm.");
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
                        body = await ReadRequestBodyAsync(body).ConfigureAwait(false);
                    }

                    int authenticateWebDigestResult = await AuthenticateWebDigestAsync(OptionsMonitor.CurrentValue.HttpDigestRealm, Request.Method, webToken, body).ConfigureAwait(false);
                    if (authenticateWebDigestResult == 0)
                    {
                        // now in case the request also contains WsUsernameToken, we must verify it
                        SoapDigestAuth token = await GetSecurityHeaderFromSoapEnvelopeAsync(Request).ConfigureAwait(false);
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

                        var identity = new GenericIdentity(webToken.UserName);
                        var claimsPrincipal = new ClaimsPrincipal(identity);
                        var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
                        return AuthenticateResult.Success(ticket);
                    }
                    else if(authenticateWebDigestResult == HttpDigestAuthentication.ERROR_NONCE_EXPIRED)
                    {
                        // using the Fail(, properties) parameter does not work, the information is lost in ASP.NET
                        Context.Items[CONTEXT_AUTHENTICATE_WEB_DIGEST_RESULT] = authenticateWebDigestResult;
                        return AuthenticateResult.Fail("HTTP Digest nonce has expired.");
                    }
                    else
                    {
                        return AuthenticateResult.Fail("HTTP Digest authentication failed.");
                    }
                }
                catch (Exception ex)
                {
                    return AuthenticateResult.Fail($"HTTP Digest authentication failed: {ex.Message}");
                }
            }
            else
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
                else
                {
                    if (AllowAnonymousAccess(Request.ContentType))
                    {
                        // use Anonymous user
                        var identity = new GenericIdentity(ANONYMOUS_USER);
                        var claimsPrincipal = new ClaimsPrincipal(identity);
                        var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
                        return AuthenticateResult.Success(ticket);
                    }
                }
            }

            return AuthenticateResult.Fail("No authentication found");
        }

        private static bool AllowAnonymousAccess(string contentType)
        {
            // according to the Onvif specification, these functions are in the access class PRE_AUTH and do not require any authentication:
            return contentType != null &&
            (
                contentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetWsdlUrl\"") ||
                contentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetServices\"") ||
                contentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetServiceCapabilities\"") ||
                contentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetCapabilities\"") ||
                contentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetHostname\"") ||
                contentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime\"") ||
                contentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetEndpointReference\"")
            ) && (contentType.Split("action=\"").Count() - 1) == 1;
        }

        private async Task<byte[]> ReadRequestBodyAsync(byte[] body)
        {
            ReadResult requestBodyInBytes = await Request.BodyReader.ReadAsync().ConfigureAwait(false);
            string content = Encoding.UTF8.GetString(requestBodyInBytes.Buffer.FirstSpan);
            Request.BodyReader.AdvanceTo(requestBodyInBytes.Buffer.Start, requestBodyInBytes.Buffer.End);
            body = Encoding.UTF8.GetBytes(content);
            return body;
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
                    if (OptionsMonitor.CurrentValue.WsUsernameTokenMaxTimeDeltaInMilliseconds < 0 || 
                        Math.Abs(DateTime.UtcNow.Subtract(createdDateTime.ToUniversalTime()).TotalMilliseconds) < OptionsMonitor.CurrentValue.WsUsernameTokenMaxTimeDeltaInMilliseconds)
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
                int nonceValidationResult = HttpDigestAuthentication.ValidateServerNonce(
                    NONCE_HASH_ALGORITHM,
                    PREFERRED_SERIALIZATION,
                    webToken.Nonce,
                    HttpDigestAuthentication.ConvertNCToInt(webToken.Nc),
                    DateTimeOffset.UtcNow, 
                    null, 
                    NONCE_SALT_LENGTH,
                    OptionsMonitor.CurrentValue.HttpDigestNonceLifetimeMilliseconds,
                    true);
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

                    if (digest.CompareTo(webToken.Response) == 0)
                    {
                        return 0;
                    }
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

            object authenticateWebDigestResult = Context.Items[CONTEXT_AUTHENTICATE_WEB_DIGEST_RESULT];
            string opaque = Context.Items[CONTEXT_OPAQUE]?.ToString();
            
            if (!string.IsNullOrEmpty(opaque))
            {
                // remove it from the cache, but keep opaque value (use the same session)
                HttpDigestAuthentication.RemoveNoncePrime(opaque);
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
                    OptionsMonitor.CurrentValue.Realm);
            */

            var now = DateTimeOffset.UtcNow;
            var hashingAlgorithms = OptionsMonitor.CurrentValue.HttpDigestHashingAlgorithms == null ? new List<string>() { "MD5" } : OptionsMonitor.CurrentValue.HttpDigestHashingAlgorithms.ToList();
            var allowedQop = OptionsMonitor.CurrentValue.HttpDigestAllowedQop == null ? "auth" : string.Join(", ", OptionsMonitor.CurrentValue.HttpDigestAllowedQop.ToList());
            
            foreach (var algorithm in hashingAlgorithms)
            {
                wwwAuth = HttpDigestAuthentication.CreateWwwAuthenticateRFC7616(
                        NONCE_HASH_ALGORITHM,
                        PREFERRED_SERIALIZATION,
                        now,
                        algorithm,
                        null,
                        HttpDigestAuthentication.CreateNonceSessionSalt(NONCE_SALT_LENGTH),
                        OptionsMonitor.CurrentValue.HttpDigestRealm,
                        opaque,
                        allowedQop,
                        "",
                        OptionsMonitor.CurrentValue.HttpDigestIsUserHashSupported,
                        isStale);
                Response.Headers.Append("WWW-Authenticate", wwwAuth);
            }

            await Context.Response.WriteAsync("You are not logged in via Digest auth").ConfigureAwait(false);
        }

        private static async Task<SoapDigestAuth> GetSecurityHeaderFromSoapEnvelopeAsync(HttpRequest request)
        {
            ReadResult requestBodyInBytes = await request.BodyReader.ReadAsync().ConfigureAwait(false);
            string body = Encoding.UTF8.GetString(requestBodyInBytes.Buffer.FirstSpan);
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
