using System.Net.NetworkInformation;
using System.Linq;

namespace SharpOnvifCommon
{
    public static class NetworkHelpers
    {
        public static string GetIPv4NetworkInterface()
        {
            var nic = NetworkInterface
             .GetAllNetworkInterfaces()
             .FirstOrDefault(i =>
                i.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                i.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

            return nic.GetIPProperties().UnicastAddresses.First(
                x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Address.ToString();
        }
    }
}
