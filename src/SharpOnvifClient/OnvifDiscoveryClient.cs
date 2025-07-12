using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace SharpOnvifClient
{
    /// <summary>
    /// Discovers Onvif devices on the network by sending a multicast discovery request. IPv4 only.
    /// </summary>
    public static class OnvifDiscoveryClient
    {
        public const int ONVIF_BROADCAST_TIMEOUT = 4000; // 4s timeout
        public const int ONVIF_DISCOVERY_PORT = 3702;
        public static string OnvifDiscoveryAddress = "239.255.255.250"; // only IPv4 networks are currently supported

        private static readonly SemaphoreSlim _discoverySlim = new SemaphoreSlim(1);

        /// <summary>
        /// Discover ONVIF devices in the local network. Sends broadcast messages to all available IP network interfaces.
        /// </summary>
        /// <param name="onDeviceDiscovered">Callback to be called when a new device is discovered.</param>
        /// <param name="broadcastTimeout"><see cref="ONVIF_BROADCAST_TIMEOUT"/>.</param>
        /// <param name="broadcastPort">Broadcast port - 0 to let the OS choose any free port.</param>
        /// <param name="deviceType">Device type we are searching for.</param>
        /// <returns>A list of discovered devices.</returns>
        public static async Task<IList<OnvifDiscoveryResult>> DiscoverAsync(Action<OnvifDiscoveryResult> onDeviceDiscovered = null, int broadcastTimeout = ONVIF_BROADCAST_TIMEOUT, int broadcastPort = 0, string deviceType = "NetworkVideoTransmitter")
        {
            return await DiscoverAllAsync(onDeviceDiscovered, broadcastTimeout, broadcastPort, deviceType);
        }

        /// <summary>
        /// Discover ONVIF devices in the local network using a given network interface.
        /// </summary>
        /// <param name="ipAddress">IP address of the network interface to use (IP of the host computer on the NIC you want to use for discovery).</param>
        /// <param name="onDeviceDiscovered">Callback to be called when a new device is discovered.</param>
        /// <param name="broadcastTimeout"><see cref="ONVIF_BROADCAST_TIMEOUT"/>.</param>
        /// <param name="broadcastPort">Broadcast port - 0 to let the OS choose any free port.</param>
        /// <param name="deviceType">Device type we are searching for.</param>
        /// <returns>A list of discovered devices.</returns>
        public static async Task<IList<OnvifDiscoveryResult>> DiscoverAsync(string ipAddress, Action<OnvifDiscoveryResult> onDeviceDiscovered = null, int broadcastTimeout = ONVIF_BROADCAST_TIMEOUT, int broadcastPort = 0, string deviceType = "NetworkVideoTransmitter")
        {
            return await DiscoverAllAsync(ipAddress, onDeviceDiscovered, broadcastTimeout, broadcastPort, deviceType);
        }

        /// <summary>
        /// Internal method to discover ONVIF devices in the local network and retrieve detailed information about them.
        /// </summary>
        /// <param name="onDeviceDiscovered"></param>
        /// <param name="broadcastTimeout"></param>
        /// <param name="broadcastPort"></param>
        /// <param name="deviceType"></param>
        /// <returns>A list of discovered devices with make and model.</returns>
        internal static async Task<IList<OnvifDiscoveryResult>> DiscoverAllAsync(Action<OnvifDiscoveryResult> onDeviceDiscovered = null, int broadcastTimeout = ONVIF_BROADCAST_TIMEOUT, int broadcastPort = 0, string deviceType = "NetworkVideoTransmitter")
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            List<Task<IList<OnvifDiscoveryResult>>> discoveryTasks = new List<Task<IList<OnvifDiscoveryResult>>>();

            foreach (NetworkInterface adapter in nics)
            {
                if (!(adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.FastEthernetFx ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet3Megabit ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.GigabitEthernet ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Fddi))
                    continue;

                if (adapter.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (!adapter.SupportsMulticast)
                    continue;

                if (!adapter.Supports(NetworkInterfaceComponent.IPv4))
                    continue;

                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();

                if (adapterProperties.GetIPv4Properties() == null)
                    continue;

                foreach (var ua in adapterProperties.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        byte[] ipAddrBytes = ua.Address.GetAddressBytes();
                        if (ipAddrBytes[0] == 169 && ipAddrBytes[1] == 254)
                            continue; // skip link-local address

                        var discoveryTask = DiscoverAllAsync(ua.Address.ToString(), onDeviceDiscovered, broadcastTimeout, broadcastPort, deviceType);
                        discoveryTasks.Add(discoveryTask);
                    }
                }
            }

            await Task.WhenAll(discoveryTasks);

            return discoveryTasks.Where(x => x.IsCompleted && !x.IsFaulted && !x.IsCanceled).SelectMany(x => x.Result).GroupBy(r => r.Addresses.FirstOrDefault()).Select(g => g.First()).ToList();
        }

        /// <summary>
        /// Internal method to discover ONVIF devices on all network interfaces and retrieve detailed information about them.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="onDeviceDiscovered"></param>
        /// <param name="broadcastTimeout"></param>
        /// <param name="broadcastPort"></param>
        /// <param name="deviceType"></param>
        /// <returns>A list of discovered devices with make and model.</returns>
        internal static async Task<IList<OnvifDiscoveryResult>> DiscoverAllAsync(string ipAddress, Action<OnvifDiscoveryResult> onDeviceDiscovered = null, int broadcastTimeout = ONVIF_BROADCAST_TIMEOUT, int broadcastPort = 0, string deviceType = "NetworkVideoTransmitter")
        {
            if (ipAddress == null)
                throw new ArgumentNullException(nameof(ipAddress));

            await _discoverySlim.WaitAsync();

            string uuid = Guid.NewGuid().ToString().ToLowerInvariant();
            string onvifDiscoveryProbe =
            "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://schemas.xmlsoap.org/ws/2004/08/addressing\">\r\n" +
            "   <s:Header>\r\n" +
            "      <a:Action s:mustUnderstand=\"1\">http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</a:Action>\r\n" +
            "      <a:MessageID>urn:uuid:" + uuid + "</a:MessageID>\r\n" +
            "      <a:ReplyTo>\r\n" +
            "        <a:Address>http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous</a:Address>\r\n" +
            "      </a:ReplyTo>\r\n" +
            "      <a:To>urn:schemas-xmlsoap-org:ws:2005:04:discovery</a:To>\r\n" +
            "   </s:Header>\r\n" +
            "   <s:Body>\r\n" +
            "      <Probe xmlns=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\">\r\n" +
            "         <d:Types xmlns:d=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\" xmlns:dp0=\"http://www.onvif.org/ver10/network/wsdl\">dp0:" + deviceType + "</d:Types>\r\n" +
            "      </Probe>\r\n" +
            "   </s:Body>\r\n" +
            "</s:Envelope>\r\n";

            var results = new List<OnvifDiscoveryResult>();
            var endpoints = new HashSet<string>();
            var cts = new CancellationTokenSource();

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), broadcastPort);
            IPEndPoint multicastEndpoint = new IPEndPoint(IPAddress.Parse(OnvifDiscoveryAddress), ONVIF_DISCOVERY_PORT);

            try
            {
                using (UdpClient client = new UdpClient(endPoint))
                {
                    void ReceiveCallback(IAsyncResult ar)
                    {
                        try
                        {
                            IPEndPoint remote = null;
                            byte[] receiveBytes = client.EndReceive(ar, ref remote);
                            string response = Encoding.UTF8.GetString(receiveBytes);

                            var parsed = ParseDiscoveryResponse(response);

                            if (parsed.Addresses != null && parsed.Addresses.Length > 0)
                            {
                                lock (results)
                                {
                                    if (endpoints.Add(parsed.Addresses.First()))
                                    {
                                        results.Add(parsed);
                                        onDeviceDiscovered?.Invoke(parsed);
                                    }
                                }
                            }
                            // continue receiving
                            if (!cts.IsCancellationRequested)
                                client.BeginReceive(ReceiveCallback, null);
                        }
                        catch
                        {
                            // ignore exceptions on shutdown
                        }
                    }

                    client.BeginReceive(ReceiveCallback, null);

                    byte[] message = Encoding.UTF8.GetBytes(onvifDiscoveryProbe);
                    await client.SendAsync(message, message.Length, multicastEndpoint);

                    await Task.Delay(broadcastTimeout, cts.Token);
                    cts.Cancel();

                    // return a snapshot of the results
                    lock (results)
                    {
                        return results.ToList();
                    }
                }
            }
            finally
            {
                cts.Dispose();
                _discoverySlim.Release();
            }
        }

        private static OnvifDiscoveryResult ParseDiscoveryResponse(string response)
        {
            using (var textReader = new StringReader(response))
            {
                var document = new XPathDocument(textReader);
                var navigator = document.CreateNavigator();

                OnvifDiscoveryResult result = new OnvifDiscoveryResult();
                result.Raw = response;

                // local-name is used to ignore the namespace

                // parse the XAddrs
                var node = navigator.SelectSingleNode("//*[local-name()='XAddrs']/text()");
                if (node != null)
                {
                    string[] addresses = node.Value.Split(' ');
                    result.Addresses = addresses;
                }

                // parse Scopes
                var scopesNode = navigator.SelectSingleNode("//*[local-name()='Scopes']/text()");
                if (scopesNode != null)
                {
                    string allScopes = scopesNode.Value;

                    string[] scopes = allScopes.Split(' ');
                    result.Scopes = scopes;

                    try
                    {
                        foreach (var scope in scopes)
                        {
                            if (scope.StartsWith(SharpOnvifCommon.Discovery.Scopes.MAC, StringComparison.OrdinalIgnoreCase))
                                result.MAC = Uri.UnescapeDataString(scope.Substring(SharpOnvifCommon.Discovery.Scopes.MAC.Length));

                            if (scope.StartsWith(SharpOnvifCommon.Discovery.Scopes.Manufacturer, StringComparison.OrdinalIgnoreCase))
                                result.Manufacturer = Uri.UnescapeDataString(scope.Substring(SharpOnvifCommon.Discovery.Scopes.Manufacturer.Length));

                            if (scope.StartsWith(SharpOnvifCommon.Discovery.Scopes.Hardware, StringComparison.OrdinalIgnoreCase))
                                result.Hardware = Uri.UnescapeDataString(scope.Substring(SharpOnvifCommon.Discovery.Scopes.Hardware.Length));

                            if (scope.StartsWith(SharpOnvifCommon.Discovery.Scopes.Name, StringComparison.OrdinalIgnoreCase))
                                result.Name = Uri.UnescapeDataString(scope.Substring(SharpOnvifCommon.Discovery.Scopes.Name.Length));

                            if (scope.StartsWith(SharpOnvifCommon.Discovery.Scopes.City, StringComparison.OrdinalIgnoreCase))
                                result.City = Uri.UnescapeDataString(scope.Substring(SharpOnvifCommon.Discovery.Scopes.City.Length));

                            if (scope.StartsWith(SharpOnvifCommon.Discovery.Scopes.Country, StringComparison.OrdinalIgnoreCase))
                                result.Country = Uri.UnescapeDataString(scope.Substring(SharpOnvifCommon.Discovery.Scopes.Country.Length));
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine($"{nameof(OnvifDiscoveryClient)}.{nameof(ParseDiscoveryResponse)} failed to parse scopes:\r\n{ex.Message}\r\nContinuing...");
                    }
                }

                return result;
            }
        }
    }
}
