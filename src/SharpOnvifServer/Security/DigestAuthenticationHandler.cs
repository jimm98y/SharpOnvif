using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using System;
using SharpOnvifCommon.Security;
using System.Globalization;
using System.IO;

namespace SharpOnvifServer
{
    public class DigestAuthenticationHandler : AuthenticationHandler<DigestAuthenticationSchemeOptions>
    {
        private const int NONCE_SALT_LENGTH = 12;
        private const string NONCE_HASH_ALGORITHM = "SHA-256";

        public const string ANONYMOUS_USER = "Anonymous";

        private readonly IUserRepository _userRepository;

        private class SoapDigestAuth
        {
            public SoapDigestAuth(string username, string password, string nonce, string created)
            {
                UserName = username;
                Password = password;
                Nonce = nonce;
                Created = created;
            }

            public string UserName { get; set; }
            public string Password { get; set; }
            public string Nonce { get; set; }
            public string Created { get; set; }
        }

        public class WebDigestAuth
        {
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
                string userHash)
            {
                Algorithm = string.IsNullOrEmpty(algorithm) ? "" : algorithm;
                UserName = userName ?? throw new ArgumentNullException(nameof(userName));
                Realm = realm ?? throw new ArgumentNullException(nameof(realm));
                Nonce = nonce ?? throw new ArgumentNullException(nameof(nonce));
                Uri = uri ?? throw new ArgumentNullException(nameof(uri));
                Response = response ?? throw new ArgumentNullException(nameof(response));

                Qop = qop;
                CNonce = cnonce;
                Nc = nc;
                UserHash = userHash;
            }

            public string Response { get; }
            public string UserName { get; }
            public string Realm { get; }
            public string Nonce { get; }
            public string Uri { get; }
            public string Algorithm { get; }

            public string Qop { get; }
            public string CNonce { get; }
            public string Nc { get; }
            public string UserHash { get; }

