using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace OnvifService.Discovery
{
    /// <summary>
    /// Onvif discovery implementation.
    /// Workaround until CoreWCF supports the discovery.
    /// </summary>
    public class DiscoveryService : IHostedService
    {
        public const int ONVIF_DISCOVERY_PORT = 3702;
        public static string OnvifDiscoveryAddress = "239.255.255.250"; // only IPv4 networks are currently supported

        private UdpClient _udpClient;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Task _listenerTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _udpClient = new UdpClient();
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udpClient.Client.Bind(new IPEndPoint(System.Net.IPAddress.Any, ONVIF_DISCOVERY_PORT));
                _udpClient.JoinMulticastGroup(System.Net.IPAddress.Parse(OnvifDiscoveryAddress));

                _listenerTask = Task.Run(() =>
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        try
                        {
                            var remoteEndpoint = new IPEndPoint(System.Net.IPAddress.Any, 0);
                            var recvResult = _udpClient.Receive(ref remoteEndpoint);

                            string message = Encoding.UTF8.GetString(recvResult);
                            var parsedMessage = ReadOnvifEndpoint(message);
                            if (parsedMessage != null) // TODO: valid types...
                            {
                                // reply
                                string reply = CreateDiscoveryResponse(parsedMessage.Types, parsedMessage.MessageUuid, "http://localhost:5000/device_service");
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

        private static string CreateDiscoveryResponse(string searchedType, string discoveryMessageUuid, string serviceEndpoints)
        {
            // TODO: searchedType and namespaces
            string uuid = Guid.NewGuid().ToString().ToLowerInvariant();
            string message =
                $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                $"<env:Envelope xmlns:env=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:d=\"http://schemas.xmlsoap.org/ws/2005/04/discovery\" " +
                $"xmlns:wsadis=\"http://schemas.xmlsoap.org/ws/2004/08/addressing\" xmlns:dn=\"http://www.onvif.org/ver10/network/wsdl\" " +
                $"xmlns:tds=\"http://www.onvif.org/ver10/device/wsdl\">" +
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
                                $"<d:Types>dn:NetworkVideoTransmitter tds:Device</d:Types>\r\n" + // TODO
                                $"<d:Scopes>" +
                                    $"onvif://www.onvif.org/type/video_encoder " +
                                    $"onvif://www.onvif.org/Profile/Streaming " +
                                    $"onvif://www.onvif.org/Profile/G " +
                                    $"onvif://www.onvif.org/Profile/T " +
                                    $"onvif://www.onvif.org/MAC/00:1c:42:46:9a:0e " + // TODO
                                    $"onvif://www.onvif.org/hardware/Test " +
                                    $"onvif://www.onvif.org/name/LukasVolf " + // TODO
                                    $"onvif://www.onvif.org/location/city/Holysov" + // TODO
                                $"</d:Scopes>\r\n" +
                                $"<d:XAddrs>{serviceEndpoints}</d:XAddrs>\r\n" +
                                $"<d:MetadataVersion>10</d:MetadataVersion>\r\n" +
                            $"</d:ProbeMatch>\r\n" +
                        $"</d:ProbeMatches>\r\n" +
                    $"</env:Body>\r\n" +
                $"</env:Envelope>\r\n";
            return message;
        }

        public class OnvifDiscoveryMessage
        {
            public string Types { get; set; }
            public string MessageUuid { get; set; }
        }

        private static OnvifDiscoveryMessage ReadOnvifEndpoint(string message)
        {
            if (!message.Contains("http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe"))
                return null;

            using (var textReader = new StringReader(message))
            {
                var document = new XPathDocument(textReader);
                var navigator = document.CreateNavigator();

                string types = "";

                // local-name is used to ignore the namespace
                var node = navigator.SelectSingleNode("//*[local-name()='Types']/text()");
                if (node != null)
                {
                    types = node.Value;
                }

                string uuid = "";
                node = navigator.SelectSingleNode("//*[local-name()='MessageID']/text()");
                if (node != null)
                {
                    uuid = node.Value;
                }

                if (!string.IsNullOrEmpty(uuid) && !string.IsNullOrEmpty(types))
                    return new OnvifDiscoveryMessage { Types = types, MessageUuid = uuid };
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

    /*
            <s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope" xmlns:a="http://schemas.xmlsoap.org/ws/2004/08/addressing">
                <s:Header>
                    <a:Action s:mustUnderstand="1">http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</a:Action>
                    <a:MessageID>uuid:537d141e-1309-47bf-927e-1a42727e262d</a:MessageID>
                    <a:ReplyTo>
                        <a:Address>http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous</a:Address>
                    </a:ReplyTo>
                    <a:To s:mustUnderstand="1">urn:schemas-xmlsoap-org:ws:2005:04:discovery</a:To>
                </s:Header>
                <s:Body>
                    <Probe xmlns="http://schemas.xmlsoap.org/ws/2005/04/discovery">
                        <d:Types xmlns:d="http://schemas.xmlsoap.org/ws/2005/04/discovery" xmlns:dp0="http://www.onvif.org/ver10/device/wsdl">dp0:Device</d:Types>
                    </Probe>
                </s:Body>
            </s:Envelope>

            <?xml version="1.0" encoding="UTF-8"?>
            <env:Envelope xmlns:env="http://www.w3.org/2003/05/soap-envelope" xmlns:d="http://schemas.xmlsoap.org/ws/2005/04/discovery" xmlns:wsadis="http://schemas.xmlsoap.org/ws/2004/08/addressing" xmlns:dn="http://www.onvif.org/ver10/network/wsdl" xmlns:tds="http://www.onvif.org/ver10/device/wsdl">
                <env:Header>
                    <wsadis:MessageID>urn:uuid:6eff0000-9fd2-11b4-8331-bcbac2a4dee3</wsadis:MessageID>
                    <wsadis:RelatesTo>uuid:537d141e-1309-47bf-927e-1a42727e262d</wsadis:RelatesTo>
                    <wsadis:To>http://schemas.xmlsoap.org/ws/2004/08/addressing/role/anonymous</wsadis:To>
                    <wsadis:Action>http://schemas.xmlsoap.org/ws/2005/04/discovery/ProbeMatches</wsadis:Action>
                    <d:AppSequence InstanceId="1716423123" MessageNumber="105702"/>
                </env:Header>
                <env:Body>
                    <d:ProbeMatches>
                        <d:ProbeMatch>
                            <wsadis:EndpointReference>
                                <wsadis:Address>urn:uuid:6eff0000-9fd2-11b4-8331-bcbac2a4dee3</wsadis:Address>
                            </wsadis:EndpointReference>
                            <d:Types>dn:NetworkVideoTransmitter tds:Device</d:Types>
                            <d:Scopes> 
                                onvif://www.onvif.org/type/video_encoder 
                                onvif://www.onvif.org/Profile/Streaming 
                                onvif://www.onvif.org/Profile/G 
                                onvif://www.onvif.org/Profile/T 
                                onvif://www.onvif.org/MAC/bc:ba:c2:a4:de:e3 
                                onvif://www.onvif.org/hardware/DS-2CD2145FWD-I 
                                onvif://www.onvif.org/name/HIKVISION%20DS-2CD2145FWD-I 
                                onvif://www.onvif.org/location/city/hangzhou
                            </d:Scopes>
                            <d:XAddrs>http://192.168.1.17/onvif/device_service http://[fd8f:ff1b:35a3:e24d:beba:c2ff:fea4:dee3]/onvif/device_service</d:XAddrs>
                            <d:MetadataVersion>10</d:MetadataVersion>
                        </d:ProbeMatch>
                    </d:ProbeMatches>
                </env:Body>
            </env:Envelope>
        */
}
