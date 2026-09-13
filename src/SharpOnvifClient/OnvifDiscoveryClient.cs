// SharpOnvif
// Copyright (C) 2026 Lukas Volf
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;

using SharpOnvifCommon;

namespace SharpOnvifClient
{
    /// <summary>
    /// Discovers Onvif devices on the network by sending a multicast discovery request. 
    /// </summary>
    public static class OnvifDiscoveryClient
    {
        public const int ONVIF_MULTICAST_TIMEOUT = 5000; // 5s timeout
        public const int ONVIF_DISCOVERY_PORT = 3702;
        public static string OnvifDiscoveryAddressIPV4 = "239.255.255.250";
        public static string OnvifDiscoveryAddressIPV6 = "ff02::c";

        private static readonly SemaphoreSlim _discoverySlim = new SemaphoreSlim(1);

        /// <summary>
        /// Raised when discovery could not do something on one interface. Discovery carries on -
        /// an interface that cannot carry multicast is ordinary - which is what makes these worth
        /// hearing: a Probe that never left the machine looks exactly like a network with nothing
        /// on it.
        /// </summary>
        public static event EventHandler<OnvifDiscoveryFailureEventArgs> Failed;

        /// <summary>How often <see cref="WaitForDeviceAsync"/> probes while it waits.</summary>
        public static TimeSpan WaitForDeviceProbeInterval { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Waits for a device to appear, for an application that starts before its camera does.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two ways at once, because neither is reliable alone. A device announces itself with a
        /// WS-Discovery Hello when it joins, which is heard the moment it happens - but the
        /// announcement is UDP multicast, so it can be lost, and one sent before this was called
        /// is already gone. So the network is also probed every
        /// <see cref="WaitForDeviceProbeInterval"/>, which finds a device that was already there.
        /// </para>
        /// <para>
        /// Returns as soon as a device matches. Cancel <paramref name="cancellationToken"/> to stop
        /// waiting; there is no timeout, because "wait until my camera is switched on" has no
        /// natural one.
        /// </para>
        /// </remarks>
        /// <param name="matches">
        /// Which device is being waited for, or null for the first one that appears.
        /// </param>
        public static async Task<OnvifDiscoveryResult> WaitForDeviceAsync(
            Func<OnvifDiscoveryResult, bool> matches = null,
            CancellationToken cancellationToken = default(CancellationToken),
            IOnvifLogger logger = null)
        {
            matches = matches ?? (device => true);

            var found = new TaskCompletionSource<OnvifDiscoveryResult>();

            using (var listener = new OnvifDiscoveryListener { Logger = logger })
            using (cancellationToken.Register(() => found.TrySetCanceled(cancellationToken)))
            {
                listener.DeviceAnnounced += (sender, e) =>
                {
                    if (Matches(e.Device, matches, logger)) found.TrySetResult(e.Device);
                };

                listener.Start();

                // Something may have been there all along, or have announced itself in the moment
                // between this being called and the listener being ready.
                while (!found.Task.IsCompleted)
                {
                    try
                    {
                        foreach (var device in await DiscoverAsync(null, 1000, logger: logger).ConfigureAwait(false))
                        {
                            if (Matches(device, matches, logger))
                            {
                                found.TrySetResult(device);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // A probe that fails - no route, an interface going down - is not the end
                        // of the wait. The next one may work, and the Hello may arrive anyway.
                        OnvifDiscoveryFailure.Raise(Failed, null, logger, OnvifDiscoveryOperation.Probe, null, ex);
                    }

                    if (found.Task.IsCompleted) break;

                    Task waited = await Task.WhenAny(
                        found.Task, Task.Delay(WaitForDeviceProbeInterval, cancellationToken)).ConfigureAwait(false);

                    if (waited == found.Task) break;
                }

                return await found.Task.ConfigureAwait(false);
            }
        }

        private static bool Matches(OnvifDiscoveryResult device, Func<OnvifDiscoveryResult, bool> matches, IOnvifLogger logger)
        {
            if (device == null || device.Addresses == null || device.Addresses.Length == 0) return false;

            try
            {
                return matches(device);
            }
            catch (Exception ex)
            {
                // The caller's predicate, on data from the network.
                OnvifDiscoveryFailure.Raise(Failed, null, logger, OnvifDiscoveryOperation.Handler, null, ex);
                return false;
            }
        }

        /// <summary>
        /// Discover ONVIF devices in the local network. Sends multicast messages to all available IP network interfaces.
        /// </summary>
        /// <param name="onDeviceDiscovered">Callback to be called when a new device is discovered.</param>
        /// <param name="multicastTimeout"><see cref="ONVIF_MULTICAST_TIMEOUT"/>.</param>
        /// <param name="multicastPort">Multicast port - 0 to let the OS choose any free port.</param>
        /// <param name="deviceType">Device type we are searching for.</param>
        /// <returns>A list of discovered devices.</returns>
        public static async Task<IList<OnvifDiscoveryResult>> DiscoverAsync(Action<OnvifDiscoveryResult> onDeviceDiscovered = null, int multicastTimeout = ONVIF_MULTICAST_TIMEOUT, int multicastPort = 0, string deviceType = "NetworkVideoTransmitter", IOnvifLogger logger = null)
        {
            return await DiscoverAllAsync(onDeviceDiscovered, multicastTimeout, multicastPort, deviceType, logger);
        }

        /// <summary>
        /// Discover ONVIF devices in the local network using a given network interface.
        /// </summary>
        /// <param name="ipAddress">IP address of the network interface to use (IP of the host computer on the NIC you want to use for discovery).</param>
        /// <param name="onDeviceDiscovered">Callback to be called when a new device is discovered.</param>
        /// <param name="multicastTimeout"><see cref="ONVIF_MULTICAST_TIMEOUT"/>.</param>
        /// <param name="multicastPort">Multicast port - 0 to let the OS choose any free port.</param>
        /// <param name="deviceType">Device type we are searching for.</param>
        /// <returns>A list of discovered devices.</returns>
        public static async Task<IList<OnvifDiscoveryResult>> DiscoverAsync(string ipAddress, Action<OnvifDiscoveryResult> onDeviceDiscovered = null, int multicastTimeout = ONVIF_MULTICAST_TIMEOUT, int multicastPort = 0, string deviceType = "NetworkVideoTransmitter", IOnvifLogger logger = null)
        {
            return await DiscoverAllAsync(ipAddress, onDeviceDiscovered, multicastTimeout, multicastPort, deviceType, logger);
        }

        /// <summary>
        /// Internal method to discover ONVIF devices in the local network and retrieve detailed information about them.
        /// </summary>
        /// <param name="onDeviceDiscovered">Callback to be called when a new device is discovered.</param>
        /// <param name="multicastTimeout"><see cref="ONVIF_MULTICAST_TIMEOUT"/>.</param>
        /// <param name="multicastPort">Multicast port - 0 to let the OS choose any free port.</param>
        /// <param name="deviceType">Device type we are searching for.</param>
        /// <returns>A list of discovered devices with make and model.</returns>
        internal static async Task<IList<OnvifDiscoveryResult>> DiscoverAllAsync(Action<OnvifDiscoveryResult> onDeviceDiscovered = null, int multicastTimeout = ONVIF_MULTICAST_TIMEOUT, int multicastPort = 0, string deviceType = "NetworkVideoTransmitter", IOnvifLogger logger = null)
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            List<Task<IList<OnvifDiscoveryResult>>> discoveryTasks = new List<Task<IList<OnvifDiscoveryResult>>>();

            foreach (NetworkInterface adapter in nics)
            {
                // Not loopback: a Probe cannot be multicast out of it - the send fails with
                // "Can't assign requested address" - and a device on this machine is listening on
                // the real interfaces too, so nothing is lost by not asking here.
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                if (!(adapter.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
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

                if (!(adapter.Supports(NetworkInterfaceComponent.IPv4) || adapter.Supports(NetworkInterfaceComponent.IPv6)))
                    continue;

                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();

                if (adapterProperties.GetIPv4Properties() == null && adapterProperties.GetIPv6Properties() == null)
                    continue;

                foreach (var ua in adapterProperties.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        byte[] ipAddrBytes = ua.Address.GetAddressBytes();
                        if (ipAddrBytes[0] == 169 && ipAddrBytes[1] == 254)
                            continue; // skip link-local address

                        var discoveryTask = DiscoverAllAsync(ua.Address.ToString(), onDeviceDiscovered, multicastTimeout, multicastPort, deviceType, logger);
                        discoveryTasks.Add(discoveryTask);
                    }
                    else if (ua.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        if(ua.Address.IsIPv6LinkLocal)
                            continue; // skip link-local address

                        var discoveryTask = DiscoverAllAsync(ua.Address.ToString(), onDeviceDiscovered, multicastTimeout, multicastPort, deviceType, logger);
                        discoveryTasks.Add(discoveryTask);
                    }
                }
            }

            // Not Task.WhenAll: one interface throwing must not lose the devices the others
            // found, and must not vanish either.
            foreach (var task in discoveryTasks)
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    OnvifDiscoveryFailure.Raise(Failed, null, logger, OnvifDiscoveryOperation.Probe, null, ex);
                }
            }

            return discoveryTasks.Where(x => x.IsCompleted && !x.IsFaulted && !x.IsCanceled).SelectMany(x => x.Result).GroupBy(r => r.Addresses.FirstOrDefault()).Select(g => g.First()).ToList();
        }

        /// <summary>
        /// Internal method to discover ONVIF devices on all network interfaces and retrieve detailed information about them.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="onDeviceDiscovered"></param>
        /// <param name="multicastTimeout"></param>
        /// <param name="multicastPort"></param>
        /// <param name="deviceType"></param>
        /// <returns>A list of discovered devices with make and model.</returns>
        internal static async Task<IList<OnvifDiscoveryResult>> DiscoverAllAsync(string ipAddress, Action<OnvifDiscoveryResult> onDeviceDiscovered = null, int multicastTimeout = ONVIF_MULTICAST_TIMEOUT, int multicastPort = 0, string deviceType = "NetworkVideoTransmitter", IOnvifLogger logger = null)
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

            var nicAddress = IPAddress.Parse(ipAddress);
            string discoveryAddress = nicAddress.AddressFamily == AddressFamily.InterNetwork ? OnvifDiscoveryAddressIPV4 : OnvifDiscoveryAddressIPV6;
            IPEndPoint endPoint = new IPEndPoint(nicAddress, multicastPort);
            IPEndPoint multicastEndpoint = new IPEndPoint(IPAddress.Parse(discoveryAddress), ONVIF_DISCOVERY_PORT);

            try
            {
                using (UdpClient client = new UdpClient(endPoint))
                {
                    if (nicAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        // ff02::c is link-local scope, so there is no route to it until the socket
                        // is told which interface it is on. Without this every IPv6 Probe failed
                        // with "No route to host", and the failure is only written to Debug - so
                        // IPv6 discovery quietly found nothing.
                        int interfaceIndex = FindInterfaceIndex(nicAddress);
                        if (interfaceIndex > 0)
                        {
                            client.Client.SetSocketOption(
                                SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, interfaceIndex);
                        }
                    }

                    void ReceiveCallback(IAsyncResult ar)
                    {
                        try
                        {
                            IPEndPoint remote = null;
                            byte[] receiveBytes = client.EndReceive(ar, ref remote);
                            string response = Encoding.UTF8.GetString(receiveBytes);

                            var parsed = ParseDiscoveryResponse(response, logger);

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
                        catch (Exception ex)
                        {
                            // The socket closing is how this ends, and is not worth reporting.
                            if (!cts.IsCancellationRequested)
                            {
                                OnvifDiscoveryFailure.Raise(
                                    Failed, null, logger, OnvifDiscoveryOperation.Receive, ipAddress, ex);
                            }
                        }
                    }

                    client.BeginReceive(ReceiveCallback, null);

                    byte[] message = Encoding.UTF8.GetBytes(onvifDiscoveryProbe);

                    try
                    {
                        await client.SendAsync(message, message.Length, multicastEndpoint);
                    }
                    catch (System.Net.Sockets.SocketException ex)
                    {
                        // Sending the Probe is the whole of discovery on this interface. Losing it
                        // silently is how an IPv6 Probe that could not leave the machine went
                        // unnoticed for as long as it did.
                        OnvifDiscoveryFailure.Raise(Failed, null, logger, OnvifDiscoveryOperation.Probe, ipAddress, ex);
                    }

                    await Task.Delay(multicastTimeout, cts.Token);
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

        /// <summary>
        /// The index of the interface an address belongs to, or 0 when it cannot be found.
        /// </summary>
        /// <remarks>
        /// An IPv6 multicast group is reached by interface, and an interface is named by index
        /// rather than by address, so the address a caller gives has to be traced back to the
        /// adapter carrying it.
        /// </remarks>
        private static int FindInterfaceIndex(IPAddress address)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up) continue;

                IPInterfaceProperties properties = adapter.GetIPProperties();

                bool carriesIt = false;
                foreach (var unicast in properties.UnicastAddresses)
                {
                    if (unicast.Address.Equals(address)) { carriesIt = true; break; }
                }

                if (!carriesIt) continue;

                try
                {
                    return properties.GetIPv6Properties().Index;
                }
                catch (NetworkInformationException)
                {
                    return 0;
                }
            }

            return 0;
        }

        internal static OnvifDiscoveryResult ParseDiscoveryResponse(string response, IOnvifLogger logger = null)
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
                        logger.Warning("A device's discovery scopes could not be read; the rest of it is still usable.", ex);
                    }
                }

                return result;
            }
        }
    }
}
