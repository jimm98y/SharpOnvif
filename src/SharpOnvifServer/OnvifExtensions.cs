// SharpOnvif
// Copyright (C) 2026 Lukas Volf
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using SharpOnvifServer.Discovery;
using SharpOnvifServer.Events;
using SharpOnvifServer.Security;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOnvifServer
{
    public static class OnvifExtensions
    {
        /// <summary>
        /// Adds the Onvif Digest authentication handler with its default options.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        public static IServiceCollection AddOnvifDigestAuthentication(this IServiceCollection services)
        {
            // The overloads take no optional arguments, so that calling this with none is not
            // ambiguous between the options object and the configuration callback.
            return services.AddOnvifDigestAuthentication((Action<DigestAuthenticationSchemeOptions>)null);
        }

        /// <summary>
        /// Add Digest authentication handler.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="options">Digest authentication options.</param>
        public static IServiceCollection AddOnvifDigestAuthentication(this IServiceCollection services, DigestAuthenticationSchemeOptions options)
        {
            return services.AddOnvifDigestAuthentication((digestOptions) =>
            {
                if (options != null)
                {
                    digestOptions.Authentication = options.Authentication;
                    digestOptions.HttpDigestQop = Distinct(options.HttpDigestQop);
                    digestOptions.HttpDigestRealm = options.HttpDigestRealm;
                    digestOptions.HttpDigestUserHash = options.HttpDigestUserHash;
                    digestOptions.HttpDigestAlgorithms = Distinct(options.HttpDigestAlgorithms);
                    digestOptions.HttpDigestNonceLifetimeMilliseconds = options.HttpDigestNonceLifetimeMilliseconds;
                    digestOptions.HttpDigestNonceReplayStore = options.HttpDigestNonceReplayStore;
                    digestOptions.PreAuthActions = Distinct(options.PreAuthActions);
                    digestOptions.WsUsernameTokenMaxTimeDeltaInMilliseconds = options.WsUsernameTokenMaxTimeDeltaInMilliseconds;
                }
            });
        }

        /// <summary>
        /// Removes repeated entries while keeping the order, which for the algorithm list is the
        /// order they are offered in.
        /// <para>
        /// Binding configuration onto these options appends to the defaults rather than replacing
        /// them, so an appsettings.json that restates the defaults would otherwise make the device
        /// advertise every algorithm twice.
        /// </para>
        /// </summary>
        private static List<string> Distinct(List<string> values)
        {
            if (values == null) return null;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<string>(values.Count);
            foreach (string value in values)
            {
                if (seen.Add(value)) result.Add(value);
            }

            return result;
        }

        /// <summary>
        /// Add Digest authentication handler.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/></param>
        /// <param name="options">Digest authentication options callback.</param>
        public static IServiceCollection AddOnvifDigestAuthentication(this IServiceCollection services, Action<DigestAuthenticationSchemeOptions> options)
        {
            string scheme = OnvifAuthenticationDefaults.AuthenticationScheme;

            services.AddHttpContextAccessor()
                    .AddAuthentication(scheme)
                    .AddScheme<DigestAuthenticationSchemeOptions, DigestAuthenticationHandler>(scheme, options);

            services.AddAuthorization();

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
                options.Scopes = new List<string>() {
                  "onvif://www.onvif.org/type/video_encoder",
                  "onvif://www.onvif.org/Profile/Streaming",
                  "onvif://www.onvif.org/Profile/G",
                  "onvif://www.onvif.org/Profile/T"
                };
                options.Types = new List<OnvifType>()
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
        /// <remarks>
        /// Kept so that existing startup code keeps compiling. It no longer does anything: the
        /// Onvif endpoint resolves an operation from the Content-Type action parameter, a
        /// wsa:Action SOAP header, or the body element itself, so a client that omits the action
        /// from the Content-Type header - as Onvif Device Manager does when subscribing to events
        /// - is handled without rewriting the request.
        /// </remarks>
        /// <param name="app"><see cref="WebApplication"/>.</param>
        /// <returns><see cref="WebApplication"/>.</returns>
        [Obsolete("No longer required. The Onvif endpoint resolves the action itself; this call can be removed.")]
        public static WebApplication UseOnvif(this WebApplication app)
        {
            return app;
        }

        /// <summary>
        /// Use Onvif events.
        /// </summary>
        /// <remarks>
        /// Kept so that existing startup code keeps compiling. It no longer does anything:
        /// <see cref="Dispatch.OnvifRoutingExtensions.MapOnvifService{TService}"/> accepts the
        /// subscription form of an address as a route of its own, which is what this used to
        /// arrange by rewriting the request path.
        /// <para>
        /// The rewrite could not survive the move off CoreWCF. Endpoint routing runs ahead of
        /// application middleware, so by the time this ran the endpoint had already been chosen
        /// and a subscription address matched nothing.
        /// </para>
        /// </remarks>
        /// <param name="app"><see cref="WebApplication"/>.</param>
        /// <param name="subscriptionManagerAddress">Onvif Subscription Manager address.</param>
        /// <returns><see cref="WebApplication"/>.</returns>
        [Obsolete("No longer required. MapOnvifService routes subscription addresses itself; this call can be removed.")]
        public static WebApplication UseOnvifEvents(this WebApplication app, string subscriptionManagerAddress)
        {
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
