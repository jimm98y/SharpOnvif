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

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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

namespace SharpOnvifServer.Discovery
{
    /// <summary>
    /// Onvif discovery implementation.
    /// Workaround until CoreWCF supports the discovery.
    /// </summary>
    public class DiscoveryService : IHostedService
    {
        // One listener runs per network interface, so anything shared between them is reached
        // concurrently. Random is not safe to use that way.
        [ThreadStatic]
        private static Random _rndForThread;

        private static Random Rnd
        {
            get { return _rndForThread ?? (_rndForThread = new Random(Guid.NewGuid().GetHashCode())); }
        }

        /// <summary>
        /// How long to wait before answering a Probe. WS-Discovery asks for a delay chosen at
        /// random up to APP_MAX_DELAY, so that a network of devices answering the same Probe does
        /// not reply in one burst the client then has to absorb.
        /// </summary>
        internal static int NextProbeDelayMilliseconds()
        {
            return Rnd.Next(0, AppMaxDelayMilliseconds + 1);
        }

        /// <summary>
        /// How much of a datagram reaches the debug log. A whole Probe is worth seeing; a
        /// megabyte of whatever a caller chose to send is not.
        /// </summary>
        private const int MaxLoggedDatagramLength = 4096;

        public const int ONVIF_DISCOVERY_PORT = 3702;

        /// <summary>The WS-Discovery namespace, which is also the To of an announcement.</summary>
        internal const string DiscoveryNamespace = "http://schemas.xmlsoap.org/ws/2005/04/discovery";

        /// <summary>
        /// The longest a device waits before answering a Probe. WS-Discovery asks for a delay
        /// chosen at random up to this, so that a network of devices answering the same Probe does
        /// not reply in one burst.
        /// </summary>
        internal const int AppMaxDelayMilliseconds = 500;
        public static string OnvifDiscoveryAddressIPV4 = "239.255.255.250";
        public static string OnvifDiscoveryAddressIPV6 = "ff02::c"; 

        private List<UdpClient> _udpClients = new List<UdpClient>();

        // The multicast group each socket joined, which is where an announcement goes out.
        private readonly List<(UdpClient Client, IPEndPoint Group, string Nic)> _announcers =
            new List<(UdpClient, IPEndPoint, string)>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private List<Task> _listenerTasks = new List<Task>();

        private readonly OnvifDiscoveryOptions _options = null;
        private readonly IServer _server;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<DiscoveryService> _logger;

        private readonly List<Uri> _listeningUris = new List<Uri>();

        public DiscoveryService(
            OnvifDiscoveryOptions options, 
            IServer server, 
            IHostApplicationLifetime hostApplicationLifetime, 
            ILogger<DiscoveryService> logger)
        {
            this._options = options;
            this._server = server;
            this._hostApplicationLifetime = hostApplicationLifetime;
            this._logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            // we need to make sure everything is started and we can access the URL
            _hostApplicationLifetime.ApplicationStarted.Register(() =>
            {
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                List<Task<IList<string>>> discoveryTasks = new List<Task<IList<string>>>();

                _logger.LogInformation($"Starting the DiscoveryService");

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

                            if (!(_options.NetworkInterfaces == null || _options.NetworkInterfaces.Count == 0 || _options.NetworkInterfaces.Contains("0.0.0.0")) && !_options.NetworkInterfaces.Contains(ua.Address.ToString()))
                                continue;
                            
                            Listen(OnvifDiscoveryAddressIPV4, ua.Address);
                        }
                        else if(ua.Address.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            if (ua.Address.IsIPv6LinkLocal)
                                continue;

                            if (!(_options.NetworkInterfaces == null || _options.NetworkInterfaces.Count == 0 || _options.NetworkInterfaces.Contains("::") || _options.NetworkInterfaces.Contains("[::]")) && !_options.NetworkInterfaces.Contains(ua.Address.ToString()))
                                continue;

                            Listen(OnvifDiscoveryAddressIPV6, ua.Address);
                        }
                    }
                }

