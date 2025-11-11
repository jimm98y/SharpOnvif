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
using System.Text.RegularExpressions;
using System.Globalization;

namespace SharpOnvifServer
{
    public class DigestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string ANONYMOUS_USER = "Anonymous";

        private readonly IUserRepository _userRepository;

        public string Realm { get; set; } = "IP Camera";

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

        private class WebDigestAuth
        {
            public WebDigestAuth(string userName, string response, string nonce, string uri)
            {
                UserName = userName;
                Response = response;
                Nonce = nonce;
                Uri = uri;
            }

            public string UserName { get; }
            public string Response { get; }
            public string Nonce { get; }
            public string Uri { get; }
        }

        /// <summary>
        /// Maximum allowed time difference in between the client and the server in seconds.
        /// </summary>
        protected int MaxValidTimeDeltaInSeconds { get; set; } = 300;

        public DigestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
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
            WebDigestAuth webToken = await GetSecurityHeaderFromHeaders(Request);
            if (webToken != null)
            {
                try
                {
                    if (await AuthenticateWebDigest(webToken.UserName, Realm, webToken.Response, webToken.Nonce, Request.Method, webToken.Uri))
                    {
                        // now in case the request also contains WsUsernameToken, we must verify it
                        SoapDigestAuth token = await GetSecurityHeaderFromSoapEnvelope(Request);
                        if (token != null)
                        {
                            try
                            {
                                if (await AuthenticateSoapDigest(token.UserName, token.Password, token.Nonce, token.Created))
                                {
                                    if (string.Compare(token.UserName, webToken.UserName, false, CultureInfo.InvariantCulture) != 0)
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

        public async Task<bool> AuthenticateSoapDigest(string userName, string password, string nonce, string created)
        {
            var user = await _userRepository.GetUser(userName);
            if (user != null)
            {
                string hashedPassword = DigestHelpers.CreateSoapDigest(nonce, created, user.Password);
                DateTimeOffset createdDateTime;
                if (DateTimeOffset.TryParse(created, out createdDateTime))
                {
                    if (MaxValidTimeDeltaInSeconds < 0 || DateTimeOffset.UtcNow.Subtract(createdDateTime).TotalSeconds < MaxValidTimeDeltaInSeconds)
                    {
                        if (hashedPassword.CompareTo(password) == 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private async Task<bool> AuthenticateWebDigest(string userName, string realm, string response, string nonce, string method, string uri)
        {
            var user = await _userRepository.GetUser(userName);
            if (user != null)
            {
                string digest = DigestHelpers.CreateWebDigestRFC2069(userName, realm, user.Password, nonce, method, uri);
                if (digest.CompareTo(response) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private Task<WebDigestAuth> GetSecurityHeaderFromHeaders(HttpRequest request)
        {
            if(request.Headers.Authorization.Count > 0)
            {
                string auth = request.Headers.Authorization[0];
                if(auth.StartsWith("Digest ") && GetValueFromHeader(auth, "realm") == Realm)
                {
                    string userName = GetValueFromHeader(auth, "username");
                    string nonce = GetValueFromHeader(auth, "nonce");
                    string response = GetValueFromHeader(auth, "response");
                    string uri = GetValueFromHeader(auth, "uri");
                    return Task.FromResult(new WebDigestAuth(userName, response, nonce, uri));
                }
            }
            return Task.FromResult((WebDigestAuth)null);
        }

        private static string GetValueFromHeader(string header, string key)
        {
            var regHeader = new Regex($@"{key}=""([^""]*)""");
            var matchHeader = regHeader.Match(header);

            if (matchHeader.Success) 
                return matchHeader.Groups[1].Value;

            throw new Exception($"Header {key} not found");
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            string nonce = DigestHelpers.CalculateNonce();

            // RFC 2069
            // TODO: RFC 2617
            Response.Headers.Append("WWW-Authenticate", $"Digest realm=\"{Realm}\", nonce=\"{nonce}\", stale=\"FALSE\"");
            
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
