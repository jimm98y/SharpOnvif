using System;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace SharpOnvifServer.Dispatch
{
    /// <summary>
    /// The HTTP request an Onvif operation is running for.
    /// <para>
    /// A service implementation often needs the address it was reached on, to build the absolute
    /// URIs Onvif responses carry: a snapshot URI, a stream URI, a subscription reference. The
    /// endpoint sets this around each call, so it is available without threading an
    /// <see cref="HttpContext"/> through every method.
    /// </para>
    /// <para>
    /// It is set by <see cref="OnvifEndpoint"/> and is only valid inside an operation.
    /// </para>
    /// </summary>
    public static class OnvifOperationContext
    {
        private static readonly AsyncLocal<HttpContext> Context = new AsyncLocal<HttpContext>();

        /// <summary>The request being handled, or null outside an Onvif operation.</summary>
        public static HttpContext Current
        {
            get { return Context.Value; }
        }

        /// <summary>
        /// The absolute URI the request arrived on, which is what a device must echo back in the
        /// addresses it publishes. Null outside an Onvif operation.
        /// </summary>
        public static Uri RequestUri
        {
            get
            {
                HttpContext context = Context.Value;
                if (context == null) return null;

                HttpRequest request = context.Request;
                var builder = new UriBuilder
                {
                    Scheme = request.Scheme,
                    Host = request.Host.Host,
                    Path = request.PathBase.Add(request.Path).ToUriComponent(),
                    Query = request.QueryString.ToUriComponent(),
                };

                if (request.Host.Port.HasValue) builder.Port = request.Host.Port.Value;

                return builder.Uri;
            }
        }

        /// <summary>Establishes the context for the duration of one operation.</summary>
        internal static void Set(HttpContext context)
        {
            Context.Value = context;
        }
    }
}
