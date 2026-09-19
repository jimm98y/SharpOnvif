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
        public void LeavesOutEveryLinkLocalAddress()
        {
            // Neither half of discovery has any use for one: nothing answers a probe sent from a
            // link-local address, and an adapter carrying nothing else has no usable network on
            // it. A machine whose only IPv6 is link-local does no IPv6 discovery at all.
            foreach (OnvifDiscoveryInterface nic in OnvifDiscoveryInterface.Enumerate())
            {
                Assert.IsFalse(nic.Address.IsIPv6LinkLocal, $"{nic.Address} is link-local");
            }
        }

        /// <summary>
        /// An interface index no machine has one of. Handing over an address that cannot be joined
        /// is how the listener is asked where it went: joining it fails, and the failure names it.
        /// </summary>
        /// <remarks>
        /// The index has to be one the stack rejects outright. Zero is not: it reads as "no
        /// interface named", which a stack is free to answer by choosing one itself - and Windows
        /// does, so a test built on zero failing passes or fails according to whose network stack
        /// is running it.
        /// </remarks>
        private const int NoSuchInterface = int.MaxValue;

        /// <summary>
        /// Reserved by RFC 5737 for documentation, so it is on no adapter anywhere and an IPv4
        /// group cannot be joined on it. IPv4 joins by address where IPv6 joins by index, and both
        /// paths are worth walking.
        /// </summary>
        private const string UnassignableIPv4 = "192.0.2.1";

        [TestMethod]
        public void ListensWhereItIsToldRatherThanWhereItChooses()
        {
            // The point of handing the list over: what the listener opens is what it was given.
            // Both of these fail to join, so both are reported by name - and neither is an address
            // this machine's own adapters could ever produce. Were the list ignored and the
            // adapters enumerated instead, the working ones would report nothing at all and these
            // two names could not appear.
            var reported = new List<string>();

            using (var listener = new OnvifDiscoveryListener(new[]
            {
                new OnvifDiscoveryInterface(IPAddress.Parse("::1"), NoSuchInterface),
                new OnvifDiscoveryInterface(IPAddress.Parse(UnassignableIPv4), 0),
            }))
            {
                listener.Failed += (sender, e) => reported.Add(e.NetworkInterface);
                listener.Start();

                // Neither could be joined, so it ended up listening nowhere. Had it enumerated
                // this machine's adapters instead, the ones that work would be in here.
                Assert.AreEqual(0, listener.ListeningOn.Count,
                    "the listener opened an interface that was not on the list it was handed");
            }

            // Equivalent rather than equal: which of the two is reported first is not the point,
            // and an adapter of this machine's turning up here would be the listener choosing.
            CollectionAssert.AreEquivalent(new[] { "::1", UnassignableIPv4 }, reported,
                "the listener went to the interfaces it was handed, and to no others");
        }

        [TestMethod]
        public void ListensNowhereWhenGivenNothing()
        {
            // An empty list is a list, not an absence: it must not fall back to choosing. Saying
            // so needs what it opened, not what it failed to open - a listener that quietly went
            // and enumerated this machine's adapters would report no failure either, and for
            // as long as that was all this test looked at, it passed whether or not the list was
            // honoured.
            var reported = new List<string>();

            using (var listener = new OnvifDiscoveryListener(Array.Empty<OnvifDiscoveryInterface>()))
            {
                listener.Failed += (sender, e) => reported.Add(e.NetworkInterface);
                listener.Start();

                Assert.AreEqual(0, listener.ListeningOn.Count,
                    "an empty list was taken for no list, and the listener chose for itself");
            }

            Assert.AreEqual(0, reported.Count);
        }

        [TestMethod]
        public void ChoosesForItselfOnlyWhenNobodyChose()
        {
            // The standalone case still works, and works from the same list. What it opens is a
            // subset of that list rather than all of it: an adapter can refuse the group, and
            // several IPv6 addresses on one adapter join once between them.
            var usable = new HashSet<string>(
                OnvifDiscoveryInterface.Enumerate().Select(nic => nic.Address.ToString()),
                StringComparer.OrdinalIgnoreCase);

            using (var listener = new OnvifDiscoveryListener())
            {
                listener.Start();

                foreach (OnvifDiscoveryInterface nic in listener.ListeningOn)
                {
                    Assert.IsTrue(usable.Contains(nic.Address.ToString()),
                        $"{nic.Address} is not one of the interfaces discovery works over");
                }
            }
        }

        [TestMethod]
        public void ReportsNothingOpenOnceItIsDisposed()
        {
            // The property says where it is listening, so after the sockets are closed it has to
            // stop naming them.
            var listener = new OnvifDiscoveryListener();

            listener.Start();
            listener.Dispose();

            Assert.AreEqual(0, listener.ListeningOn.Count);
        }

        [TestMethod]
        public void OpensNothingUntilItIsStarted()
        {
            using (var listener = new OnvifDiscoveryListener())
            {
                Assert.AreEqual(0, listener.ListeningOn.Count);
            }
        }
    }
}
