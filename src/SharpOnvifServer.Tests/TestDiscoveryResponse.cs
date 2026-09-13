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
using SharpOnvifCommon.Discovery;
using SharpOnvifServer.Discovery;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The ProbeMatch a device answers a discovery Probe with. It is the only thing a client sees
    /// before it knows the device exists, so a malformed one makes the device invisible.
    /// </summary>
    [TestClass]
    public sealed class TestDiscoveryResponse
    {
        private const string Discovery = "http://schemas.xmlsoap.org/ws/2005/04/discovery";

        /// <summary>The defaults AddOnvifDiscovery installs when the caller configures nothing.</summary>
        private static OnvifDiscoveryOptions DefaultOptions() => new OnvifDiscoveryOptions
        {
            Scopes = new List<string>
            {
                "onvif://www.onvif.org/type/video_encoder",
                "onvif://www.onvif.org/Profile/Streaming",
            },
            Types = new List<OnvifType>
            {
                new OnvifType("http://www.onvif.org/ver10/network/wsdl", "NetworkVideoTransmitter"),
                new OnvifType("http://www.onvif.org/ver10/device/wsdl", "Device"),
            },
        };

        private static XElement Reply(OnvifDiscoveryOptions options, string messageId = "urn:uuid:1")
        {
            string xml = DiscoveryService.CreateDiscoveryResponse(
                options, new[] { new Uri("http://192.168.1.10/onvif/device_service") }, messageId);

            return XDocument.Parse(xml).Root;
        }

        private static string ValueOf(XElement reply, string localName) =>
            reply.Descendants().First(e => e.Name.LocalName == localName).Value;

        [TestMethod]
        public void AdvertisesEachTypeAsATypeOfItsOwn()
        {
            // d:Types is a whitespace-separated list of QNames. Run together, the two default
            // types read as one name that matches neither, and a client filtering on
            // NetworkVideoTransmitter never finds the device.
            XElement reply = Reply(DefaultOptions());
            string[] types = ValueOf(reply, "Types").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            Assert.AreEqual(2, types.Length, "the two types have to be two names");

            var resolved = types.Select(t =>
            {
                string[] parts = t.Split(':');
                Assert.AreEqual(2, parts.Length, $"'{t}' is not a QName");
                XNamespace ns = reply.GetNamespaceOfPrefix(parts[0]);
                Assert.IsNotNull(ns, $"'{t}' uses a prefix the envelope does not declare");
                return ns.NamespaceName + "#" + parts[1];
            }).ToArray();

            CollectionAssert.AreEquivalent(
                new[]
                {
                    "http://www.onvif.org/ver10/network/wsdl#NetworkVideoTransmitter",
                    "http://www.onvif.org/ver10/device/wsdl#Device",
                },
                resolved);
        }

        [TestMethod]
        public void AnswersEvenWhenNoScopesWereConfigured()
        {
            // Options bound from configuration carry only what the file names, so Scopes is
            // routinely null. It used to be copied before it was checked.
            var options = DefaultOptions();
            options.Scopes = null;
            options.Name = "Front door";

            XElement reply = Reply(options);

            StringAssert.Contains(ValueOf(reply, "Scopes"), Scopes.Name + "Front%20door",
                "the scopes built from the named properties still have to be there");
        }

        [TestMethod]
        public void AnswersWhenNoTypesWereConfiguredEither()
        {
            var options = DefaultOptions();
            options.Types = null;
            options.Scopes = null;

            XElement reply = Reply(options);

            Assert.AreEqual(string.Empty, ValueOf(reply, "Types"));
            Assert.AreEqual(string.Empty, ValueOf(reply, "Scopes"));
        }

        [TestMethod]
        public void SeparatesScopesFromOneAnother()
        {
            var options = DefaultOptions();
            options.City = "Prague";

            string[] scopes = ValueOf(Reply(options), "Scopes")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            Assert.AreEqual(3, scopes.Length);
            CollectionAssert.Contains(scopes, Scopes.City + "Prague");
        }

        [TestMethod]
        public void QuotesBackAMessageIdThatIsNotMarkup()
        {
            // The message id comes out of the sender's datagram, and this document is built by
            // concatenation. Unescaped, a '<' in it is markup and the reply stops being XML.
            XElement reply = Reply(DefaultOptions(), "urn:uuid:1<evil/>&x");

            Assert.AreEqual("urn:uuid:1<evil/>&x", ValueOf(reply, "RelatesTo"),
                "the id has to come back as the text it was, not as markup");
            Assert.IsFalse(reply.Descendants().Any(e => e.Name.LocalName == "evil"),
                "the id introduced an element of its own");
        }

        [TestMethod]
        public void RepliesToTheProbeItWasSent()
        {
            XElement reply = Reply(DefaultOptions());

            Assert.AreEqual(Discovery + "/ProbeMatches", ValueOf(reply, "Action"));
            StringAssert.Contains(ValueOf(reply, "XAddrs"), "http://192.168.1.10/onvif/device_service");
        }
    }
}
