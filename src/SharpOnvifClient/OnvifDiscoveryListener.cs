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
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharpOnvifClient
{
    /// <summary>What a device said about itself, and whether it was arriving or leaving.</summary>
    public class OnvifAnnouncementEventArgs : EventArgs
    {
        public OnvifAnnouncementEventArgs(OnvifDiscoveryResult device, bool isLeaving)
        {
            Device = device;
            IsLeaving = isLeaving;
        }

        /// <summary>The device. A Bye carries only its address, so the rest may be empty.</summary>
        public OnvifDiscoveryResult Device { get; }

        /// <summary>True for a Bye - the device is going away - and false for a Hello.</summary>
        public bool IsLeaving { get; }
    }

    /// <summary>
    /// Listens for the devices that announce themselves, rather than asking for them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A Probe finds the devices that are on the network at the moment it is sent, which is no
    /// help to an application that starts before its camera does. A device announces itself with a
    /// WS-Discovery Hello when it joins and a Bye when it leaves, and this reports both as they
    /// arrive.
    /// </para>
    /// <para>
    /// Announcements are UDP multicast and are not retransmitted, so one can simply be lost, and
    /// one sent before this started listening is already gone. An application that must not miss a
    /// device should probe as well as listen - <see cref="OnvifDiscoveryClient.WaitForDeviceAsync"/>
    /// does both.
    /// </para>
    /// </remarks>
    public sealed class OnvifDiscoveryListener : IDisposable
    {
        private const string DiscoveryNamespace = "http://schemas.xmlsoap.org/ws/2005/04/discovery";

        /// <summary>How long <see cref="Dispose"/> waits for the listeners before giving up on them.</summary>
        private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly object _syncRoot = new object();
        private readonly List<UdpClient> _clients = new List<UdpClient>();
        private readonly List<Task> _listeners = new List<Task>();
        private bool _disposed;

        /// <summary>Raised when a device announces that it has joined the network.</summary>
        public event EventHandler<OnvifAnnouncementEventArgs> DeviceAnnounced;

        /// <summary>Raised when a device announces that it is leaving.</summary>
        public event EventHandler<OnvifAnnouncementEventArgs> DeviceLeft;

        /// <summary>
        /// Starts listening on every interface that can carry multicast. Interfaces that cannot be
        /// joined are skipped rather than failing the whole listener - one unusable interface on a
        /// machine is normal.
        /// </summary>
        public void Start()
        {
            foreach (IPAddress address in MulticastAddresses())
            {
                Listen(address);
            }
        }

        private static IEnumerable<IPAddress> MulticastAddresses()
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up) continue;
                if (!adapter.SupportsMulticast) continue;

                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    // IPv4 only: the Onvif IPv6 group cannot be joined on every host, and a device
                    // that announces at all announces on IPv4.
                    if (unicast.Address.AddressFamily != AddressFamily.InterNetwork) continue;

                    byte[] bytes = unicast.Address.GetAddressBytes();
                    if (bytes[0] == 169 && bytes[1] == 254) continue; // link-local

                    yield return unicast.Address;
                }
            }
        }

        private void Listen(IPAddress nicAddress)
        {
            UdpClient client = null;
            try
            {
                client = new UdpClient(AddressFamily.InterNetwork);

                // A device on this machine is already bound to the discovery port, and both of
                // them are entitled to hear what arrives.
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(nicAddress, OnvifDiscoveryClient.ONVIF_DISCOVERY_PORT));
                client.JoinMulticastGroup(
                    IPAddress.Parse(OnvifDiscoveryClient.OnvifDiscoveryAddressIPV4), nicAddress);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Cannot listen for Onvif announcements on {nicAddress}: {ex.Message}");
                client?.Dispose();
                return;
            }

            lock (_syncRoot)
            {
                _clients.Add(client);
            }

            CancellationToken token = _cts.Token;

            Task listener = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // Awaited rather than blocked on, so that cancelling actually ends this.
                        UdpReceiveResult received = await ReceiveAsync(client, token).ConfigureAwait(false);
                        Report(Encoding.UTF8.GetString(received.Buffer));
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        // One bad datagram is not a reason to stop listening to the network.
                        Debug.WriteLine($"Onvif announcement on {nicAddress} could not be read: {ex.Message}");
                    }
                }
            });

            lock (_syncRoot)
            {
                _listeners.Add(listener);
            }
        }

        /// <summary>
        /// Receives one datagram, giving up when cancelled.
        /// </summary>
        /// <remarks>
        /// Only .NET 6 and later have a cancellable overload. Elsewhere the receive is raced
        /// against the token and the socket is closed underneath it by <see cref="Dispose"/>,
        /// which does complete a pending receive on those runtimes - the completion is driven by
        /// the I/O port rather than by a thread parked inside the call.
        /// </remarks>
        private static async Task<UdpReceiveResult> ReceiveAsync(UdpClient client, CancellationToken token)
        {
#if NET6_0_OR_GREATER
            return await client.ReceiveAsync(token).ConfigureAwait(false);
#else
            Task<UdpReceiveResult> receiving = client.ReceiveAsync();

            var cancelled = new TaskCompletionSource<bool>();
            using (token.Register(() => cancelled.TrySetResult(true)))
            {
                if (await Task.WhenAny(receiving, cancelled.Task).ConfigureAwait(false) != receiving)
                    throw new OperationCanceledException(token);
            }

            return await receiving.ConfigureAwait(false);
#endif
        }

        private void Report(string message)
        {
            bool leaving;
            if (!IsAnnouncement(message, out leaving)) return;

            OnvifDiscoveryResult device = OnvifDiscoveryClient.ParseDiscoveryResponse(message);
            if (device == null) return;

            var args = new OnvifAnnouncementEventArgs(device, leaving);

            // A handler is the application's code; one that throws must not take the listener with
            // it, or a single bad notification stops the machine hearing about any device again.
            try
            {
                if (leaving) DeviceLeft?.Invoke(this, args);
                else DeviceAnnounced?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An Onvif announcement handler threw: {ex.Message}");
            }
        }

        /// <summary>
        /// Whether this datagram is a Hello or a Bye. A ProbeMatches answers somebody else's Probe
        /// and is not an announcement; its action contains the Probe action, so the actions are
        /// compared whole.
        /// </summary>
        internal static bool IsAnnouncement(string message, out bool leaving)
        {
            leaving = false;
            if (string.IsNullOrEmpty(message)) return false;

            if (HasAction(message, DiscoveryNamespace + "/Hello")) return true;
            if (HasAction(message, DiscoveryNamespace + "/Bye"))
            {
                leaving = true;
                return true;
            }

            return false;
        }

        private static bool HasAction(string message, string action)
        {
            int at = message.IndexOf(action, StringComparison.Ordinal);
            if (at < 0) return false;

            // Nothing may follow but the end of the element: an action is not a prefix of itself
            // plus more letters.
            int after = at + action.Length;
            return after >= message.Length || !char.IsLetterOrDigit(message[after]);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();

            Task[] listeners;
            List<UdpClient> clients;
            lock (_syncRoot)
            {
                listeners = _listeners.ToArray();
                clients = new List<UdpClient>(_clients);
                _listeners.Clear();
                _clients.Clear();
            }

            // Bounded: a listener that will not stop must not hold up the application that is
            // trying to shut down.
            try
            {
                Task.WhenAll(listeners).Wait(ShutdownTimeout);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"An Onvif announcement listener did not stop cleanly: {ex.Message}");
            }

            foreach (UdpClient client in clients)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"An Onvif announcement socket did not close: {ex.Message}");
                }
            }

            _cts.Dispose();
        }
    }
}