                // Announced once the sockets are up. Without it a client only learns of the device
                // when it happens to probe, which is why a camera appears in a client minutes
                // after it was switched on.
                Announce(DiscoveryMessageType.Hello);
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// Multicasts a Hello or a Bye on every group the service joined.
        /// </summary>
        private void Announce(DiscoveryMessageType messageType)
        {
            if (!_options.AnnounceOnStartAndStop) return;

            List<(UdpClient Client, IPEndPoint Group, string Nic)> announcers;
            lock (_announcers)
            {
                announcers = _announcers.ToList();
            }

            if (announcers.Count == 0) return;

            string message = CreateDiscoveryMessage(
                messageType, _options, _listeningUris.ToArray(), null, EndpointReferenceOf(_options));
            byte[] bytes = Encoding.UTF8.GetBytes(message);

            foreach (var announcer in announcers)
            {
                try
                {
                    announcer.Client.Client.SendTo(bytes, announcer.Group);
                    _logger.LogInformation($"Announced {messageType} on {announcer.Nic}");
                }
                catch (Exception ex)
                {
                    // One interface failing is not a reason to stay silent on the others, and a
                    // Bye that cannot be sent must not stop the service from shutting down.
                    _logger.LogError($"Failed to announce {messageType} on {announcer.Nic}: {ex.Message}");
                }
            }
        }

        private void Listen(string discoveryAddress, IPAddress nicAddress)
        {
            string nicIPAddress = nicAddress.ToString();

            var httpEndpoints = _server.GetHttpEndpoints();
            var httpsEndpoints = _server.GetHttpsEndpoints();
            var allEndpoints = httpEndpoints.Concat(httpsEndpoints).ToArray();

            foreach (var endpoint in allEndpoints)
            {
                var httpUriBuilder = new UriBuilder(new Uri(endpoint));
                httpUriBuilder.Host = nicIPAddress.ToString();
                _listeningUris.Add(httpUriBuilder.Uri);
            }

            try
            {
                // to kill a process owning a port: Get-Process -Id (Get-NetUDPEndpoint -LocalPort 3702).OwningProcess
                var udpClient = new UdpClient(nicAddress.AddressFamily);
                _udpClients.Add(udpClient);

                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // because of the multicast, we cannot use IPAddress.Any - it would have joined the multicast group only on the default NIC on multihomed system
                udpClient.Client.Bind(new IPEndPoint(IPAddress.Parse(nicIPAddress), ONVIF_DISCOVERY_PORT));

                if (nicAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    IPv6MulticastOption ipv6MulticastOption = new IPv6MulticastOption(IPAddress.Parse(discoveryAddress));
                    IPAddress group = ipv6MulticastOption.Group;
                    long interfaceIndex = ipv6MulticastOption.InterfaceIndex;
                    udpClient.JoinMulticastGroup((int)ipv6MulticastOption.InterfaceIndex, ipv6MulticastOption.Group);

                    // Joining says where to listen. Announcing has to say where to send, or the
                    // host picks an interface by its routing table and a Hello goes out of the
                    // wrong one - or nowhere, with "No route to host".
                    udpClient.Client.SetSocketOption(
                        SocketOptionLevel.IPv6, SocketOptionName.MulticastInterface, (int)interfaceIndex);
                }
                else if(nicAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    udpClient.JoinMulticastGroup(IPAddress.Parse(discoveryAddress), IPAddress.Parse(nicIPAddress));

                    udpClient.Client.SetSocketOption(
                        SocketOptionLevel.IP, SocketOptionName.MulticastInterface, nicAddress.GetAddressBytes());
                }
                else
                {
                    throw new NotSupportedException();
                }

                _logger.LogInformation($"DiscoveryService is listening on the {nicIPAddress} network interface");

                lock (_announcers)
                {
                    _announcers.Add((udpClient,
                        new IPEndPoint(IPAddress.Parse(discoveryAddress), ONVIF_DISCOVERY_PORT),
                        nicIPAddress));
                }

                var listenerTask = Task.Run(() =>
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        try
                        {
                            var remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
                            var recvResult = udpClient.Receive(ref remoteEndpoint);

                            string message = Encoding.UTF8.GetString(recvResult);
                            // Anyone on the network can send this datagram, so it is rendered as
                            // printable text: unescaped, its newlines would read as further log
                            // entries.
                            _logger.LogDebug(
                                $"Received Discovery request on {nicIPAddress}: {UntrustedText.Printable(message, MaxLoggedDatagramLength)}");

                            var parsedMessage = ReadOnvifEndpoint(message);
                            if (parsedMessage != null && IsSearchingOurTypes(_options.Types, parsedMessage.Types))
                            {
                                // WS-Discovery asks a device to wait a random moment before
                                // answering, so that a network of them does not reply to one Probe
                                // in a single burst the client then has to absorb.
                                int delay = NextProbeDelayMilliseconds();
                                if (delay > 0 && !_cts.IsCancellationRequested)
                                    _cts.Token.WaitHandle.WaitOne(delay);

                                if (_cts.IsCancellationRequested) continue;

                                string reply = CreateDiscoveryResponse(_options, _listeningUris.ToArray(), parsedMessage.MessageUuid);
                                var replyBytes = Encoding.UTF8.GetBytes(reply);
                                int sentBytes = udpClient.Client.SendTo(replyBytes, remoteEndpoint);
                                _logger.LogDebug(
                                    $"Sent Discovery response on {nicIPAddress}: {UntrustedText.Printable(reply, MaxLoggedDatagramLength)}");
                            }
                        }
                        catch(SocketException socketEx)
                        {
                            if (socketEx.Message.Contains("WSACancelBlockingCall"))
                            {
                                _logger.LogInformation($"Discovery request on {nicIPAddress} was cancelled.");
                            }
                            else
                            {
                                _logger.LogError($"Failed to process Discovery request on {nicIPAddress}: {socketEx.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Failed to process Discovery request on {nicIPAddress}: {ex.Message}");
                        }
                    }
                });

                _listenerTasks.Add(listenerTask);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to start multicast listener on {nicIPAddress}: {ex.Message}");
            }
        }