            // The real username after resolving the optional userhash in RFC 7616
            public string RealUserName { get; internal set; }
        }

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
            WebDigestAuth webToken = GetSecurityHeaderFromHeaders(Request);
            if (webToken != null && webToken.Realm == OptionsMonitor.CurrentValue.Realm)
            {
                try
                {
                    byte[] body = null;
                    if (string.Compare("auth-int", webToken.Qop, true) == 0)
                    {
                        ReadResult requestBodyInBytes = await Request.BodyReader.ReadAsync();
                        string content = Encoding.UTF8.GetString(requestBodyInBytes.Buffer.FirstSpan);
                        Request.BodyReader.AdvanceTo(requestBodyInBytes.Buffer.Start, requestBodyInBytes.Buffer.End);
                        body = Encoding.UTF8.GetBytes(content);
                    }

                    if (await AuthenticateWebDigest(OptionsMonitor.CurrentValue.Realm, Request.Method, webToken, body))
                    {
                        // now in case the request also contains WsUsernameToken, we must verify it
                        SoapDigestAuth token = await GetSecurityHeaderFromSoapEnvelope(Request);
                        if (token != null)
                        {
                            try
                            {
                                if (await AuthenticateSoapDigest(token.UserName, token.Password, token.Nonce, token.Created))
                                {
                                    if (string.Compare(token.UserName, webToken.RealUserName, false, CultureInfo.InvariantCulture) != 0)
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

                        var identity = new GenericIdentity(webToken.UserName);
                        var claimsPrincipal = new ClaimsPrincipal(identity);
                        var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
                        return AuthenticateResult.Success(ticket);
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
                SoapDigestAuth token = await GetSecurityHeaderFromSoapEnvelope(Request);
                if (token != null)
                {
                    try
                    {
                        if (await AuthenticateSoapDigest(token.UserName, token.Password, token.Nonce, token.Created))
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
                    // according to the Onvif specification, these functions are in the access class PRE_AUTH and do not require any authentication:
                    if (
                        Request.ContentType != null && 
                        (
                            Request.ContentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetWsdlUrl\"") ||
                            Request.ContentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetServices\"") ||
                            Request.ContentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetServiceCapabilities\"") ||
                            Request.ContentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetCapabilities\"") ||
                            Request.ContentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetHostname\"") ||
                            Request.ContentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime\"") ||
                            Request.ContentType.Contains("action=\"http://www.onvif.org/ver10/device/wsdl/GetEndpointReference\"")
                        ) && 
                        (
                            Request.ContentType.Split("action=\"").Count() - 1) == 1
                        )
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

        public async Task<bool> AuthenticateSoapDigest(string userName, string digest, string nonce, string created)
        {
            var user = await _userRepository.GetUser(userName);
            if (user != null)
            {
                if (user.IsPasswordAlreadyHashed)
                    throw new NotSupportedException("WsUsernameToken is not compatible with pre-hashed (HA1) passwords.");

                string calculatedDigest = WsDigestAuthentication.CreateSoapDigest(nonce, created, user.Password);                
                DateTime createdDateTime;

                // All times MUST be in UTC format as specified https://docs.oasis-open.org/wss-m/wss/v1.1.1/os/wss-SOAPMessageSecurity-v1.1.1-os.html
                if (DateTime.TryParse(created, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out createdDateTime))
                {
                    if (OptionsMonitor.CurrentValue.MaxValidTimeDeltaInSeconds < 0 || 
                        Math.Abs(DateTime.UtcNow.Subtract(createdDateTime.ToUniversalTime()).TotalSeconds) < OptionsMonitor.CurrentValue.MaxValidTimeDeltaInSeconds)
                    {
                        if (calculatedDigest.CompareTo(digest) == 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private async Task<bool> AuthenticateWebDigest(string realm, string method, WebDigestAuth webToken, byte[] body = null)
        {
            UserInfo user = null;

            if (!string.IsNullOrEmpty(webToken.UserHash) && webToken.UserHash.ToUpperInvariant() == "TRUE")
            {
                user = await _userRepository.GetUserByHash(webToken.Algorithm, webToken.UserName, webToken.Realm);
                webToken.RealUserName = user.UserName;
            }
            else
            {
                user = await _userRepository.GetUser(webToken.UserName);
                webToken.RealUserName = user.UserName;
            }

            if (user != null)
            {
                if (DigestAuthentication.ValidateServerNonce(NONCE_HASH_ALGORITHM, BinarySerializationType.Hex, webToken.Nonce, DigestAuthentication.ConvertNCToInt(webToken.Nc), DateTimeOffset.UtcNow, null, NONCE_SALT_LENGTH) == 0)
                {
                    string digest;

                    // legacy, not officially supported by Onvif Core specs
                    /*
                    digest = DigestAuthentication.CreateWebDigestRFC2069(
                        webToken.Algorithm, 
                        webToken.RealUserName, 
                        realm, 
                        user.Password,
                        user.IsPasswordAlreadyHashed,
                        webToken.Nonce, 
                        method,
                        webToken.Uri);
                    */

                    digest = DigestAuthentication.CreateWebDigestRFC7616(
                        webToken.Algorithm,
                        webToken.RealUserName,
                        realm,
                        user.Password,
                        user.IsPasswordAlreadyHashed,
                        webToken.Nonce,
                        method,
                        webToken.Uri,
                        DigestAuthentication.ConvertNCToInt(webToken.Nc),
                        webToken.CNonce,
                        webToken.Qop,
                        body);

                    if (digest.CompareTo(webToken.Response) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static WebDigestAuth GetSecurityHeaderFromHeaders(HttpRequest request)
        {
            if(request.Headers.Authorization.Count > 0)
            {
                string auth = request.Headers.Authorization[0];
                if(auth.StartsWith("Digest ", StringComparison.OrdinalIgnoreCase))
                {
                    string realm = DigestAuthentication.GetValueFromHeader(auth, "realm", true);
                    string algorithm = DigestAuthentication.GetValueFromHeader(auth, "algorithm", true) ?? "";
                    string userName = DigestAuthentication.GetValueFromHeader(auth, "username", true);

                    // read username*
                    if (string.IsNullOrEmpty(userName))
                    {
                        userName = DigestAuthentication.GetValueFromHeader(auth, "username\\*", false); // username*
                        if(!string.IsNullOrEmpty(userName) && userName.StartsWith("UTF-8''"))
                        {
                            userName = Uri.UnescapeDataString(userName.Substring("UTF-8''".Length));
                        }
                    }

                    string response = DigestAuthentication.GetValueFromHeader(auth, "response", true);
                    string nonce = DigestAuthentication.GetValueFromHeader(auth, "nonce", true);
                    string uri = DigestAuthentication.GetValueFromHeader(auth, "uri", true);

                    // some implementations put quotes around qop (ODM)
                    string qop = DigestAuthentication.GetValueFromHeader(auth, "qop", false);
                    if(!string.IsNullOrEmpty(qop) && qop.Contains("\""))
                    {
                        // Note that this is against RFC 7616 that says: For historical reasons, a sender MUST NOT
                        //  generate the quoted string syntax for the following parameters: qop and nc
                        qop = qop.Replace("\"", "");
                    }

                    string cnonce = DigestAuthentication.GetValueFromHeader(auth, "cnonce", true);
                    string nc = DigestAuthentication.GetValueFromHeader(auth, "nc", false);
                    if (!string.IsNullOrEmpty(nc) && nc.Contains("\""))
                    {
                        // Note that this is against RFC 7616 that says: For historical reasons, a sender MUST NOT
                        //  generate the quoted string syntax for the following parameters: qop and nc
                        nc = nc.Replace("\"", "");
                    }

                    string userHash = DigestAuthentication.GetValueFromHeader(auth, "userhash", false);

                    return new WebDigestAuth(algorithm, userName, realm, nonce, uri, response, qop, cnonce, nc, userHash);
                }
            }
            return null;
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;

            string wwwAuth;

            // legacy, not officially supported by Onvif Core spec
            /*
            wwwAuth = DigestAuthentication.CreateWwwAuthenticateRFC2069(
                    NONCE_HASH_ALGORITHM,
                    BinarySerializationType.Hex, 
                    DateTimeOffset.UtcNow, 
                    "MD5",
                    null, 
                    DigestAuthentication.CreateNonceSessionSalt(NONCE_SALT_LENGTH), 
                    OptionsMonitor.CurrentValue.Realm);
            */

            byte[] salt = DigestAuthentication.CreateNonceSessionSalt(NONCE_SALT_LENGTH);
            var now = DateTimeOffset.UtcNow;

            var hashingAlgorithms = OptionsMonitor.CurrentValue.HashingAlgorithms == null ? new System.Collections.Generic.List<string>() { "MD5" } : OptionsMonitor.CurrentValue.HashingAlgorithms.ToList();
            foreach (var algorithm in hashingAlgorithms)
            {
                wwwAuth = DigestAuthentication.CreateWwwAuthenticateRFC7616(
                        NONCE_HASH_ALGORITHM,
                        BinarySerializationType.Hex,
                        now,
                        algorithm,
                        null,
                        salt,
                        OptionsMonitor.CurrentValue.Realm,
                        "00000000",
                        "auth, auth-int",
                        "",
                        true);
                Response.Headers.Append("WWW-Authenticate", wwwAuth);
            }

            await Context.Response.WriteAsync("You are not logged in via Digest auth").ConfigureAwait(false);
        }

        private static async Task<SoapDigestAuth> GetSecurityHeaderFromSoapEnvelope(HttpRequest request)
        {
            ReadResult requestBodyInBytes = await request.BodyReader.ReadAsync();
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
