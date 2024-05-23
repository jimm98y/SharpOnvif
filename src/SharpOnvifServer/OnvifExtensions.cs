using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using SharpOnvifServer.Discovery;

namespace SharpOnvifServer
{
    public static class OnvifExtensions
    {
        public static void AddOnvifDigestAuthentication(this IServiceCollection services)
        {
            const string SCHEME_DIGEST = "Digest";

            // all endpoints must have [DisableMustUnderstandValidation] for this to work
            services
                .AddAuthentication(SCHEME_DIGEST)
                .AddScheme<AuthenticationSchemeOptions, DigestAuthenticationHandler>(SCHEME_DIGEST, null);
            services.AddAuthorization(); // this means we require Digest on all endpoints
        }

        public static void AddOnvifDiscovery(this IServiceCollection services)
        {
            services.AddHostedService<DiscoveryService>();
        }
    }
}
