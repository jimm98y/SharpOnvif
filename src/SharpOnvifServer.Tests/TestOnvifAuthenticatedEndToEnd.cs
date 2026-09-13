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
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Soap;
using SharpOnvifServer;
using SharpOnvifServer.Dispatch;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The client authenticating to the server, both sides being SharpOnvif.
    /// <para>
    /// This is the interop test that matters most: the digest challenge the server generates and
    /// the response the client computes are written independently, and only a real exchange shows
    /// that they agree. It also pins down that an Onvif endpoint refuses an unauthenticated
    /// request while still answering the PRE_AUTH operations the specification exempts.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestOnvifAuthenticatedEndToEnd
    {
        private const string EndpointPath = "/onvif/device_service";
        private const string UserName = "admin";
        private const string Password = "password";

        private static WebApplication _app;
        private static string _endpoint;

        private sealed class UserRepository : IUserRepository
        {
            public UserInfo GetUser(string userName)
            {
                return string.Equals(userName, UserName, StringComparison.Ordinal)
                    ? new UserInfo { UserName = UserName, Password = Password }
                    : null;
            }

            public Task<UserInfo> GetUserAsync(string userName)
            {
                return Task.FromResult(GetUser(userName));
            }

            public UserInfo GetUserByHash(string algorithm, string userName, string realm)
            {
                string hashed = HttpDigestAuthentication.CreateUserNameHashRFC7616(algorithm, UserName, realm);
                return string.Equals(userName, hashed, StringComparison.Ordinal) ? GetUser(UserName) : null;
            }

            public Task<UserInfo> GetUserByHashAsync(string algorithm, string userName, string realm)
            {
                return Task.FromResult(GetUserByHash(algorithm, userName, realm));
            }
        }

        private sealed class DeviceImpl : SharpOnvifServer.DeviceMgmt.DeviceBase
        {
            public override SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse GetDeviceInformation()
            {
                return new SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse
                {
                    Manufacturer = "ACME",
                    Model = "Vaultboy 9000",
                };
            }

            /// <summary>A PRE_AUTH operation, which the specification says must answer unauthenticated.</summary>
            public override SharpOnvifServer.DeviceMgmt.GetSystemDateAndTimeResponse GetSystemDateAndTime()
            {
                return new SharpOnvifServer.DeviceMgmt.GetSystemDateAndTimeResponse
                {
                    SystemDateAndTime = new SharpOnvifCommon.Onvif.SystemDateTime
                    {
                        DateTimeType = SharpOnvifCommon.Onvif.SetDateTimeType.NTP,
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

            builder.Services.AddSingleton<IUserRepository, UserRepository>();
            builder.Services.AddOnvifDigestAuthentication();
            builder.Services.AddSingleton<DeviceImpl>();

            _app = builder.Build();
            _app.UseAuthentication();
            _app.UseAuthorization();
            _app.MapOnvifService<DeviceImpl>(EndpointPath);

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
        public async Task AuthenticatesWithTheCredentialsTheDeviceExpects()
        {
            using (var device = new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint, UserName, Password))
            {
                var info = await device.GetDeviceInformationAsync();

                Assert.AreEqual("ACME", info.Manufacturer);
                Assert.AreEqual("Vaultboy 9000", info.Model);
            }
        }

        [TestMethod]
        public async Task StaysAuthenticatedAcrossSeveralCalls()
        {
            // The challenge is negotiated once and reused, and the nonce count advances, so a
            // device that rejects a replayed count still accepts every call.
            using (var device = new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint, UserName, Password))
            {
                for (int i = 0; i < 5; i++)
                {
                    var info = await device.GetDeviceInformationAsync();
                    Assert.AreEqual("ACME", info.Manufacturer, $"call {i + 1} failed");
                }
            }
        }

        [TestMethod]
        public async Task RefusesAnOperationWithoutCredentials()
        {
            using (var device = new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint))
            {
                await Assert.ThrowsExactlyAsync<SharpOnvifCommon.Xml.SoapFaultException>(
                    () => device.GetDeviceInformationAsync());
            }
        }

        [TestMethod]
        public async Task AnswersAPreAuthOperationWithoutCredentials()
        {
            using (var device = new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint))
            {
                var response = await device.GetSystemDateAndTimeAsync();

                Assert.AreEqual(SharpOnvifCommon.Onvif.SetDateTimeType.NTP,
                    response.SystemDateAndTime.DateTimeType);
            }
        }

        [TestMethod]
        public async Task ChallengesWithADigestHeaderRatherThanForbidding()
        {
            // A client can only authenticate if it is told how: it needs 401 plus a
            // WWW-Authenticate challenge to compute its response. A bare 403 would leave it
            // with nowhere to go, so the distinction is worth asserting rather than just
            // checking that the request was refused.
            using (var http = new System.Net.Http.HttpClient())
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, _endpoint);
                request.Content = new System.Net.Http.StringContent(
                    "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                    "<s:Body><GetDeviceInformation xmlns=\"http://www.onvif.org/ver10/device/wsdl\"/></s:Body></s:Envelope>");
                request.Content.Headers.ContentType =
                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse(
                        "application/soap+xml; charset=utf-8; action=\"http://www.onvif.org/ver10/device/wsdl/GetDeviceInformation\"");

                var response = await http.SendAsync(request);

                Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.IsTrue(response.Headers.WwwAuthenticate.Any(h => h.Scheme == "Digest"),
                    "the refusal has to carry a Digest challenge");

                // Each offered algorithm appears once. Binding configuration onto the options
                // appends to their defaults, which has doubled this list before now.
                var challenges = response.Headers.WwwAuthenticate.Select(h => h.Parameter).ToList();
                CollectionAssert.AllItemsAreUnique(challenges.Select(Algorithm).ToList(),
                    "an algorithm must not be offered twice");
            }
        }

        private static string Algorithm(string challenge)
        {
            return SharpOnvifCommon.Security.HttpDigestAuthentication
                .GetValueFromHeader(challenge, "algorithm", false) ?? "MD5";
        }

        [TestMethod]
        public async Task ProvesItsOwnIdentityWithAuthenticationInfo()
        {
            // The other half of HTTP Digest: rspauth is the device's digest over its own
            // response, and shows the client it is talking to something that knows the password.
            // The client validates it, so a device that stops sending it leaves that unchecked.
            using (var http = new System.Net.Http.HttpClient(
                       new HttpDigestHandler(
                           new System.Net.NetworkCredential(UserName, Password),
                           new OnvifAuthenticationSettings(),
                           new System.Net.Http.HttpClientHandler())))
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, _endpoint);
                request.Content = new System.Net.Http.StringContent(
                    "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                    "<s:Body><GetDeviceInformation xmlns=\"http://www.onvif.org/ver10/device/wsdl\"/></s:Body></s:Envelope>");
                request.Content.Headers.ContentType =
                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse(
                        "application/soap+xml; charset=utf-8; action=\"http://www.onvif.org/ver10/device/wsdl/GetDeviceInformation\"");

                var response = await http.SendAsync(request);

                Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
                Assert.IsTrue(response.Headers.TryGetValues("Authentication-Info", out var values),
                    "the device has to prove itself as well");
                StringAssert.Contains(string.Join(", ", values), "rspauth=");
            }
        }

        [TestMethod]
        public async Task ValidatesTheDevicesProofOnEveryCall()
        {
            // The client throws when rspauth does not check out, so a clean run of several calls
            // is the assertion that the device's half of the exchange is right.
            using (var device = new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint, UserName, Password))
            {
                for (int i = 0; i < 3; i++)
                    Assert.AreEqual("ACME", (await device.GetDeviceInformationAsync()).Manufacturer);
            }
        }

        [TestMethod]
        public void RefusesToDropHttpDigestSilentlyForASuppliedHttpClient()
        {
            // Digest takes a handler in the pipeline, which cannot be added to a built client.
            // Accepting the client and sending unauthenticated requests would be the worst
            // outcome, so the combination is refused.
            var settings = new SharpOnvifCommon.Soap.OnvifClientSettings
            {
                Credentials = new System.Net.NetworkCredential(UserName, Password),
                Authentication = new OnvifClientAuthentication(DigestAuthentication.HttpDigest),
                HttpClient = new System.Net.Http.HttpClient(),
            };

            var error = Assert.ThrowsExactly<InvalidOperationException>(
                () => new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint, settings));

            StringAssert.Contains(error.Message, "Transport");
        }

        [TestMethod]
        public void AcceptsASuppliedHttpClientWhenDigestIsNotAskedFor()
        {
            var settings = new SharpOnvifCommon.Soap.OnvifClientSettings
            {
                Credentials = new System.Net.NetworkCredential(UserName, Password),
                Authentication = new OnvifClientAuthentication(DigestAuthentication.WsUsernameToken),
                HttpClient = new System.Net.Http.HttpClient(),
            };

            using (var device = new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint, settings))
            {
                Assert.IsNotNull(device);
            }
        }

        [TestMethod]
        public async Task RefusesTheWrongPassword()
        {
            using (var device = new SharpOnvifClient.DeviceMgmt.DeviceClient(_endpoint, UserName, "not-the-password"))
            {
                await Assert.ThrowsExactlyAsync<SharpOnvifCommon.Xml.SoapFaultException>(
                    () => device.GetDeviceInformationAsync());
            }
        }
    }
}
