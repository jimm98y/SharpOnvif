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
using System.Diagnostics;
using SharpOnvifCommon.Security;

namespace SharpOnvifServer
{
    public class DigestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IUserRepository _userRepository;

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
            UsernameToken token = await GetSecurityHeaderFromSoapEnvelope(Request);

            if (token != null)
            {
                try
                {
                    if (await Authenticate(token.Username, token.Password.Text, token.Nonce.Text, token.Created))
                    {
                        var identity = new GenericIdentity(token.Username);
                        var claimsPrincipal = new ClaimsPrincipal(identity);
                        var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
                        return AuthenticateResult.Success(ticket);
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return AuthenticateResult.Fail("Failed to authenticate the user.");
                }
            }

            return AuthenticateResult.Fail("Invalid Authorization Header");
        }

        public async Task<bool> Authenticate(string userName, string password, string nonce, string created)
        {
            var user = await _userRepository.GetUser(userName);
            if (user != null)
            {
                string hashedPassword = DigestHelpers.CreateHashedPassword(nonce, created, user.Password);
                DateTime createdDateTime;
                if (DateTime.TryParse(created, out createdDateTime))
                {
                    if (MaxValidTimeDeltaInSeconds < 0 || DateTime.UtcNow.Subtract(createdDateTime).TotalSeconds < MaxValidTimeDeltaInSeconds)
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

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            string nonce = DigestHelpers.CalculateNonce();
            Response.Headers.Append("WWW-Authenticate", $"Digest qop=\"auth\", realm=\"IP Camera\", nonce=\"{nonce}\", stale=\"FALSE\"");
            await Context.Response.WriteAsync("You are not logged in via Digest auth").ConfigureAwait(false);
        }

        public static async Task<UsernameToken> GetSecurityHeaderFromSoapEnvelope(HttpRequest request)
        {
            ReadResult requestBodyInBytes = await request.BodyReader.ReadAsync();
            string body = Encoding.UTF8.GetString(requestBodyInBytes.Buffer.FirstSpan);
            request.BodyReader.AdvanceTo(requestBodyInBytes.Buffer.Start, requestBodyInBytes.Buffer.End);

            UsernameToken security = null;
            if (body?.Contains(@"http://www.w3.org/2003/05/soap-envelope") == true)
            {
                XNamespace ns = "http://www.w3.org/2003/05/soap-envelope";
                var soapEnvelope = XDocument.Parse(body);
                var headers = soapEnvelope.Descendants(ns + "Header").ToList();

                foreach (var header in headers)
                {
                    var securityElement = header.Descendants().FirstOrDefault();
                    if (securityElement != null)
                    {
                        var userNameTokenElement = securityElement.Descendants().FirstOrDefault();
                        if (userNameTokenElement != null)
                        {
                            var serializer = new XmlSerializer(typeof(UsernameToken));
                            using (var str = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(userNameTokenElement.ToString())))
                            {
                                UsernameToken xml = (UsernameToken)serializer.Deserialize(str);
                                security = xml;
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
