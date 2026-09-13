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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Threading.Tasks;
using SharpOnvifCommon.Security;

namespace SharpOnvifClient
{
    /// <summary>
    /// Simple Onvif event listener: the address a camera is told to deliver Basic subscription
    /// notifications to, and the endpoint that receives them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is an inbound endpoint on your machine, and Onvif gives a camera no way to
    /// authenticate itself to it - the notification arrives as a plain POST. Anything that can
    /// reach the port can therefore deliver a notification, and an application that acts on one
    /// acts on whatever it is told.
    /// </para>
    /// <para>
    /// What can be done about it, in the order it is worth doing:
    /// </para>
    /// <list type="number">
    /// <item>
    /// The address handed to the camera carries an unguessable token, and a request that does not
    /// present it is refused. Nothing is asked of the camera - it posts where it was told - so
    /// this works with every device, and it is on by default. It stops anything that has not seen
    /// the address, which is everything scanning the network; it does not stop something that has.
    /// </item>
    /// <item>
    /// <see cref="AllowedSources"/> restricts who may deliver at all. Set it to the camera's
    /// address and a notification from anywhere else is refused before it is read.
    /// </item>
    /// <item>
    /// Bind to one interface rather than the default of all of them, by passing the address to the
    /// constructor. An endpoint that is not reachable from a network cannot be reached from it.
    /// </item>
    /// </list>
    /// <para>
    /// None of this makes the channel private: the notification crosses the network in the clear,
    /// and a camera cannot in general be told to use https. Treat what arrives as a hint that
    /// something happened, and read anything that matters from the device over the authenticated
    /// connection you already have.
    /// </para>
    /// <para>Basic events need an exception in Windows Firewall + VS must run as Admin.</para>
    /// </remarks>
    public class SimpleOnvifEventListener : IDisposable
    {
        /// <summary>Bytes of randomness in the address token.</summary>
        private const int TokenBytes = 16;

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
            PathToken = CreateToken();
        }

        /// <summary>
        /// The unguessable segment of the address a camera is given, which it has to present for a
        /// notification to be accepted.
        /// </summary>
        /// <remarks>
        /// Generated per listener. Set it to null or an empty string to hand out a bare address
        /// instead, which anything that finds the port can post to.
        /// </remarks>
        public string PathToken { get; set; }

        /// <summary>
        /// Addresses a notification may be delivered from. Empty - the default - accepts delivery
        /// from anywhere that can reach the port.
        /// </summary>
        public ICollection<IPAddress> AllowedSources { get; } = new List<IPAddress>();

        /// <summary>
        /// The largest notification that will be read. A camera's notification is a few kilobytes;
        /// this bounds what something else can make the process allocate.
        /// </summary>
        public int MaxNotificationBytes { get; set; } = 1024 * 1024;

        /// <summary>Notifications refused since the listener started, for logging or alerting.</summary>
        public long RefusedCount { get { return System.Threading.Interlocked.Read(ref _refused); } }

        private long _refused;

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
                    HttpListenerContext ctx;
                    try
                    {
                        ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Closing the listener is how this loop ends.
                        if (_disposedValue) return;
                        Debug.WriteLine(ex.Message);
                        continue;
                    }

