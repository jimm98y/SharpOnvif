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

namespace SharpOnvifClient
{
    /// <summary>
    /// An address discovery uses, and for IPv6 the interface carrying it - an IPv6 group is joined
    /// by interface index, not by address.
    /// </summary>
    public sealed class OnvifDiscoveryInterface
    {
        public OnvifDiscoveryInterface(IPAddress address, int index)
        {
            Address = address;
            Index = index;
        }

        /// <summary>The address: what a probe is sent from, and what an IPv4 group is joined on.</summary>
        public IPAddress Address { get; private set; }

        /// <summary>The IPv6 interface index, or zero for an IPv4 address.</summary>
        public int Index { get; private set; }

        /// <summary>
        /// The interfaces discovery works over.
        /// </summary>
        /// <remarks>
        /// One list, used by both halves of discovery: <see cref="OnvifDiscoveryClient"/> sends
        /// its probes over these, and hands the same list to the
        /// <see cref="OnvifDiscoveryListener"/> that hears the answers. The listener used to
        /// choose for itself, on looser rules, so a machine could be probing on one set of
        /// adapters and listening on another.
        /// <para>
        /// Which adapters those are is the part that has been proved against real networks:
        /// wired, wireless or FDDI, up, multicast-capable, and carrying IP. Loopback is left out
        /// because a probe cannot be multicast from it, and a device on this machine is reached
        /// through the real adapters anyway.
        /// </para>
        /// <para>
        /// Every qualifying address is returned, IPv6 link-local included, and what to do with
        /// one is the caller's: a probe cannot be sent from a link-local address, but an adapter
        /// that has only link-local IPv6 can still be listened on, because listening needs the
        /// index rather than the address.
        /// </para>
        /// </remarks>
        public static IEnumerable<OnvifDiscoveryInterface> Enumerate()
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                if (!(adapter.NetworkInterfaceType == NetworkInterfaceType.FastEthernetT ||
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

                if (!(adapter.Supports(NetworkInterfaceComponent.IPv4) || adapter.Supports(NetworkInterfaceComponent.IPv6)))
                    continue;

                IPInterfaceProperties properties = adapter.GetIPProperties();

                if (properties.GetIPv4Properties() == null && properties.GetIPv6Properties() == null)
                    continue;

                int index = 0;
                try
                {
                    index = properties.GetIPv6Properties().Index;
                }
                catch (NetworkInformationException)
                {
                    // No IPv6 on this adapter; its IPv4 addresses are still worth having.
                }

                foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        byte[] bytes = unicast.Address.GetAddressBytes();
                        if (bytes[0] == 169 && bytes[1] == 254)
                            continue; // link-local: nothing answers there

                        yield return new OnvifDiscoveryInterface(unicast.Address, 0);
                    }
                    else if (unicast.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        if (index <= 0)
                            continue; // no index, so no group to join and no route to send on

                        yield return new OnvifDiscoveryInterface(unicast.Address, index);
                    }
                }
            }
        }
    }
}
