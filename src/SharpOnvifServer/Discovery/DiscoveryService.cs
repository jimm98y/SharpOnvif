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
    public class OnvifDiscoveryMessage
    {
        public List<Tuple<string, string>> Types { get; set; }
        public string MessageUuid { get; set; }
    }

    public class OnvifDiscoveryOptions
    {
        public List<string> NetworkInterfaces { get; set; } = new List<string>();

        public List<string> ServiceAddresses { get; set; } = new List<string>();

        public List<string> Scopes { get; set; } = new List<string>()
        {
            "onvif://www.onvif.org/type/video_encoder",
            "onvif://www.onvif.org/Profile/Streaming",
            "onvif://www.onvif.org/Profile/G",
            "onvif://www.onvif.org/Profile/T"
        };

        public List<Tuple<string, string>> Types { get; set; } = new List<Tuple<string, string>>
        {
            new Tuple<string, string>("http://www.onvif.org/ver10/network/wsdl", "NetworkVideoTransmitter"),
            new Tuple<string, string>("http://www.onvif.org/ver10/device/wsdl", "Device")
        };

        public string MAC { get; set; }
        public string Hardware { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
    }

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

        public DiscoveryService(OnvifDiscoveryOptions options, IServer server, IHostApplicationLifetime hostApplicationLifetime, ILogger<DiscoveryService> logger)
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
                    if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
                        continue;

                    if (!(adapter.Supports(NetworkInterfaceComponent.IPv4) || adapter.Supports(NetworkInterfaceComponent.IPv6)))
                        continue;

                    IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                    foreach (var ua in adapterProperties.UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
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
                                                _logger.LogDebug($"Sent Discovery response on {ua.Address.ToString()}:\r\n{reply}");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError($"Failed to process Discovery request on {ua.Address.ToString()}: {ex.Message}");
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

        private bool IsSearchingOurTypes(List<Tuple<string, string>> types1, List<Tuple<string, string>> types2)
        {
            var types1combined = types1.Select(x => $"{x.Item1}#{x.Item2}").ToArray();
            var types2combined = types2.Select(x => $"{x.Item1}#{x.Item2}").ToArray();
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

            if (options.Types.Count > 0)
            {
                foreach(var type in options.Types) 
                {
                    nsPrefixes.TryAdd(type.Item1, GetPrefix(nsPrefixes)); // TryAdd -> support multiple different types from the same namespace
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
                                $"<d:Types>{BuildTypes(options.Types, nsPrefixes)}</d:Types>\r\n" +
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

        private static string BuildTypes(List<Tuple<string, string>> values, Dictionary<string, string> nsPrefixes)
        {
            StringBuilder ret = new StringBuilder();
            foreach (var type in values)
            {
                ret.Append($"{nsPrefixes[type.Item1]}:{type.Item2}");
            }
            return ret.ToString();
        }

        private static string GetPrefix(Dictionary<string, string> nsPrefixes)
        {
            string prefix = "";
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
            if (options.ServiceAddresses.Count > 0)
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

                List<Tuple<string, string>> requestedTypes = new List<Tuple<string, string>>();

                // local-name is used to ignore the namespace
                var node = navigator.SelectSingleNode("//*[local-name()='Types']/text()");
                if (node != null)
                {
                    string[] parsedTypes = node.Value.Split(new char[] { ' ' }).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    foreach(var type in parsedTypes)
                    {
                        string[] parsedTypeNs = type.Split(new char[] { ':' });
                        requestedTypes.Add(new Tuple<string, string>(node.LookupNamespace(parsedTypeNs[0].Trim()), parsedTypeNs[1].Trim()));
                    }
                }

                string uuid = "";
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
