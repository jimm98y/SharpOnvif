using System;
using System.Diagnostics;
using System.IO;
using System.Net;
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
        private readonly Action<int, string> _onEvent;
        private readonly int _port;
        private readonly Task _listenerTask;

        public HttpListener Listener { get; } = new HttpListener();

        public SimpleOnvifEventListener(Action<int, string> onEvent, int port = 9999)
        {
            _onEvent = onEvent ?? throw new ArgumentNullException(nameof(onEvent));
            _port = port;

            string httpUri = GetHttpUri("+", port);
            Listener.Prefixes.Add(httpUri);
            Listener.Start();
            _listenerTask = Task.Run(async () =>
            {
                while (!_disposedValue)
                {
                    HttpListenerContext ctx = await Listener.GetContextAsync();
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

        public string GetOnvifEventListenerUri(string host = "0.0.0.0", int cameraID = 0)
        {
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
                    Listener.Close();
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
