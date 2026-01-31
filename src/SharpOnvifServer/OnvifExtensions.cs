using CoreWCF.Description;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using SharpOnvifServer.Discovery;
using SharpOnvifServer.Events;
using System;
using System.Collections.Generic;
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
        /// <param name="options">Digest authentication options.</param>
        public static IServiceCollection AddOnvifDigestAuthentication(this IServiceCollection services, Action<DigestAuthenticationSchemeOptions> options = null)
        {
            const string SCHEME_DIGEST = "Digest";

            // all endpoints must have [DisableMustUnderstandValidation] for this to work
            services.AddHttpContextAccessor()
                    .AddSingleton<IServiceBehavior, HttpDigestBehavior>()
                    .AddAuthentication(SCHEME_DIGEST)
                    .AddScheme<DigestAuthenticationSchemeOptions, DigestAuthenticationHandler>(SCHEME_DIGEST, options);

            // CoreWCF cannot have a contract with some endpoints anonymous and some requiring the authentication
            services.AddAuthorization(); // this means we require Digest on all endpoints => Unauthenticated users will have to default to "Anonymous" user account

            return services;
        }

        /// <summary>
        /// Adds a UDP listener for Onvif discovery.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        public static IServiceCollection AddOnvifDiscovery(this IServiceCollection services, OnvifDiscoveryOptions options = null)
        {
            if (options == null)
            {
                // if not specified, fill in the defaults
                options = new OnvifDiscoveryOptions();
                options.Scopes = new System.Collections.Generic.List<string>() {
                  "onvif://www.onvif.org/type/video_encoder",
                  "onvif://www.onvif.org/Profile/Streaming",
                  "onvif://www.onvif.org/Profile/G",
                  "onvif://www.onvif.org/Profile/T"
                };
                options.Types = new System.Collections.Generic.List<OnvifType>()
                {
                    new OnvifType("http://www.onvif.org/ver10/network/wsdl", "NetworkVideoTransmitter"),
                    new OnvifType("http://www.onvif.org/ver10/device/wsdl", "Device")
                };
            }

            services.AddSingleton(options);
            services.AddHostedService<DiscoveryService>();

            return services;
        }

        /// <summary>
        /// Use Onvif.
        /// </summary>
        /// <param name="app"><see cref="WebApplication"/>.</param>
        /// <returns><see cref="WebApplication"/>.</returns>
        public static WebApplication UseOnvif(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                // Onvif Device Manager sends an empty action in the Content-Type header for Event subscription
                //  and instead puts the action inside the Soap Header. CoreWCF expects it in the Content-Type header,
                //  so we have to move it there.
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

        /// <summary>
        /// Use Onvif events.
        /// </summary>
        /// <param name="app"><see cref="WebApplication"/>.</param>
        /// <param name="subscriptionManagerAddress">Onvif Subscription Manager address.</param>
        /// <returns><see cref="WebApplication"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <see cref="subscriptionManagerAddress"> is null.</exception>
        public static WebApplication UseOnvifEvents(this WebApplication app, string subscriptionManagerAddress)
        {
            if (subscriptionManagerAddress == null)
                throw new ArgumentNullException(nameof(subscriptionManagerAddress));

            if (!subscriptionManagerAddress.EndsWith('/'))
                subscriptionManagerAddress = subscriptionManagerAddress + '/';

            app.Use(async (context, next) =>
            {
                // SubscriptionManager should be instantiated per subscription, which is not supported by CoreWCF. The way it works now is:
                //  In EventsImpl Subscribe we create a new SubscriptionManagerImpl and we register it in IEventSubscriptionManager singleton. We get back 
                //   subscription ID, which we return in the SubscriptionReferenceUri as "/onvif/Events/Subscription/<subscriptionID>/". When the client 
                //   calls us again with this ID, we have no service registered for such endpoint, so we use this ASP.NET middleware to strip the 
                //   subscription ID from the request and we route it to the global RouterSubscriptionManagerImpl. We also store the subscription ID 
                //   in the HttpContext so that the RouterSubscriptionManagerImpl can retrieve it. It uses the subscription ID to resolve the registered 
                //   SubscriptionManagerImpl and it forwards all the requests.
                // Note: It would have been easier to use a parameter, e.g. /onvif/Events/Subscription?Idx=<subscriptionID>. However, it seems like SOAP has some 
                //  strict requirements and one of them is to have parameters in a request body and/or headers. 
                if (context.Request.Path != null && context.Request.Path.HasValue && context.Request.Path.Value.Contains(subscriptionManagerAddress))
                {
                    int subscriptionLength = context.Request.Path.Value.IndexOf(subscriptionManagerAddress) + subscriptionManagerAddress.Length;
                    string subscription = context.Request.Path.Value.Substring(subscriptionLength).Trim('/');
                    int subscriptionID = 0;
                    if (int.TryParse(subscription, out subscriptionID))
                    {
                        context.Items[OnvifEvents.ONVIF_SUBSCRIPTION_ID] = subscriptionID;
                        context.Request.Path = new Microsoft.AspNetCore.Http.PathString(context.Request.Path.Value.Substring(0, subscriptionLength));
                    }
                }

                await next(context).ConfigureAwait(false);
            });

            return app;
        }

        public static string GetHttpEndpoint(this IServer server)
        {
            return GetHttpEndpoints(server).FirstOrDefault();
        }

        public static IEnumerable<string> GetHttpEndpoints(this IServer server)
        {
            var addresses = server.Features.Get<IServerAddressesFeature>().Addresses;
            return addresses.Where(x => x.StartsWith("http://"));
        }

        public static string GetHttpsEndpoint(this IServer server)
        {
            return GetHttpsEndpoints(server).FirstOrDefault();
        }

        public static IEnumerable<string> GetHttpsEndpoints(this IServer server)
        {
            var addresses = server.Features.Get<IServerAddressesFeature>().Addresses;
            return addresses.Where(x => x.StartsWith("https://"));
        }
    }
}
