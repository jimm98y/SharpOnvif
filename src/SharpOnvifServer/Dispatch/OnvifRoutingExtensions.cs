using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace SharpOnvifServer.Dispatch
{
    /// <summary>Maps Onvif services onto ASP.NET Core endpoints.</summary>
    public static class OnvifRoutingExtensions
    {
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
            where TService : class, IDispatchedService
        {
            if (routes == null) throw new ArgumentNullException(nameof(routes));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

            // The service says what routes to it, so there is nothing to look up and nothing to
            // get wrong: a type that is not a generated service does not satisfy the constraint,
            // and the compiler says so where it is mapped.
            ServiceDispatcher dispatcher = TService.Dispatcher;

            Dictionary<string, OnvifEndpoint> byPath = MappedPathsOf(routes).ByPath;

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

        /// <summary>The paths already mapped on this builder, creating the record on first use.</summary>
        private static MappedPaths MappedPathsOf(IEndpointRouteBuilder routes)
        {
            foreach (EndpointDataSource source in routes.DataSources)
            {
                if (source is MappedPaths mapped) return mapped;
            }

            var added = new MappedPaths();
            routes.DataSources.Add(added);
            return added;
        }

        /// <summary>
        /// What has been mapped where, kept on the builder that was mapped rather than in a static
        /// of its own.
        /// </summary>
        /// <remarks>
        /// A static keyed by builder was two faults in one. Applications built at the same time
        /// shared it with nothing between them, and a concurrent write left the dictionary
        /// corrupted - which is a race in the library, not only in a test that starts several
        /// hosts at once. It also held every builder and every endpoint it had ever been given for
        /// the life of the process, because nothing ever removed an application that had finished
        /// being built.
        /// <para>
        /// Routing already keeps a per-builder collection, so the record goes there and lives
        /// exactly as long as the application it describes. This one contributes no endpoints of
        /// its own and never changes: it is somewhere to keep the map and nothing more. Two
        /// threads mapping onto one builder are still the caller's business, the same way they are
        /// for the MapPost below.
        /// </para>
        /// </remarks>
        private sealed class MappedPaths : EndpointDataSource
        {
            // Endpoints are keyed by path so that several services can be added to one URL. A
            // device that exposes everything under /onvif/device_service is legal and common, and
            // the CoreWCF bindings this replaces could not express it.
            public Dictionary<string, OnvifEndpoint> ByPath { get; } =
                new Dictionary<string, OnvifEndpoint>(StringComparer.OrdinalIgnoreCase);

            public override IReadOnlyList<Endpoint> Endpoints { get { return Array.Empty<Endpoint>(); } }

            public override IChangeToken GetChangeToken() { return NeverChanges.Instance; }

            /// <summary>A change token for a data source that has nothing to announce.</summary>
            private sealed class NeverChanges : IChangeToken, IDisposable
            {
                public static readonly NeverChanges Instance = new NeverChanges();

                public bool HasChanged { get { return false; } }

                public bool ActiveChangeCallbacks { get { return false; } }

                public IDisposable RegisterChangeCallback(Action<object> callback, object state)
                {
                    return this;
                }

                public void Dispose()
                {
                }
            }
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
