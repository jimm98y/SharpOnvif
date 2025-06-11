using System.Collections.Generic;

namespace SharpOnvifServer.Discovery
{
    public class OnvifDiscoveryMessage
    {
        public List<OnvifType> Types { get; set; }
        public string MessageUuid { get; set; }
    }
}
