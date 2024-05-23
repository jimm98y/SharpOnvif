using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using SharpOnvifServer.Discovery;

namespace SharpOnvifServer
{
    public static class OnvifExtensions
    {
        /// <summary>
        /// Add Digest authentication handler.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        public static void AddOnvifDigestAuthentication(this IServiceCollection services)
        {
            const string SCHEME_DIGEST = "Digest";

            // all endpoints must have [DisableMustUnderstandValidation] for this to work
            services
                .AddAuthentication(SCHEME_DIGEST)
                .AddScheme<AuthenticationSchemeOptions, DigestAuthenticationHandler>(SCHEME_DIGEST, null);

            // CoreWCF cannot have a contract with some endpoints anonymous and some requiring the authentication
            services.AddAuthorization(); // this means we require Digest on all endpoints

            // Possible workaround would be to not use app.UseAuthentication(); und use something like:
            /*
            app.Use(async (context, next) =>
            {
                // TODO: Custom logic here
                if (context.Request.Path.StartsWithSegments("/Service.svc"))
                {
                    var authResult = await context.AuthenticateAsync("Digest");
                    if (authResult.None)
                    {
                        await context.ChallengeAsync("Digest");
                        return;
                    }
                }
                await next(context);
            });
            */
        }

        /// <summary>
        /// Adds a UDP listener for Onvif discovery.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        public static void AddOnvifDiscovery(this IServiceCollection services)
        {
            services.AddHostedService<DiscoveryService>();
        }
    }
}
