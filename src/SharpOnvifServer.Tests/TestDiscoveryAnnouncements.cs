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
using System.Xml.Linq;
using SharpOnvifServer.Discovery;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The announcements a device makes about itself: Hello when it joins the network, Bye when it
    /// leaves, and a ProbeMatch when asked. A client that never sees a Hello only finds the device
    /// by probing for it, and one that never sees a Bye waits for a device that has gone.
    /// </summary>
    [TestClass]
    public sealed class TestDiscoveryAnnouncements
    {
        private static readonly Uri[] Addresses = { new Uri("http://192.168.1.10/onvif/device_service") };

        private static OnvifDiscoveryOptions Options() => new OnvifDiscoveryOptions
        {
            Scopes = new List<string> { "onvif://www.onvif.org/Profile/Streaming" },
            Types = new List<OnvifType>
            {
                new OnvifType("http://www.onvif.org/ver10/network/wsdl", "NetworkVideoTransmitter"),
            },
        };

        private static XElement Announce(
            DiscoveryService.DiscoveryMessageType type, OnvifDiscoveryOptions options)
        {
            string xml = DiscoveryService.CreateDiscoveryMessage(
                type, options, Addresses, "urn:uuid:probe", DiscoveryService.EndpointReferenceOf(options));

            return XDocument.Parse(xml).Root;
        }

        private static string ValueOf(XElement message, string localName) =>
            message.Descendants().First(e => e.Name.LocalName == localName).Value;

        private static bool Has(XElement message, string localName) =>
            message.Descendants().Any(e => e.Name.LocalName == localName);

        [TestMethod]
        public void SaysHelloWithEverythingAProbeWouldHaveReturned()
        {
            // A client that sees the Hello should not have to probe to learn anything more.
            XElement hello = Announce(DiscoveryService.DiscoveryMessageType.Hello, Options());

            Assert.IsTrue(Has(hello, "Hello"));
            Assert.AreEqual(DiscoveryService.DiscoveryNamespace + "/Hello", ValueOf(hello, "Action"));
            StringAssert.Contains(ValueOf(hello, "Types"), "NetworkVideoTransmitter");
            StringAssert.Contains(ValueOf(hello, "XAddrs"), "192.168.1.10");
            StringAssert.StartsWith(ValueOf(hello, "Address"), "urn:uuid:");

            Assert.IsFalse(Has(hello, "RelatesTo"), "an announcement is not a reply to anything");
            Assert.AreEqual(DiscoveryService.DiscoveryNamespace, ValueOf(hello, "To"),
                "an announcement goes to everyone listening, not to one client");
        }

        [TestMethod]
        public void SaysByeWithTheAddressAndNothingToActOn()
        {
            XElement bye = Announce(DiscoveryService.DiscoveryMessageType.Bye, Options());

            Assert.IsTrue(Has(bye, "Bye"));
            Assert.AreEqual(DiscoveryService.DiscoveryNamespace + "/Bye", ValueOf(bye, "Action"));
            StringAssert.StartsWith(ValueOf(bye, "Address"), "urn:uuid:");

            Assert.IsFalse(Has(bye, "XAddrs"), "a device that is leaving is not offering an address to call");
        }

        [TestMethod]
        public void CallsItselfTheSameThingInEveryAnnouncement()
        {
            // A client pairs a Bye with the Hello and the ProbeMatch that named the same endpoint.
            // A fresh address per message is a different device every time.
            var options = Options();

            string hello = ValueOf(Announce(DiscoveryService.DiscoveryMessageType.Hello, options), "Address");
            string bye = ValueOf(Announce(DiscoveryService.DiscoveryMessageType.Bye, options), "Address");
            string probeMatch = XDocument
                .Parse(DiscoveryService.CreateDiscoveryResponse(options, Addresses, "urn:uuid:probe"))
                .Root.Descendants().First(e => e.Name.LocalName == "Address").Value;

            Assert.AreEqual(hello, bye);
            Assert.AreEqual(hello, probeMatch);

            Assert.AreNotEqual(hello, ValueOf(Announce(DiscoveryService.DiscoveryMessageType.Hello, Options()), "Address"),
                "two devices are not the same device");
        }

        [TestMethod]
        public void UsesTheAddressItWasConfiguredWith()
        {
            // A device that survives a restart has to be recognised as the same device after it.
            var options = Options();
            options.EndpointReference = "urn:uuid:11111111-2222-3333-4444-555555555555";

            Assert.AreEqual(options.EndpointReference,
                ValueOf(Announce(DiscoveryService.DiscoveryMessageType.Hello, options), "Address"));
        }

        [TestMethod]
        public void ReportsTheMetadataVersionItWasConfiguredWith()
        {
            var options = Options();
            options.MetadataVersion = 42;

            Assert.AreEqual("42", ValueOf(Announce(DiscoveryService.DiscoveryMessageType.Hello, options), "MetadataVersion"));
        }

        [TestMethod]
        public void SpreadsItsAnswersToAProbeOverTime()
        {
            // WS-Discovery asks for a delay chosen at random up to APP_MAX_DELAY, so that a
            // network of devices answering one Probe does not reply in a single burst.
            var seen = new HashSet<int>();
            for (int i = 0; i < 2000; i++)
            {
                int delay = DiscoveryService.NextProbeDelayMilliseconds();

                Assert.IsTrue(delay >= 0 && delay <= DiscoveryService.AppMaxDelayMilliseconds,
                    $"{delay}ms is outside the window the specification allows");
                seen.Add(delay);
            }

            Assert.IsTrue(seen.Count > 100, $"only {seen.Count} distinct delays - the answers are not spread out");
        }
    }
}