        private bool IsSearchingOurTypes(List<OnvifType> types1, List<OnvifType> types2)
        {
            if (types1 == null)
                return false;

            var types1combined = types1.Select(x => $"{x.TypeNamespace}#{x.TypeName}").ToArray();
            var types2combined = types2.Select(x => $"{x.TypeNamespace}#{x.TypeName}").ToArray();
            foreach(var type in types2combined)
            {
                if(types1combined.Contains(type))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The WS-Discovery messages a device sends. All three carry the same description of the
        /// device and differ only in what wraps it, so they are built together.
        /// </summary>
        internal enum DiscoveryMessageType
        {
            /// <summary>Answers a Probe, to the client that sent it.</summary>
            ProbeMatches,
            /// <summary>Announces the device on joining the network.</summary>
            Hello,
            /// <summary>Announces the device leaving it.</summary>
            Bye,
        }

        internal static string CreateDiscoveryResponse(
            OnvifDiscoveryOptions options, IEnumerable<Uri> httpUri, string discoveryMessageUuid)
        {
            return CreateDiscoveryMessage(
                DiscoveryMessageType.ProbeMatches, options, httpUri, discoveryMessageUuid, EndpointReferenceOf(options));
        }

        /// <summary>
        /// The device's own address, which every announcement it makes has to agree on: a client
        /// pairs a Bye with the Hello and the ProbeMatch that named the same endpoint. A fresh one
        /// per message would look like a different device each time.
        /// </summary>
        internal static string EndpointReferenceOf(OnvifDiscoveryOptions options)
        {
            if (!string.IsNullOrEmpty(options.EndpointReference))
                return options.EndpointReference;

            // Stable for as long as the process runs. A device that survives a restart should set
            // EndpointReference from something that survives with it.
            return options.RuntimeEndpointReference;
        }

        internal static string CreateDiscoveryMessage(
            DiscoveryMessageType messageType,
            OnvifDiscoveryOptions options,
            IEnumerable<Uri> httpUri,
            string relatesToMessageUuid,
            string endpointReference)
        {
            Dictionary<string, string> nsPrefixes = new Dictionary<string, string>
            {
                { "http://www.w3.org/2003/05/soap-envelope", "env" },
                { "http://schemas.xmlsoap.org/ws/2005/04/discovery", "d" },
                { "http://schemas.xmlsoap.org/ws/2004/08/addressing", "wsadis" }
            };

            if (options.Types != null && options.Types.Count > 0)
            {
                foreach(var type in options.Types) 
                {
                    nsPrefixes.TryAdd(type.TypeNamespace, GetPrefix(nsPrefixes)); // TryAdd -> support multiple different types from the same namespace
                }
            }

            string uuid = Guid.NewGuid().ToString().ToLowerInvariant();
            string action = DiscoveryNamespace + "/" + messageType.ToString();

            // A Probe is answered to the client that sent it; Hello and Bye are announced to
            // everyone listening on the discovery group.
            string to = messageType == DiscoveryMessageType.ProbeMatches
                ? "http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous"
                : DiscoveryNamespace;

            // Only a reply relates to anything. The id came out of the sender's datagram and this
            // document is built by concatenation, so nothing else is going to escape it.
            string relatesTo = messageType == DiscoveryMessageType.ProbeMatches
                ? $"<wsadis:RelatesTo>{UntrustedText.ForXmlText(relatesToMessageUuid)}</wsadis:RelatesTo>\r\n"
                : string.Empty;

            // Bye says the device is leaving, so it carries the address and nothing else to act on.
            string description =
                $"<wsadis:EndpointReference>" +
                    $"<wsadis:Address>{UntrustedText.ForXmlText(endpointReference, 512)}</wsadis:Address>\r\n" +
                $"</wsadis:EndpointReference>\r\n" +
                (messageType == DiscoveryMessageType.Bye
                    ? string.Empty
                    : $"<d:Types>{BuildTypes(options, nsPrefixes)}</d:Types>\r\n" +
                      $"<d:Scopes>{BuildScopes(options)}</d:Scopes>\r\n" +
                      $"<d:XAddrs>{BuildAddresses(options, httpUri)}</d:XAddrs>\r\n") +
                $"<d:MetadataVersion>{options.MetadataVersion}</d:MetadataVersion>\r\n";

            string body = messageType == DiscoveryMessageType.ProbeMatches
                ? $"<d:ProbeMatches><d:ProbeMatch>{description}</d:ProbeMatch>\r\n</d:ProbeMatches>\r\n"
                : $"<d:{messageType}>{description}</d:{messageType}>\r\n";

            string message =
                $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                $"<env:Envelope " +
                    BuildNamespaces(nsPrefixes) +
                    ">" +
                    $"<env:Header>" +
                        $"<wsadis:MessageID>urn:uuid:{uuid}</wsadis:MessageID>\r\n" +
                        relatesTo +
                        $"<wsadis:To>{to}</wsadis:To>\r\n" +
                        $"<wsadis:Action>{action}</wsadis:Action>\r\n" +
                    $"</env:Header>\r\n" +
                    $"<env:Body>" +
                        body +
                    $"</env:Body>\r\n" +
                $"</env:Envelope>\r\n";
            return message;
        }

        private static string BuildNamespaces(Dictionary<string, string> nsPrefixes)
        {
            StringBuilder ret = new StringBuilder();
            foreach(var nsPrefix in nsPrefixes)
            {
                ret.Append($"xmlns:{nsPrefix.Value}=\"{nsPrefix.Key}\" ");
            }
            return ret.ToString();
        }

        internal static string BuildTypes(OnvifDiscoveryOptions options, Dictionary<string, string> nsPrefixes)
        {
            if (options.Types == null)
                return string.Empty;

            // d:Types is a list of QNames separated by whitespace. Run together, two types read
            // as one name that matches neither, and a client filtering on NetworkVideoTransmitter
            // does not find the device.
            var names = new List<string>();
            foreach (var type in options.Types)
            {
                names.Add($"{nsPrefixes[type.TypeNamespace]}:{type.TypeName}");
            }
            return string.Join(" ", names);
        }

        private static string GetPrefix(Dictionary<string, string> nsPrefixes)
        {
            string prefix = string.Empty;
            const int minLen = 2;

            while (true)
            {
                int num = Rnd.Next(0, 26); // Zero to 25
                char c1 = (char)('a' + num);
                prefix += c1;

                if (prefix.Length >= minLen && !nsPrefixes.Values.Contains(prefix))
                    break;
            }
            return prefix;
        }

        private static string BuildAddresses(OnvifDiscoveryOptions options, IEnumerable<Uri> fallbackHttpUri)
        {
            if (options.ServiceAddresses != null && options.ServiceAddresses.Count > 0)
            {
                return string.Join(' ', options.ServiceAddresses);
            }
            else
            {
                List<string> uris = new List<string>();
                // fallback
                foreach (var uri in fallbackHttpUri)
                {
                    var httpUriBuilder = new UriBuilder(uri);
                    httpUriBuilder.Path = "/onvif/device_service";
                    uris.Add(httpUriBuilder.Uri.ToString());
                }
                return string.Join(' ', uris);
            }
        }

        internal static string BuildScopes(OnvifDiscoveryOptions options)
        {
            // A copy, because the scopes below are added to it - and options bound from
            // configuration need not carry any, so this has to survive there being none.
            List<string> scopes = options.Scopes == null ? new List<string>() : options.Scopes.ToList();

            if (!string.IsNullOrEmpty(options.City))
                scopes.Add($"{SharpOnvifCommon.Discovery.Scopes.City}{Uri.EscapeDataString(options.City)}");

            if (!string.IsNullOrEmpty(options.Country))
                scopes.Add($"{SharpOnvifCommon.Discovery.Scopes.Country}{Uri.EscapeDataString(options.Country)}");

            if (!string.IsNullOrEmpty(options.Hardware))
                scopes.Add($"{SharpOnvifCommon.Discovery.Scopes.Hardware}{Uri.EscapeDataString(options.Hardware)}");

            if (!string.IsNullOrEmpty(options.MAC))
                scopes.Add($"{SharpOnvifCommon.Discovery.Scopes.MAC}{Uri.EscapeDataString(options.MAC)}");

            if (!string.IsNullOrEmpty(options.Manufacturer))
                scopes.Add($"{SharpOnvifCommon.Discovery.Scopes.Manufacturer}{Uri.EscapeDataString(options.Manufacturer)}");

            if (!string.IsNullOrEmpty(options.Name))
                scopes.Add($"{SharpOnvifCommon.Discovery.Scopes.Name}{Uri.EscapeDataString(options.Name)}");

            return string.Join(' ', scopes);
        }

        private static OnvifDiscoveryMessage ReadOnvifEndpoint(string message)
        {
            // The Probe action is a prefix of the ProbeMatches action, so a plain Contains on it
            // also matches another device's reply. Only a Probe is answered here.
            const string ProbeAction = "http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe";
            int probeAction = message.IndexOf(ProbeAction, StringComparison.Ordinal);
            if (probeAction < 0)
                return null;

            int afterAction = probeAction + ProbeAction.Length;
            if (afterAction < message.Length && char.IsLetter(message[afterAction]))
                return null;

            using (var textReader = new StringReader(message))
            {
                var document = new XPathDocument(textReader);
                var navigator = document.CreateNavigator();

                List<OnvifType> requestedTypes = new List<OnvifType>();

                // local-name is used to ignore the namespace
                var node = navigator.SelectSingleNode("//*[local-name()='Types']/text()");
                if (node != null)
                {
                    string[] parsedTypes = node.Value.Split(new char[] { ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    foreach(var type in parsedTypes)
                    {
                        string[] parsedTypeNs = type.Split(new char[] { ':' });
                        requestedTypes.Add(new OnvifType(node.LookupNamespace(parsedTypeNs[0].Trim()), parsedTypeNs[1].Trim()));
                    }
                }

                string uuid = string.Empty;
                node = navigator.SelectSingleNode("//*[local-name()='MessageID']/text()");
                if (node != null)
                {
                    uuid = node.Value;
                }

                if (!string.IsNullOrEmpty(uuid) && requestedTypes.Count > 0)
                    return new OnvifDiscoveryMessage { Types = requestedTypes, MessageUuid = uuid };
                else
                    return null;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Before anything is torn down: a Bye needs the sockets it goes out on, and tells
            // clients the device has gone rather than leaving them to time it out.
            Announce(DiscoveryMessageType.Bye);

            lock (_announcers)
            {
                _announcers.Clear();
            }

            await _cts.CancelAsync();
            _cts.Dispose();

            if (_udpClients.Count > 0)
            {
                foreach (var udpClient in _udpClients)
                {
                    udpClient.Dispose();
                }

                _udpClients.Clear();
            }

            await Task.WhenAll(_listenerTasks);
            _listenerTasks.Clear();
        }
    }
}
