using Microsoft.AspNetCore.Hosting.Server;
using SharpOnvifServer.Events;

namespace OnvifService.Onvif
{
    public class SubscriptionManagerImpl : SubscriptionManagerBase
    {
        private readonly IServer _server;

        public SubscriptionManagerImpl(IServer server)
        {
            _server = server;
        }
    }
}
