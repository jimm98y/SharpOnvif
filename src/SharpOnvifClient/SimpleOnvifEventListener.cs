using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace SharpOnvifClient
{
    /// <summary>
    /// Simple Onvif event listener.
    /// </summary>
    /// <remarks>Basic events need an exception in Windows Firewall + VS must run as Admin.</remarks>
    public class SimpleOnvifEventListener : IDisposable
    {
        private bool _disposedValue;
        private Action<int, string> _onEvent;
        private readonly string _host;
        private readonly ushort _port;
        private Task _listenerTask;

        private HttpListener _listener = new HttpListener();

        public SimpleOnvifEventListener(string host = "+", ushort port = 9999)
        {
            _host = host;
            _port = port;
        }

        public void Start(Action<int, string> onEvent)
        {
            _onEvent = onEvent ?? throw new ArgumentNullException(nameof(onEvent));

            string httpUri = GetHttpUri(_host, _port);
            _listener.Prefixes.Add(httpUri);
            _listener.Start();
            _listenerTask = Task.Run(async () =>
            {
                while (!_disposedValue)
                {
                    HttpListenerContext ctx = await _listener.GetContextAsync();
                    using (HttpListenerResponse resp = ctx.Response)
                    {
                        try
                        {
                            string data = GetRequestPostData(ctx.Request);
                            int cameraID = 0;

                            // ctx.Request.RawUrl should be somthing like "/0/"
                            if (int.TryParse(ctx.Request.RawUrl.TrimStart('/').TrimEnd('/'), out cameraID))
                            {
                                ProcessNotification(cameraID, data);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        finally
                        {
                            resp.StatusCode = (int)HttpStatusCode.OK;
                            resp.StatusDescription = "Status OK";
                        }
                    }
                }
            });
        }

        public string GetOnvifEventListenerUri(int cameraID = 0)
        {
            string host = _host;
            if (host == "+")
            {
                // fallback, pick the first active interface that matches our criteria
                host = NetworkInterface.GetAllNetworkInterfaces().First(
                    i => i.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    i.NetworkInterfaceType != NetworkInterfaceType.Tunnel && 
                    i.OperationalStatus == OperationalStatus.Up
                ).GetIPProperties().UnicastAddresses.First(x => x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).Address.ToString();
            }

            return $"{GetHttpUri(host, _port)}{cameraID}/";
        }

        private static string GetHttpUri(string host, int port)
        {
            return $"http://{host}:{port}/";
        }

        private void ProcessNotification(int cameraID, string data)
        {
            _onEvent.Invoke(cameraID, data);
        }

        private static string GetRequestPostData(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
                return string.Empty;

            using (Stream body = request.InputStream)
            {
                using (var reader = new StreamReader(body, request.ContentEncoding))
                    return reader.ReadToEnd();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _listener.Close();
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
