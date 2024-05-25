using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Linq;

namespace SharpOnvifServer
{
    public static class AspNetExtensions
    {
        public static string GetHttpEndpoint(this IServer server)
        {
            return server.Features.Get<IServerAddressesFeature>().Addresses.FirstOrDefault(x => x.StartsWith("http://"));
        }

        public static string GetHttpsEndpoint(this IServer server)
        {
            return server.Features.Get<IServerAddressesFeature>().Addresses.FirstOrDefault(x => x.StartsWith("https://"));
        }
    }
}
