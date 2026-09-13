using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace SharpOnvifServer.Dispatch
{
    /// <summary>Maps Onvif services onto ASP.NET Core endpoints.</summary>
    public static class OnvifRoutingExtensions
    {
        // Endpoints are keyed by path so that several services can be added to one URL. A device
        // that exposes everything under /onvif/device_service is legal and common, and the
        // CoreWCF bindings this replaces could not express it.
        private static readonly Dictionary<IEndpointRouteBuilder, Dictionary<string, OnvifEndpoint>> Endpoints =
            new Dictionary<IEndpointRouteBuilder, Dictionary<string, OnvifEndpoint>>();

        /// <summary>
        /// Publishes an Onvif service implementation at <paramref name="path"/>.
        /// <para>
        /// Call it more than once with the same path to serve several services from one URL; the
        /// SOAP action decides which one handles each request. The implementation type must be
        /// registered in the service collection.
        /// </para>
        /// <para>
        /// A trailing segment is accepted as well, so that an address like
        /// <c>/onvif/Events/PullPointSubscription/3/</c> reaches the service mapped at
        /// <c>/onvif/Events/PullPointSubscription</c>. Onvif addresses a subscription manager that
        /// way, and the segment is published to the implementation as
        /// <see cref="Events.OnvifEvents.ONVIF_SUBSCRIPTION_ID"/>.
        /// </para>
        /// </summary>
        /// <typeparam name="TService">
        /// The implementation, deriving from a generated service base such as
        /// <c>SharpOnvifServer.DeviceMgmt.DeviceBase</c>.
        /// </typeparam>
        public static IEndpointConventionBuilder MapOnvifService<TService>(
            this IEndpointRouteBuilder routes, string path)
            where TService : class
        {
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

            ServiceDispatcher dispatcher = OnvifDispatcherRegistry.For(typeof(TService));
            if (dispatcher == null)
            {
                throw new InvalidOperationException(
                    typeof(TService).FullName + " does not derive from a generated Onvif service base, " +
                    "so there is no dispatcher for it. Derive from a generated base such as DeviceBase.");
            }

            if (!Endpoints.TryGetValue(routes, out var byPath))
            {
                byPath = new Dictionary<string, OnvifEndpoint>(StringComparer.OrdinalIgnoreCase);
                Endpoints[routes] = byPath;
            }

            if (byPath.TryGetValue(path, out OnvifEndpoint existing))
            {
                // The route is already mapped; adding the service to it is enough.
                existing.Add(dispatcher, typeof(TService));
                return new NullConventionBuilder();
            }

            var endpoint = new OnvifEndpoint(path);
            endpoint.Add(dispatcher, typeof(TService));
            byPath[path] = endpoint;

            // The subscription form is a route of its own rather than a path rewrite, because
            // routing runs ahead of application middleware and would have already chosen an
            // endpoint by the time a rewrite could take effect.
            routes.MapPost(path.TrimEnd('/') + "/{" + OnvifEndpoint.SubscriptionRouteValue + "}", endpoint.HandleAsync);

            return routes.MapPost(path, endpoint.HandleAsync);
        }

        /// <summary>Returned when a service joins an endpoint that was already mapped.</summary>
        private sealed class NullConventionBuilder : IEndpointConventionBuilder
        {
            public void Add(Action<EndpointBuilder> convention)
            {
            }
        }
    }
}
