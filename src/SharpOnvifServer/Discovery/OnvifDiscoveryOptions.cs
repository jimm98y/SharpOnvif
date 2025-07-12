using System.Collections.Generic;

namespace SharpOnvifServer.Discovery
{
    public class OnvifDiscoveryOptions
    {
        public List<string> NetworkInterfaces { get; set; }
        public List<string> ServiceAddresses { get; set; }
        public List<string> Scopes { get; set; }
        public List<OnvifType> Types { get; set; }
        public string MAC { get; set; }
        public string Manufacturer { get; set; }
        public string Hardware { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }
}
