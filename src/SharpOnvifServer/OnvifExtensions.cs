using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SharpOnvifServer.Discovery;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SharpOnvifServer
{
    public static class OnvifExtensions
    {
        /// <summary>
        /// Add Digest authentication handler.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        public static IServiceCollection AddOnvifDigestAuthentication(this IServiceCollection services)
        {
            const string SCHEME_DIGEST = "Digest";

            // all endpoints must have [DisableMustUnderstandValidation] for this to work
            services
                .AddAuthentication(SCHEME_DIGEST)
                .AddScheme<AuthenticationSchemeOptions, DigestAuthenticationHandler>(SCHEME_DIGEST, null);

            // CoreWCF cannot have a contract with some endpoints anonymous and some requiring the authentication
            services.AddAuthorization(); // this means we require Digest on all endpoints

            // Possible workaround would be to not use services.AddAuthorization()/app.UseAuthentication(); und use something like:
            /*
            app.Use(async (context, next) =>
            {
                // TODO: Custom logic here
                if (context.Request.Path.StartsWithSegments("/onvif/device_service"))
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

            return services;
        }

        /// <summary>
        /// Adds a UDP listener for Onvif discovery.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        public static IServiceCollection AddOnvifDiscovery(this IServiceCollection services, OnvifDiscoveryOptions options = null)
        {
            if (options == null)
                options = new OnvifDiscoveryOptions();

            services.AddSingleton(options);
            services.AddHostedService<DiscoveryService>();

            return services;
        }

        public static WebApplication UseOnvif(this WebApplication app)
        {
            // Onvif Device Manager sends an empty action in the Content-Type header for Event subscription
            //  and instead puts the action inside the Soap Header. CoreWCF expects it in the Content-Type header,
            //  so we have to move it there.
            app.Use(async (context, next) =>
            {
                if (context.Request.ContentType != null && !context.Request.ContentType.Contains("action="))
                {
                    ReadResult requestBodyInBytes = await context.Request.BodyReader.ReadAsync().ConfigureAwait(false);
                    string body = Encoding.UTF8.GetString(requestBodyInBytes.Buffer.FirstSpan);
                    context.Request.BodyReader.AdvanceTo(requestBodyInBytes.Buffer.Start, requestBodyInBytes.Buffer.End);

                    XNamespace ns = "http://www.w3.org/2003/05/soap-envelope";
                    var soapEnvelope = XDocument.Parse(body);
                    var headers = soapEnvelope.Descendants(ns + "Header").ToList();

                    foreach (var header in headers)
                    {
                        var actionElement = header.Descendants().FirstOrDefault(x => x.Name.LocalName == "Action");
                        if (actionElement != null)
                        {
                            context.Request.ContentType = $"{context.Request.ContentType}; action=\"{actionElement.Value}\"";
                        }
                    }
                }

                await next(context).ConfigureAwait(false);
            });

            return app;
        }
    }
}
