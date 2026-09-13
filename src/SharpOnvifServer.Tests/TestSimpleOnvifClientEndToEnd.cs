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
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpOnvifClient;
using SharpOnvifCommon;
using SharpOnvifServer.Dispatch;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// SimpleOnvifClient against a device, which exercises the step the generated clients do not:
    /// discovering where a service lives.
    /// <para>
    /// The client reads the addresses GetServices advertises and sends every later call there. A
    /// device that advertises an address it does not serve leaves the client calling into
    /// nothing, and the failure surfaces far from its cause - so this asserts that what the
    /// device publishes is actually reachable.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestSimpleOnvifClientEndToEnd
    {
        private const string EndpointPath = "/onvif/device_service";

        private static WebApplication _app;
        private static string _endpoint;

        private sealed class DeviceImpl : SharpOnvifServer.DeviceMgmt.DeviceBase
        {
            public override SharpOnvifServer.DeviceMgmt.GetServicesResponse GetServices(
                SharpOnvifServer.DeviceMgmt.GetServicesRequest request)
            {
                // Advertise the address this request arrived on, which is what a device serving
                // several services from one endpoint does.
                string address = OnvifOperationContext.RequestUri.ToString();

                return new SharpOnvifServer.DeviceMgmt.GetServicesResponse
                {
                    Service = new[]
                    {
                        Service(OnvifServices.DEVICE_MGMT, address),
                        Service(OnvifServices.MEDIA, address),
                    },
                };
            }

            private static SharpOnvifServer.DeviceMgmt.Service Service(string ns, string address)
            {
                return new SharpOnvifServer.DeviceMgmt.Service
                {
                    Namespace = ns,
                    XAddr = address,
                    Version = new SharpOnvifCommon.Onvif.OnvifVersion { Major = 2, Minor = 6 },
                };
            }

            public override SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse GetDeviceInformation()
            {
                return new SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse { Manufacturer = "ACME" };
            }
        }

        private sealed class MediaImpl : SharpOnvifServer.Media.MediaBase
        {
            public override SharpOnvifServer.Media.GetProfilesResponse GetProfiles(
                SharpOnvifServer.Media.GetProfilesRequest request)
            {
                return new SharpOnvifServer.Media.GetProfilesResponse
                {
                    Profiles = new[] { new SharpOnvifCommon.Onvif.Profile { token = "p0", Name = "MainStream" } },
                };
            }

            public override SharpOnvifServer.Media.GetStreamUriResponse GetStreamUri(
                SharpOnvifCommon.Onvif.StreamSetup StreamSetup, string ProfileToken)
            {
                return new SharpOnvifServer.Media.GetStreamUriResponse
                {
                    MediaUri = new SharpOnvifCommon.Onvif.MediaUri
                    {
                        Uri = "rtsp://localhost:8554/" + ProfileToken,
                    },
                };
            }
        }

        [ClassInitialize]
        public static async Task StartServer(TestContext context)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            builder.Services.AddSingleton<DeviceImpl>();
            builder.Services.AddSingleton<MediaImpl>();

            _app = builder.Build();
            _app.MapOnvifService<DeviceImpl>(EndpointPath);
            _app.MapOnvifService<MediaImpl>(EndpointPath);

            await _app.StartAsync();

            string address = _app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>().Addresses.First();
            _endpoint = address.TrimEnd('/') + EndpointPath;
        }

        [ClassCleanup]
        public static async Task StopServer()
        {
            if (_app != null) await _app.StopAsync();
        }

        [TestMethod]
        public async Task ReachesAServiceThroughTheAddressTheDeviceAdvertises()
        {
            using (var client = new SimpleOnvifClient(_endpoint))
            {
                var services = await client.GetServicesAsync(true);
                Assert.IsNotNull(services.Service.FirstOrDefault(x => x.Namespace == OnvifServices.MEDIA),
                    "the device has to advertise the media service for the rest of this to mean anything");

                // Goes to whatever GetServices advertised, not to the endpoint we started from.
                var profiles = await client.GetProfilesAsync();

                Assert.AreEqual(1, profiles.Profiles.Length);
                Assert.AreEqual("MainStream", profiles.Profiles[0].Name);
            }
        }

        [TestMethod]
        public async Task FollowsTheAdvertisedAddressForEveryLaterCall()
        {
            using (var client = new SimpleOnvifClient(_endpoint))
            {
                var stream = await client.GetStreamUriAsync("p0");
                Assert.AreEqual("rtsp://localhost:8554/p0", stream.Uri);
            }
        }

        [TestMethod]
        public async Task ReportsAServiceTheDeviceDoesNotSupport()
        {
            using (var client = new SimpleOnvifClient(_endpoint))
            {
                // PTZ is not advertised, so resolving it has to fail with something that says so
                // rather than calling an address that does not exist.
                await Assert.ThrowsExactlyAsync<NotSupportedException>(
                    () => client.GetStatusAsync("p0"));
            }
        }
    }
}
