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
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpOnvifCommon;

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
        /// Raised when the listener could not do something - open a socket on an interface, read
        /// what arrived, or run a handler. Listening carries on: one interface that cannot carry
        /// multicast is ordinary, and is not a reason to stop hearing the others.
        /// </summary>
        public event EventHandler<OnvifDiscoveryFailureEventArgs> Failed;

        /// <summary>
        /// Starts listening on every interface that can carry multicast, IPv4 and IPv6 alike.
        /// Interfaces that cannot be joined are skipped rather than failing the whole listener -
        /// one unusable interface on a machine is normal.
        /// </summary>
        public void Start()
        {
            foreach (Interface nic in MulticastInterfaces())
            {
                Listen(nic);
            }
        }

        /// <summary>
        /// An address to listen on, and for IPv6 the interface it belongs to - an IPv6 group is
        /// joined by interface index, not by address.
        /// </summary>
        private struct Interface
        {
            public Interface(IPAddress address, int index)
            {
                Address = address;
                Index = index;
            }

            public IPAddress Address;
            public int Index;
        }

        private static IEnumerable<Interface> MulticastInterfaces()
        {
            // One join per interface for IPv6: a single adapter commonly carries several IPv6
            // addresses, and they would all be joining the same group on the same interface.
            var joinedIPv6 = new HashSet<int>();

            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up) continue;
                if (!adapter.SupportsMulticast) continue;

                IPInterfaceProperties properties = adapter.GetIPProperties();

                int interfaceIndex = -1;
                try
                {
                    interfaceIndex = properties.GetIPv6Properties().Index;
                }
                catch (NetworkInformationException)
                {
                    // No IPv6 on this adapter; its IPv4 addresses are still worth listening on.
                }

                foreach (var unicast in properties.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        byte[] bytes = unicast.Address.GetAddressBytes();
                        if (bytes[0] == 169 && bytes[1] == 254) continue; // link-local

                        yield return new Interface(unicast.Address, 0);
                    }
                    else if (unicast.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        if (interfaceIndex < 0) continue;
                        if (!joinedIPv6.Add(interfaceIndex)) continue;

                        yield return new Interface(unicast.Address, interfaceIndex);
                    }
                }
            }
        }

        private void Listen(Interface nic)
        {
            bool isIPv6 = nic.Address.AddressFamily == AddressFamily.InterNetworkV6;

            UdpClient client = null;
            try
            {
                client = new UdpClient(nic.Address.AddressFamily);

                // A device on this machine is already bound to the discovery port, and both of
                // them are entitled to hear what arrives.
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Bound to every address, joined on one interface below. A socket bound to an
                // interface's own address is not given multicast looped back from this machine,
                // so a device running beside this one would never be heard.
                client.Client.Bind(new IPEndPoint(
                    isIPv6 ? IPAddress.IPv6Any : IPAddress.Any,
                    OnvifDiscoveryClient.ONVIF_DISCOVERY_PORT));

                if (isIPv6)
                {
                    // By index: ff02::c is link-local scope, so there is no default interface to
                    // fall back on and index zero is not an interface at all.
                    client.JoinMulticastGroup(
                        nic.Index, IPAddress.Parse(OnvifDiscoveryClient.OnvifDiscoveryAddressIPV6));
                }
                else
                {
                    client.JoinMulticastGroup(
                        IPAddress.Parse(OnvifDiscoveryClient.OnvifDiscoveryAddressIPV4), nic.Address);
                }
            }
            catch (Exception ex)
            {
                // One interface that cannot carry this - a VM bridge with no IPv6, say - is not a
                // reason to listen on none of the others.
                OnvifDiscoveryFailure.Raise(Failed, this, OnvifDiscoveryOperation.Listen,
                    nic.Address.ToString(), ex);
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
                        OnvifDiscoveryFailure.Raise(Failed, this, OnvifDiscoveryOperation.Receive,
                            nic.Address.ToString(), ex);
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
                OnvifDiscoveryFailure.Raise(Failed, this, OnvifDiscoveryOperation.Handler, null, ex);
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
                Log.Warning("An Onvif announcement listener did not stop cleanly.", ex);
            }

            foreach (UdpClient client in clients)
            {
                try
                {
                    client.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Warning("An Onvif announcement socket did not close.", ex);
                }
            }

            _cts.Dispose();
        }
    }
}
