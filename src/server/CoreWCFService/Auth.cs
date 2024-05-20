using CoreWCF.Dispatcher;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.ObjectModel;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using System.Xml.Linq;
using static CoreWCFService.HttpRequestExtensions;

namespace CoreWCFService
{
    public class DigestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {

        private readonly IUserRepository _userRepository;
        public DigestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IUserRepository userRepository) :
           base(options, logger, encoder)
        {
            _userRepository = userRepository;
        }

        protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string authTicketFromSoapEnvelope = await Request!.GetAuthenticationHeaderFromSoapEnvelope();

            if (authTicketFromSoapEnvelope != null)
            {
                //if (await _userRepository.Authenticate("admin", credentials[1]))
                //{
                //    var identity = new GenericIdentity(credentials[0]);
                //    var claimsPrincipal = new ClaimsPrincipal(identity);
                //    var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
                //    return await Task.FromResult(AuthenticateResult.Success(ticket));
                //}

                var identity = new GenericIdentity("admin");
                var claimsPrincipal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(claimsPrincipal, Scheme.Name);
                return await Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return await Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            string nonce = "4d6d466b596a637a4d7a67364f546335596a4a6c5932513d";
            Response.Headers.Append("WWW-Authenticate", $"Digest qop=\"auth\", realm=\"IP Camera\", nonce=\"{nonce}\", stale=\"FALSE\"");
            Context.Response.WriteAsync("You are not logged in via Digest auth").Wait();
            return Task.CompletedTask;
        }
    }

    public static class HttpRequestExtensions
    {

        public static async Task<string> GetAuthenticationHeaderFromSoapEnvelope(this HttpRequest request)
        {
            ReadResult requestBodyInBytes = await request.BodyReader.ReadAsync();
            string body = Encoding.UTF8.GetString(requestBodyInBytes.Buffer.FirstSpan);
            request.BodyReader.AdvanceTo(requestBodyInBytes.Buffer.Start, requestBodyInBytes.Buffer.End);

            string security = null;

            if (body?.Contains(@"http://www.w3.org/2003/05/soap-envelope") == true)
            {
                XNamespace ns = "http://www.w3.org/2003/05/soap-envelope";
                var soapEnvelope = XDocument.Parse(body);
                var headers = soapEnvelope.Descendants(ns + "Header").ToList();

                foreach (var header in headers)
                {
                    var securityElement = header.Descendants().FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(securityElement?.Value))
                    {
                        security = securityElement.Value;
                        break;
                    }
                }
            }

            return security;
        }

        public interface IUserRepository
        {
            public Task<bool> Authenticate(string username, string password);
        }

        public class UserRepository : IUserRepository
        {
            public Task<bool> Authenticate(string username, string password)
            {
                //TODO: some dummie auth mechanism used here, make something more realistic such as DB user repo lookup or similar
                if (username == "admin" && password == "password")
                {
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DisableMustUnderstandValidationAttribute : Attribute, IServiceBehavior
    {
        void IServiceBehavior.Validate(ServiceDescription description, ServiceHostBase serviceHostBase) { }
        void IServiceBehavior.AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) { }
        void IServiceBehavior.ApplyDispatchBehavior(ServiceDescription description, ServiceHostBase serviceHostBase)
        {
            for (int i = 0; i < serviceHostBase.ChannelDispatchers.Count; i++)
            {
                if (serviceHostBase.ChannelDispatchers[i] is ChannelDispatcher channelDispatcher)
                {
                    foreach (EndpointDispatcher endpointDispatcher in channelDispatcher.Endpoints)
                    {
                        if (endpointDispatcher.IsSystemEndpoint) continue;
                        DispatchRuntime runtime = endpointDispatcher.DispatchRuntime;
                        runtime.ValidateMustUnderstand = false;
                    }
                }
            }
        }
    }
}
