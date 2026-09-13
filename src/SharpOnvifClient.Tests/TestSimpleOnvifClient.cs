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
using System.Threading.Tasks;
using SharpOnvifClient;
using SharpOnvifClient.DeviceMgmt;
using SharpOnvifCommon;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// How the convenience client deals with what devices actually answer, and how long it is
    /// prepared to wait for them.
    /// </summary>
    [TestClass]
    public sealed class TestSimpleOnvifClient
    {
        private const string Endpoint = "http://192.168.1.10/onvif/device_service";

        /// <summary>Answers GetServices from a script instead of from a device.</summary>
        private sealed class ScriptedClient : SimpleOnvifClient
        {
            private readonly GetServicesResponse _services;

            public ScriptedClient(GetServicesResponse services) : base(Endpoint)
            {
                _services = services;
            }

            public int GetServicesCalls;

            public override Task<GetServicesResponse> GetServicesAsync(bool includeCapability = false)
            {
                GetServicesCalls++;
                return Task.FromResult(_services);
            }

            public Task<string> ResolveAsync(string ns) => GetServiceUriAsync(ns);
        }

        private static Service Service(string ns, string address) =>
            new Service { Namespace = ns, XAddr = address };

        [TestMethod]
        public async Task FindsAServiceAddress()
        {
            using (var client = new ScriptedClient(new GetServicesResponse
            {
                Service = new[]
                {
                    Service(OnvifServices.DEVICE_MGMT, Endpoint),
                    Service(OnvifServices.MEDIA, "http://192.168.1.10/onvif/media"),
                },
            }))
            {
                Assert.AreEqual("http://192.168.1.10/onvif/media", await client.ResolveAsync(OnvifServices.MEDIA));

                await client.ResolveAsync(OnvifServices.DEVICE_MGMT);
                Assert.AreEqual(1, client.GetServicesCalls, "the service list is read once and kept");

                await Assert.ThrowsExactlyAsync<NotSupportedException>(
                    () => client.ResolveAsync(OnvifServices.PTZ));
            }
        }

        [TestMethod]
        public async Task SurvivesADeviceThatListsAServiceTwice()
        {
            // Devices do it - the same service at two versions, or simply repeated. Adding to the
            // dictionary threw, and took down every call that needs a service address with it.
            using (var client = new ScriptedClient(new GetServicesResponse
            {
                Service = new[]
                {
                    Service(OnvifServices.MEDIA, "http://192.168.1.10/onvif/media"),
                    Service(OnvifServices.MEDIA, "http://192.168.1.10/onvif/media_2"),
                    Service(null, "http://192.168.1.10/onvif/nameless"),
                },
            }))
            {
                Assert.AreEqual("http://192.168.1.10/onvif/media", await client.ResolveAsync(OnvifServices.MEDIA),
                    "the first address the device gave is the one to use");
            }
        }

        [DataRow(60, 60, DisplayName = "the defaults, which used to be equal")]
        [DataRow(120, 60, DisplayName = "a longer poll than the configured timeout")]
        [DataRow(1, 60, DisplayName = "a short poll under a long configured timeout")]
        [TestMethod]
        public void WaitsLongerForAPullThanThePullItselfLasts(int pullSeconds, int configuredSeconds)
        {
            // The device holds the request for the pull timeout and answers right at the end when
            // nothing happened. An HTTP timeout that is merely equal turns every quiet minute into
            // a cancelled request.
            TimeSpan configured = TimeSpan.FromSeconds(configuredSeconds);
            TimeSpan http = SimpleOnvifClient.GetPullMessagesHttpTimeout(pullSeconds, configured);

            Assert.IsTrue(http > TimeSpan.FromSeconds(pullSeconds),
                $"a {pullSeconds}s pull cannot be given a {http.TotalSeconds}s HTTP timeout");
            Assert.IsTrue(http >= configured, "the configured timeout is a floor, not a ceiling");
        }
    }
}
