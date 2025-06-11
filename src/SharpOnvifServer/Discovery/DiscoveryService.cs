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
        private static readonly Random _rnd = new Random();

        public const int ONVIF_DISCOVERY_PORT = 3702;
        public static string OnvifDiscoveryAddress = "239.255.255.250"; // only IPv4 networks are currently supported

        private List<UdpClient> _udpClients = new List<UdpClient>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private List<Task> _listenerTasks=  new List<Task>();

        private readonly OnvifDiscoveryOptions _options = null;
        private readonly IServer _server;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly ILogger<DiscoveryService> _logger;

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

                            string nicIPAddress = ua.Address.ToString();

                            if (!(_options.NetworkInterfaces == null || _options.NetworkInterfaces.Count == 0 || _options.NetworkInterfaces.Contains("0.0.0.0")) && !_options.NetworkInterfaces.Contains(nicIPAddress))
                                continue;

                            try
                            {
                                // to kill a process owning a port: Get-Process -Id (Get-NetUDPEndpoint -LocalPort 3702).OwningProcess
                                var udpClient = new UdpClient();
                                _udpClients.Add(udpClient);

                                udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                                // because of the multicast, we cannot use IPAddress.Any - it would have joined the multicast group only on the default NIC on multihomed system
                                udpClient.Client.Bind(new IPEndPoint(IPAddress.Parse(nicIPAddress), ONVIF_DISCOVERY_PORT)); 
                                udpClient.JoinMulticastGroup(IPAddress.Parse(OnvifDiscoveryAddress));

                                _logger.LogInformation($"DiscoveryService is listening on the {nicIPAddress} network interface");

                                var listenerTask = Task.Run(() =>
                                {
                                    while (!_cts.IsCancellationRequested)
                                    {
                                        try
                                        {
                                            var remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
                                            var recvResult = udpClient.Receive(ref remoteEndpoint);

                                            string message = Encoding.UTF8.GetString(recvResult);
                                            _logger.LogDebug($"Received Discovery request on {nicIPAddress}:\r\n{message}");

                                            var parsedMessage = ReadOnvifEndpoint(message);
                                            if (parsedMessage != null && IsSearchingOurTypes(_options.Types, parsedMessage.Types))
                                            {
                                                string reply = CreateDiscoveryResponse(_options, _server.GetHttpEndpoint(), parsedMessage.MessageUuid);
                                                var replyBytes = Encoding.UTF8.GetBytes(reply);
                                                int sentBytes = udpClient.Client.SendTo(replyBytes, remoteEndpoint);
                                                _logger.LogDebug($"Sent Discovery response on {nicIPAddress}:\r\n{reply}");
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
                                _logger.LogError($"Failed to start broadcast listener: {ex.Message}");
                            }
                        }
                    }
                }
            });
            return Task.CompletedTask;
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

        private static string CreateDiscoveryResponse(OnvifDiscoveryOptions options, string httpUri, string discoveryMessageUuid)
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
            string message =
                $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                $"<env:Envelope " +
                    BuildNamespaces(nsPrefixes) +
                    ">" +
                    $"<env:Header>" +
                        $"<wsadis:MessageID>urn:uuid:{uuid}</wsadis:MessageID>\r\n" +
                        $"<wsadis:RelatesTo>{discoveryMessageUuid}</wsadis:RelatesTo>\r\n" +
                        $"<wsadis:To>http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous</wsadis:To>\r\n" +
                        $"<wsadis:Action>http://schemas.xmlsoap.org/ws/2005/04/discovery/ProbeMatches</wsadis:Action>\r\n" +
                    $"</env:Header>\r\n" +
                    $"<env:Body>" +
                        $"<d:ProbeMatches>" +
                            $"<d:ProbeMatch>" +
                                $"<wsadis:EndpointReference>" +
                                    $"<wsadis:Address>urn:uuid:{uuid}</wsadis:Address>\r\n" +
                                $"</wsadis:EndpointReference>\r\n" +
                                $"<d:Types>{BuildTypes(options, nsPrefixes)}</d:Types>\r\n" +
                                $"<d:Scopes>{BuildScopes(options)}</d:Scopes>\r\n" +
                                $"<d:XAddrs>{BuildAddresses(options, httpUri)}</d:XAddrs>\r\n" +
                                $"<d:MetadataVersion>10</d:MetadataVersion>\r\n" +
                            $"</d:ProbeMatch>\r\n" +
                        $"</d:ProbeMatches>\r\n" +
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

        private static string BuildTypes(OnvifDiscoveryOptions options, Dictionary<string, string> nsPrefixes)
        {
            if (options.Types == null)
                return string.Empty;

            StringBuilder ret = new StringBuilder();
            foreach (var type in options.Types)
            {
                ret.Append($"{nsPrefixes[type.TypeNamespace]}:{type.TypeName}");
            }
            return ret.ToString();
        }

        private static string GetPrefix(Dictionary<string, string> nsPrefixes)
        {
            string prefix = string.Empty;
            const int minLen = 2;

            while (true)
            {
                int num = _rnd.Next(0, 26); // Zero to 25
                char c1 = (char)('a' + num);
                prefix += c1;

                if (prefix.Length >= minLen && !nsPrefixes.Values.Contains(prefix))
                    break;
            }
            return prefix;
        }

        private static string BuildAddresses(OnvifDiscoveryOptions options, string fallbackHttpUri)
        {
            if (options.ServiceAddresses != null && options.ServiceAddresses.Count > 0)
            {
                return string.Join(' ', options.ServiceAddresses);
            }
            else
            {
                // fallback
                return $"{fallbackHttpUri}/onvif/device_service";
            }
        }

        private static string BuildScopes(OnvifDiscoveryOptions options)
        {
            List<string> scopes = options.Scopes;
            if (scopes == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(options.MAC))
                scopes.Add($"onvif://www.onvif.org/MAC/{Uri.EscapeDataString(options.MAC)}");

            if (!string.IsNullOrEmpty(options.Hardware))
                scopes.Add($"onvif://www.onvif.org/hardware/{Uri.EscapeDataString(options.Hardware)}");

            if (!string.IsNullOrEmpty(options.Name))
                scopes.Add($"onvif://www.onvif.org/name/{Uri.EscapeDataString(options.Name)}");

            if (!string.IsNullOrEmpty(options.City))
                scopes.Add($"onvif://www.onvif.org/location/city/{Uri.EscapeDataString(options.City)}");

            return string.Join(' ', scopes);
        }

        private static OnvifDiscoveryMessage ReadOnvifEndpoint(string message)
        {
            if (!message.Contains("http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe"))
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
