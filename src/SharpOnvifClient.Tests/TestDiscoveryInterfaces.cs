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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using SharpOnvifClient;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The interfaces discovery works over.
    /// <para>
    /// One list, shared: probes go out on these and the listener hears the answers on the same
    /// ones. Each half used to choose for itself, on different rules, so a machine could be
    /// probing over one set of adapters and listening on another.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestDiscoveryInterfaces
    {
        [TestMethod]
        public void OffersNothingThatCannotCarryDiscovery()
        {
            // Whatever this machine has, every entry has to be usable: nothing on loopback, no
            // IPv4 address nothing answers on, and no IPv6 without the interface index a group is
            // joined by.
            foreach (OnvifDiscoveryInterface nic in OnvifDiscoveryInterface.Enumerate())
            {
                Assert.IsFalse(IPAddress.IsLoopback(nic.Address), $"{nic.Address} is loopback");

                if (nic.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    byte[] bytes = nic.Address.GetAddressBytes();
                    Assert.IsFalse(bytes[0] == 169 && bytes[1] == 254, $"{nic.Address} is link-local");
                    Assert.AreEqual(0, nic.Index, "an IPv4 address is joined by address, not by index");
                }
                else
                {
                    Assert.IsTrue(nic.Index > 0, $"{nic.Address} has no interface to join on");
                }
            }
        }

        [TestMethod]
        public void KeepsIPv6LinkLocalForWhoeverCanUseIt()
        {
            // The two halves want different things from the same list. A probe cannot be sent from
            // a link-local address, so the client skips those; the listener joins by index and can
            // use an adapter that has nothing else - so the list carries them and the caller
            // decides.
            foreach (OnvifDiscoveryInterface nic in OnvifDiscoveryInterface.Enumerate())
            {
                if (nic.Address.AddressFamily != AddressFamily.InterNetworkV6) continue;
                if (!nic.Address.IsIPv6LinkLocal) continue;

                Assert.IsTrue(nic.Index > 0, "a link-local entry is only useful with its index");
                return;
            }

            Assert.Inconclusive("this machine has no link-local IPv6 to check");
        }

        [TestMethod]
        public void ListensWhereItIsToldRatherThanWhereItChooses()
        {
            // The point of handing the list over: what the listener opens is what it was given.
            // Index 0 is not an interface, so this one cannot be joined and is reported - from a
            // list this machine's own adapters would never produce.
            var reported = new List<string>();

            using (var listener = new OnvifDiscoveryListener(new[]
            {
                new OnvifDiscoveryInterface(IPAddress.Parse("::1"), 0),
            }))
            {
                listener.Failed += (sender, e) => reported.Add(e.NetworkInterface);
                listener.Start();
            }

            CollectionAssert.AreEqual(new[] { "::1" }, reported,
                "the listener went to the interface it was handed, and to no others");
        }

        [TestMethod]
        public void ListensNowhereWhenGivenNothing()
        {
            // An empty list is a list, not an absence: it must not fall back to choosing.
            var reported = new List<string>();

            using (var listener = new OnvifDiscoveryListener(Array.Empty<OnvifDiscoveryInterface>()))
            {
                listener.Failed += (sender, e) => reported.Add(e.NetworkInterface);
                listener.Start();
            }

            Assert.AreEqual(0, reported.Count);
        }

        [TestMethod]
        public void ChoosesForItselfOnlyWhenNobodyChose()
        {
            // The standalone case still works, and works from the same list.
            using (var listener = new OnvifDiscoveryListener())
            {
                listener.Start();
            }
        }
    }
}
