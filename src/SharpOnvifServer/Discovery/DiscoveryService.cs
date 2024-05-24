using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="serviceAddress">Public Onvif endpoint service address.</param>
        public OnvifDiscoveryOptions(string serviceAddress)
        {
            ServiceAddresses.Add(serviceAddress);
        }
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

        private UdpClient _udpClient;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _listenerTask;

        private readonly OnvifDiscoveryOptions _options = null;

        public DiscoveryService(OnvifDiscoveryOptions options)
        {
            this._options = options;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // to kill a process owning a port: Get-Process -Id (Get-NetUDPEndpoint -LocalPort 3702).OwningProcess
                _udpClient = new UdpClient();
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, ONVIF_DISCOVERY_PORT));
                _udpClient.JoinMulticastGroup(IPAddress.Parse(OnvifDiscoveryAddress));

                _listenerTask = Task.Run(() =>
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        try
                        {
                            var remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
                            var recvResult = _udpClient.Receive(ref remoteEndpoint);

                            string message = Encoding.UTF8.GetString(recvResult);
                            var parsedMessage = ReadOnvifEndpoint(message);
                            if (parsedMessage != null && IsSearchingOurTypes(_options.Types, parsedMessage.Types)) 
                            {
                                string reply = CreateDiscoveryResponse(_options, parsedMessage.MessageUuid);
                                var replyBytes = Encoding.UTF8.GetBytes(reply);
                                int sentBytes = _udpClient.Client.SendTo(replyBytes, remoteEndpoint);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start broadcast listener: {ex.Message}");
            }
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

        private static string CreateDiscoveryResponse(OnvifDiscoveryOptions options, string discoveryMessageUuid)
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
                    nsPrefixes.Add(type.Item1, GetPrefix(nsPrefixes));
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
                                $"<d:XAddrs>{BuildAddresses(options.ServiceAddresses)}</d:XAddrs>\r\n" +
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

        private static string BuildAddresses(IEnumerable<string> values)
        {
            return string.Join(' ', values);
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
                    string[] parsedTypes = node.Value.Split(new char[] { ' ' });
                    foreach(var type in parsedTypes)
                    {
                        string[] parsedTypeNs = type.Split(new char[] { ':' });
                        requestedTypes.Add(new Tuple<string, string>(node.LookupNamespace(parsedTypeNs[0]), parsedTypeNs[1]));
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
            _udpClient.Dispose();
        }
    }
}