                    using (HttpListenerResponse resp = ctx.Response)
                    {
                        try
                        {
                            Handle(ctx, resp);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            resp.StatusCode = (int)HttpStatusCode.OK;
                        }
                    }
                }
            });
        }

        private void Handle(HttpListenerContext ctx, HttpListenerResponse resp)
        {
            if (!string.Equals(ctx.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                Refuse(resp, HttpStatusCode.MethodNotAllowed);
                return;
            }

            if (!IsAllowedSource(ctx.Request.RemoteEndPoint))
            {
                Refuse(resp, HttpStatusCode.Forbidden);
                return;
            }

            int cameraID;
            if (!TryReadAddress(ctx.Request.RawUrl, out cameraID))
            {
                // Not found rather than forbidden: an address that was never handed out does not
                // exist, and saying so tells a caller nothing about what a real one looks like.
                Refuse(resp, HttpStatusCode.NotFound);
                return;
            }

            if (ctx.Request.ContentLength64 > MaxNotificationBytes)
            {
                Refuse(resp, HttpStatusCode.RequestEntityTooLarge);
                return;
            }

            string data = GetRequestPostData(ctx.Request, MaxNotificationBytes);
            if (data == null)
            {
                Refuse(resp, HttpStatusCode.RequestEntityTooLarge);
                return;
            }

            ProcessNotification(cameraID, data);

            resp.StatusCode = (int)HttpStatusCode.OK;
            resp.StatusDescription = "Status OK";
        }

        private void Refuse(HttpListenerResponse resp, HttpStatusCode status)
        {
            System.Threading.Interlocked.Increment(ref _refused);
            resp.StatusCode = (int)status;
        }

        private bool IsAllowedSource(IPEndPoint remote)
        {
            if (AllowedSources.Count == 0) return true;
            if (remote == null) return false;

            IPAddress source = remote.Address;
            if (source.IsIPv4MappedToIPv6) source = source.MapToIPv4();

            foreach (IPAddress allowed in AllowedSources)
            {
                if (allowed == null) continue;

                IPAddress candidate = allowed.IsIPv4MappedToIPv6 ? allowed.MapToIPv4() : allowed;
                if (candidate.Equals(source)) return true;
            }

            return false;
        }

        /// <summary>
        /// Reads the address a notification arrived on: the token, if one is in use, then the
        /// camera it belongs to.
        /// </summary>
        internal bool TryReadAddress(string rawUrl, out int cameraID)
        {
            cameraID = 0;
            if (rawUrl == null) return false;

            int query = rawUrl.IndexOf('?');
            if (query >= 0) rawUrl = rawUrl.Substring(0, query);

            string[] segments = rawUrl.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            int next = 0;

            if (!string.IsNullOrEmpty(PathToken))
            {
                // Compared in fixed time: the token is the only thing standing between this
                // endpoint and anything else that can reach the port.
                if (segments.Length == 0) return false;
                if (!HttpDigestAuthentication.FixedTimeEquals(PathToken, segments[0])) return false;
                next = 1;
            }

            if (segments.Length <= next) return false;

            return int.TryParse(segments[next], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out cameraID);
        }

        /// <summary>
        /// The address to give the camera in <c>BasicSubscribeAsync</c>. It carries the token, so
        /// it is the only address notifications will be accepted on.
        /// </summary>
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

            string token = string.IsNullOrEmpty(PathToken) ? string.Empty : PathToken + "/";
            return $"{GetHttpUri(host, _port)}{token}{cameraID}/";
        }

        private static string CreateToken()
        {
            byte[] bytes = new byte[TokenBytes];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }

            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string GetHttpUri(string host, int port)
        {
            return $"http://{host}:{port}/";
        }

        private void ProcessNotification(int cameraID, string data)
        {
            _onEvent.Invoke(cameraID, data);
        }

        /// <summary>Reads the body, or null when it turns out to be longer than allowed.</summary>
        private static string GetRequestPostData(HttpListenerRequest request, int maxBytes)
        {
            if (!request.HasEntityBody)
                return string.Empty;

            using (Stream body = request.InputStream)
            {
                // A chunked request declares no length, so the limit is enforced as it arrives.
                var buffer = new MemoryStream();
                byte[] chunk = new byte[8192];
                int total = 0;

                while (true)
                {
                    int read = body.Read(chunk, 0, chunk.Length);
                    if (read == 0) break;

                    total += read;
                    if (total > maxBytes) return null;

                    buffer.Write(chunk, 0, read);
                }

                return request.ContentEncoding.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                _disposedValue = true;

                if (disposing)
                {
                    _listener.Close();
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
