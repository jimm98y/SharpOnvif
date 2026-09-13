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
using SharpOnvifCommon.Xml;
using SharpOnvifServer.Dispatch;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// A generated client talking to a generated service over real HTTP, which exercises the
    /// whole stack at once: the SOAP envelope, the action routing, both call styles on the
    /// client, both override styles on the server, and the fault path.
    /// </summary>
    [TestClass]
    public sealed class TestOnvifEndToEnd
    {
        private const string EndpointPath = "/onvif/device_service";

        private static WebApplication _app;
        private static string _endpoint;

        /// <summary>Answers a few operations, overriding the unwrapped form.</summary>
        private sealed class DeviceImpl : SharpOnvifServer.DeviceMgmt.DeviceBase
        {
            public override SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse GetDeviceInformation()
            {
                return new SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse
                {
                    Manufacturer = "ACME",
                    Model = "Vaultboy 9000",
                    FirmwareVersion = "1.2.3",
                    SerialNumber = "SN-000042",
                    HardwareId = "HW-1",
                };
            }

            /// <summary>Overrides the wrapper form instead, which the dispatcher reaches equally.</summary>
            public override SharpOnvifServer.DeviceMgmt.GetServicesResponse GetServices(
                SharpOnvifServer.DeviceMgmt.GetServicesRequest request)
            {
                return new SharpOnvifServer.DeviceMgmt.GetServicesResponse
                {
                    Service = new[]
                    {
                        new SharpOnvifServer.DeviceMgmt.Service
                        {
                            Namespace = "http://www.onvif.org/ver10/device/wsdl",
                            XAddr = "http://localhost" + EndpointPath,
                            Version = new SharpOnvifCommon.Onvif.OnvifVersion { Major = 2, Minor = 6 },
                        },
                        new SharpOnvifServer.DeviceMgmt.Service
                        {
                            Namespace = "http://www.onvif.org/ver10/media/wsdl",
                            XAddr = "http://localhost" + EndpointPath,
                            Version = new SharpOnvifCommon.Onvif.OnvifVersion { Major = 2, Minor = 6 },
                        },
                    },
                };
            }

            public override SharpOnvifServer.DeviceMgmt.GetSystemDateAndTimeResponse GetSystemDateAndTime()
            {
                return new SharpOnvifServer.DeviceMgmt.GetSystemDateAndTimeResponse
                {
                    SystemDateAndTime = new SharpOnvifCommon.Onvif.SystemDateTime
                    {
                        DateTimeType = SharpOnvifCommon.Onvif.SetDateTimeType.Manual,
                        UTCDateTime = new SharpOnvifCommon.Onvif.OnvifDateTime
                        {
                            Date = new SharpOnvifCommon.Onvif.Date { Year = 2026, Month = 9, Day = 12 },
                            Time = new SharpOnvifCommon.Onvif.Time { Hour = 21, Minute = 34, Second = 7 },
                        },
                    },
                };
            }
        }

        /// <summary>A second service sharing the one endpoint.</summary>
        private sealed class MediaImpl : SharpOnvifServer.Media.MediaBase
        {
            public override SharpOnvifServer.Media.GetProfilesResponse GetProfiles(
                SharpOnvifServer.Media.GetProfilesRequest request)
            {
                return new SharpOnvifServer.Media.GetProfilesResponse
                {
                    Profiles = new[]
                    {
                        new SharpOnvifCommon.Onvif.Profile
                        {
                            token = "profile0",
                            Name = "MainStream",
                            VideoEncoderConfiguration = new SharpOnvifCommon.Onvif.VideoEncoderConfiguration
                            {
                                token = "venc0",
                                Name = "H264",
                                UseCount = 1,
                                Encoding = SharpOnvifCommon.Onvif.VideoEncoding.H264,
                                Resolution = new SharpOnvifCommon.Onvif.VideoResolution { Width = 1920, Height = 1080 },
                                Quality = 4.5f,
                            },
                        },
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

            // Both services on one URL, told apart by the SOAP action.
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
        public async Task CallsAnOperationTheServiceAnsweredFromTheUnwrappedForm()
        {
            using (var device = new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint))
            {
                var info = await device.GetDeviceInformationAsync();

                Assert.AreEqual("ACME", info.Manufacturer);
                Assert.AreEqual("Vaultboy 9000", info.Model);
                Assert.AreEqual("SN-000042", info.SerialNumber);
            }
        }

        [TestMethod]
        public async Task CallsAnOperationWithItsRequestMembersAsArguments()
        {
            using (var device = new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint))
            {
                var services = await device.GetServicesAsync(false);

                Assert.AreEqual(2, services.Service.Length);
                Assert.AreEqual("http://www.onvif.org/ver10/media/wsdl", services.Service[1].Namespace);
                Assert.AreEqual(2, services.Service[0].Version.Major);
            }
        }

        [TestMethod]
        public async Task CallsTheSameOperationWithItsRequestWrapper()
        {
            using (var device = new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint))
            {
                var request = new SharpOnvifClient.DeviceMgmt.GetServicesRequest(true);
                var services = await device.GetServicesAsync(request);

                Assert.AreEqual(2, services.Service.Length);
            }
        }

        [TestMethod]
        public async Task ReturnsTheResponseContractFromEitherOverload()
        {
            using (var device = new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint))
            {
                // Both call styles return the response contract; the unwrapped overload only
                // saves the caller from building the request.
                var fromEmptyRequest = await device.GetSystemDateAndTimeAsync();
                var fromWrapper = await device.GetSystemDateAndTimeAsync(
                    new SharpOnvifClient.DeviceMgmt.GetSystemDateAndTimeRequest());

                foreach (var response in new[] { fromEmptyRequest, fromWrapper })
                {
                    var time = response.SystemDateAndTime;
                    Assert.AreEqual(2026, time.UTCDateTime.Date.Year);
                    Assert.AreEqual(34, time.UTCDateTime.Time.Minute);
                    Assert.AreEqual(SharpOnvifCommon.Onvif.SetDateTimeType.Manual, time.DateTimeType);
                }
            }
        }

        [TestMethod]
        public async Task ReachesASecondServiceOnTheSameEndpoint()
        {
            using (var media = new SharpOnvifClient.Media.MediaClient(_endpoint))
            {
                var profiles = await media.GetProfilesAsync();

                Assert.AreEqual(1, profiles.Profiles.Length);
                Assert.AreEqual("MainStream", profiles.Profiles[0].Name);
                Assert.AreEqual("profile0", profiles.Profiles[0].token);
            }
        }

        [TestMethod]
        public async Task CarriesInheritedMembersAcrossTheWire()
        {
            using (var media = new SharpOnvifClient.Media.MediaClient(_endpoint))
            {
                var profiles = await media.GetProfilesAsync();
                var encoder = profiles.Profiles[0].VideoEncoderConfiguration;

                Assert.AreEqual(SharpOnvifCommon.Onvif.VideoEncoding.H264, encoder.Encoding);
                Assert.AreEqual(1920, encoder.Resolution.Width);
                Assert.AreEqual(4.5f, encoder.Quality);

                // Name and UseCount come from ConfigurationEntity, which the type extends.
                Assert.AreEqual("H264", encoder.Name);
                Assert.AreEqual(1, encoder.UseCount);
            }
        }

        [TestMethod]
        public async Task ReportsAnUnimplementedOperationAsAFault()
        {
            using (var device = new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint))
            {
                var fault = await Assert.ThrowsExactlyAsync<SoapFaultException>(
                    () => device.GetHostnameAsync());

                Assert.AreEqual("ActionNotSupported", fault.Fault.Subcode);
            }
        }

        [TestMethod]
        public async Task ReportsAnUnknownActionAsAFault()
        {
            // The PTZ service is not published here, so its actions have nowhere to go.
            using (var ptz = new SharpOnvifClient.PTZ.PTZClient(_endpoint))
            {
                var fault = await Assert.ThrowsExactlyAsync<SoapFaultException>(
                    () => ptz.GetConfigurationsAsync());

                Assert.AreEqual("ActionNotSupported", fault.Fault.Subcode);
            }
        }
    }
}
