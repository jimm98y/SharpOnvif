using System.Net.NetworkInformation;
using System.Linq;

namespace SharpOnvifCommon
{
    public static class NetworkHelpers
    {
        private static NetworkInterface GetPrimaryNetworkInterface()
        {
            return NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(
                i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                i.NetworkInterfaceType != NetworkInterfaceType.Tunnel
            );
        }

        public static string GetIPv4NetworkInterface()
        {
            var nic = GetPrimaryNetworkInterface();
            return nic.GetIPProperties().UnicastAddresses.FirstOrDefault(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.Address.ToString();
        }

        public static string GetIPv4NetworkInterfaceDns()
        {
            var nic = GetPrimaryNetworkInterface();
            return nic.GetIPProperties().DnsAddresses.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();
        }

        public static string GetPrimaryNetworkInterfaceMAC()
        {
            var nic = GetPrimaryNetworkInterface();
            return nic.GetPhysicalAddress().ToString();
        }

        public static string GetIPv4NTPAddress(string ntp = "time.windows.com")
        {
            return System.Net.Dns.GetHostEntry(ntp).AddressList.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();
        }

        public static string GetIPv4NetworkInterfaceGateway()
        {
            var nic = GetPrimaryNetworkInterface();
            return nic.GetIPProperties().GatewayAddresses.FirstOrDefault(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Address?.ToString();
        }
    }
}
