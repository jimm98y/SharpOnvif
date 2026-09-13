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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SharpOnvifClient;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// Hearing a device announce itself, rather than asking whether it is there.
    /// <para>
    /// An application that starts before its camera does gets nothing from a Probe: the Probe
    /// finds what is on the network at the moment it is sent. A device says Hello when it joins,
    /// and that is what an application waiting for one should be listening for.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestDiscoveryListener
    {
        private const string Discovery = "http://schemas.xmlsoap.org/ws/2005/04/discovery";

        /// <summary>An announcement shaped like the one the server sends.</summary>
        private static string Announcement(string action) =>
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<env:Envelope xmlns:env=\"http://www.w3.org/2003/05/soap-envelope\" " +
                          $"xmlns:d=\"{Discovery}\" " +
                          "xmlns:wsadis=\"http://schemas.xmlsoap.org/ws/2004/08/addressing\">" +
              "<env:Header>" +
                "<wsadis:MessageID>urn:uuid:1</wsadis:MessageID>" +
                $"<wsadis:To>{Discovery}</wsadis:To>" +
                $"<wsadis:Action>{Discovery}/{action}</wsadis:Action>" +
              "</env:Header>" +
              "<env:Body>" +
                $"<d:{action}>" +
                  "<wsadis:EndpointReference><wsadis:Address>urn:uuid:2</wsadis:Address></wsadis:EndpointReference>" +
                  "<d:Types>dn:NetworkVideoTransmitter</d:Types>" +
                  "<d:Scopes>onvif://www.onvif.org/name/Front%20door onvif://www.onvif.org/hardware/ACME</d:Scopes>" +
                  "<d:XAddrs>http://192.168.1.10/onvif/device_service</d:XAddrs>" +
                  "<d:MetadataVersion>10</d:MetadataVersion>" +
                $"</d:{action}>" +
              "</env:Body>" +
            "</env:Envelope>";

        [TestMethod]
        public void KnowsAnArrivalFromADeparture()
        {
            Assert.IsTrue(OnvifDiscoveryListener.IsAnnouncement(Announcement("Hello"), out bool leaving));
            Assert.IsFalse(leaving, "a Hello is a device arriving");

            Assert.IsTrue(OnvifDiscoveryListener.IsAnnouncement(Announcement("Bye"), out leaving));
            Assert.IsTrue(leaving, "a Bye is a device leaving");
        }

        [TestMethod]
        public void IgnoresWhatIsNotAnAnnouncement()
        {
            // A ProbeMatches answers somebody else's Probe. Its action contains the Probe action as
            // a prefix, which is how a reply gets mistaken for a request.
            Assert.IsFalse(OnvifDiscoveryListener.IsAnnouncement(Announcement("ProbeMatches"), out _));
            Assert.IsFalse(OnvifDiscoveryListener.IsAnnouncement(Announcement("Probe"), out _));
            Assert.IsFalse(OnvifDiscoveryListener.IsAnnouncement("", out _));
            Assert.IsFalse(OnvifDiscoveryListener.IsAnnouncement(null, out _));
        }

        [TestMethod]
        public void ReadsTheDeviceOutOfTheAnnouncement()
        {
            // A Hello carries what a ProbeMatch carries, so an application that hears one knows
            // where the device is without having to ask.
            OnvifDiscoveryResult device =
                OnvifDiscoveryClient.ParseDiscoveryResponse(Announcement("Hello"));

            Assert.IsNotNull(device);
            CollectionAssert.AreEqual(
                new[] { "http://192.168.1.10/onvif/device_service" }, device.Addresses);
            Assert.AreEqual("Front door", device.Name, "a scope is URL-escaped and has to be read back");
            Assert.AreEqual("ACME", device.Hardware);
        }

        [TestMethod]
        [Timeout(60000)]
        public void StartsAndStopsWithoutLeavingAnythingBehind()
        {
            // The service this listens for had exactly this bug: a socket read that cancelling
            // could not interrupt, so the process would not exit.
            var stopwatch = Stopwatch.StartNew();

            using (var listener = new OnvifDiscoveryListener())
            {
                listener.Start();
                Thread.Sleep(500);
            }

            stopwatch.Stop();
            Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(15),
                $"disposing took {stopwatch.Elapsed}, which is how a process ends up having to be killed");
        }

        [TestMethod]
        [Timeout(60000)]
        public async Task StopsWaitingWhenItIsToldTo()
        {
            // There is no timeout on waiting for a device - "wait until my camera is switched on"
            // has no natural one - so cancelling has to work.
            using (var cancellation = new CancellationTokenSource())
            {
                Task<OnvifDiscoveryResult> waiting =
                    OnvifDiscoveryClient.WaitForDeviceAsync(device => false, cancellation.Token);

                cancellation.CancelAfter(TimeSpan.FromSeconds(2));

                await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => waiting);
            }
        }
    }
}
