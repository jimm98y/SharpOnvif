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

namespace SharpOnvifClient
{
    public static class OnvifDiscovery
    {
        private class UdpState
        {
            public UdpClient Client { get; set; }
            public IPEndPoint Endpoint { get; set; }
            public IList<string> Result { get; set; }
            public Action<string> Callback { get; set; }
        }

        public const int ONVIF_BROADCAST_TIMEOUT = 4000; // 4s timeout
        public const int ONVIF_DISCOVERY_PORT = 3702;
        public static string OnvifDiscoveryAddress = "239.255.255.250"; // only IPv4 networks are currently supported

        private static readonly SemaphoreSlim _discoverySlim = new SemaphoreSlim(1);

        /// <summary>
        /// Discover ONVIF devices in the local network.
        /// </summary>
        /// <param name="ipAddress">IP address of the network interface to use (IP of the host computer).</param>
        /// <param name="onDeviceDiscovered">Callback to be called when a new device is discovered.</param>
        /// <param name="broadcastTimeout"><see cref="ONVIF_BROADCAST_TIMEOUT"/>.</param>
        /// <param name="broadcastPort">Broadcast port - 0 to let the OS choose any free port.</param>
        /// <param name="deviceType">Device type we are searching for.</param>
        /// <returns>A list of discovered devices.</returns>
        public static async Task<IList<string>> DiscoverAsync(string ipAddress, Action<string> onDeviceDiscovered = null, int broadcastTimeout = ONVIF_BROADCAST_TIMEOUT, int broadcastPort = 0, string deviceType = "NetworkVideoTransmitter")
        {
            if (ipAddress == null)
                return new List<string>();

            await _discoverySlim.WaitAsync();

            string uuid = Guid.NewGuid().ToString().ToLowerInvariant();
            string onvifDiscoveryProbe =
            "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:a=\"http://schemas.xmlsoap.org/ws/2004/08/addressing\">\r\n" +
            "   <s:Header>\r\n" +
            "      <a:Action s:mustUnderstand=\"1\">http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</a:Action>\r\n" +
            "      <a:MessageID>urn:uuid:" + uuid + "</a:MessageID>\r\n" + // uuid has to be unique for each request
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

            IList<string> devices = new List<string>();
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), broadcastPort);
            IPEndPoint multicastEndpoint = new IPEndPoint(IPAddress.Parse(OnvifDiscoveryAddress), ONVIF_DISCOVERY_PORT);

            try
            {
                using (UdpClient client = new UdpClient(endPoint))
                {
                    UdpState s = new UdpState
                    {
                        Endpoint = endPoint,
                        Client = client,
                        Result = devices,
                        Callback = onDeviceDiscovered
                    };

                    client.BeginReceive(DiscoveryMessageReceived, s);

                    byte[] message = Encoding.UTF8.GetBytes(onvifDiscoveryProbe);
                    await client.SendAsync(message, message.Count(), multicastEndpoint);

                    // make sure we do not wait forever
                    await Task.Delay(broadcastTimeout);

                    return s.Result.OrderBy(x => x).ToArray();
                }
            }
            finally
            {
                _discoverySlim.Release();
            }
        }

        private static void DiscoveryMessageReceived(IAsyncResult result)
        {
            try
            {
                UdpClient client = ((UdpState)result.AsyncState).Client;
                IPEndPoint endpoint = ((UdpState)result.AsyncState).Endpoint;
                byte[] receiveBytes = client.EndReceive(result, ref endpoint);
                string message = Encoding.UTF8.GetString(receiveBytes);
                string host = endpoint.Address.ToString();
                var devices = ((UdpState)result.AsyncState).Result;
                var deviceEndpoint = ReadOnvifEndpoint(message);
                if (deviceEndpoint != null)
                {
                    devices.Add(deviceEndpoint);

                    var callback = ((UdpState)result.AsyncState).Callback;
                    if (callback != null)
                    {
                        try
                        {
                            callback.Invoke(deviceEndpoint);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }
                client.BeginReceive(DiscoveryMessageReceived, result.AsyncState);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private static string ReadOnvifEndpoint(string message)
        {
            using (var textReader = new StringReader(message))
            {
                var document = new XPathDocument(textReader);
                var navigator = document.CreateNavigator();

                // local-name is used to ignore the namespace
                var node = navigator.SelectSingleNode("//*[local-name()='XAddrs']/text()");
                if (node != null)
                {
                    string[] addresses = node.Value.Split(' ');
                    return addresses.First();
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
