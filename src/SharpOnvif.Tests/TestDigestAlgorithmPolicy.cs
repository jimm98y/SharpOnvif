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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpOnvifCommon.Security;
using SharpOnvifServer;
using SharpOnvifServer.Dispatch;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// A device that offers only SHA-256 has to refuse MD5.
    /// <para>
    /// The algorithm arrives in the request, so a device that does not check it lets any client
    /// pick the weakest one available no matter what was configured - and an unrecognised name
    /// used to fall through to MD5 as well. Neither defeats authentication on its own, but both
    /// make captured exchanges easier to attack offline and make the configuration meaningless.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestDigestAlgorithmPolicy
    {
        private const string Path = "/onvif/device_service";
        private const string Realm = "SHA-256 only";
        private const string UserName = "admin";
        private const string Password = "password";
        private const string Action = "http://www.onvif.org/ver10/device/wsdl/GetDeviceInformation";

        private static WebApplication _app;
        private static string _endpoint;

        private sealed class UserRepository : IUserRepository
        {
            public UserInfo GetUser(string userName) =>
                userName == UserName ? new UserInfo { UserName = UserName, Password = Password } : null;

            public Task<UserInfo> GetUserAsync(string userName) => Task.FromResult(GetUser(userName));

            public UserInfo GetUserByHash(string algorithm, string userName, string realm)
            {
                string hashed = HttpDigestAuthentication.CreateUserNameHashRFC7616(algorithm, UserName, realm);
                return userName == hashed ? GetUser(UserName) : null;
            }

            public Task<UserInfo> GetUserByHashAsync(string algorithm, string userName, string realm) =>
                Task.FromResult(GetUserByHash(algorithm, userName, realm));
        }

        private sealed class DeviceImpl : SharpOnvifServer.DeviceMgmt.DeviceBase
        {
            public override SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse GetDeviceInformation() =>
                new SharpOnvifServer.DeviceMgmt.GetDeviceInformationResponse { Manufacturer = "ACME" };
        }

        [ClassInitialize]
        public static async Task StartServer(TestContext context)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            builder.Services.AddSingleton<IUserRepository, UserRepository>();
            builder.Services.AddOnvifDigestAuthentication(options =>
            {
                options.HttpDigestRealm = Realm;
                options.HttpDigestAlgorithms = ["SHA-256"];
                options.HttpDigestUserHash = false;
                options.PreAuthActions = [];
            });
            builder.Services.AddSingleton<DeviceImpl>();

            _app = builder.Build();
            _app.UseAuthentication();
            _app.UseAuthorization();
            _app.MapOnvifService<DeviceImpl>(Path);

            await _app.StartAsync();

            _endpoint = _app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>().Addresses.First().TrimEnd('/') + Path;
        }

        [ClassCleanup]
        public static async Task StopServer()
        {
            if (_app != null) await _app.StopAsync();
        }

        private const string Envelope =
            "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
            "<s:Body><GetDeviceInformation xmlns=\"http://www.onvif.org/ver10/device/wsdl\"/></s:Body></s:Envelope>";

        private static HttpRequestMessage Request()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(Envelope),
            };
            request.Content.Headers.ContentType =
                MediaTypeHeaderValue.Parse($"application/soap+xml; charset=utf-8; action=\"{Action}\"");
            return request;
        }

        /// <summary>Answers the device's challenge with a digest computed using one algorithm.</summary>
        private static async Task<HttpStatusCode> AuthenticateWithAsync(string algorithm)
        {
            using (var http = new HttpClient())
            {
                var challenged = await http.SendAsync(Request());
                Assert.AreEqual(HttpStatusCode.Unauthorized, challenged.StatusCode);

                string challenge = challenged.Headers.WwwAuthenticate.First().Parameter;
                string nonce = HttpDigestAuthentication.GetValueFromHeader(challenge, "nonce", true);
                string opaque = HttpDigestAuthentication.GetValueFromHeader(challenge, "opaque", true);
                string clientNonce = HttpDigestAuthentication.GenerateClientNonce(BinarySerializationType.Hex);

                var request = Request();
                string uri = request.RequestUri.PathAndQuery;

                string response = HttpDigestAuthentication.CreateWebDigestRFC7616(
                    algorithm, UserName, Realm, Password, false, nonce, "POST", uri,
                    1, clientNonce, "auth", null, nonce, clientNonce);

                request.Headers.TryAddWithoutValidation("Authorization",
                    HttpDigestAuthentication.CreateAuthorizationRFC7616(
                        UserName, Realm, nonce, uri, response, opaque, algorithm, "auth", 1, clientNonce, false));

                return (await http.SendAsync(request)).StatusCode;
            }
        }

        [TestMethod]
        public async Task AcceptsTheAlgorithmItOffers()
        {
            Assert.AreEqual(HttpStatusCode.OK, await AuthenticateWithAsync("SHA-256"));
        }

        [TestMethod]
        public async Task RefusesMD5WhenOnlySha256IsOffered()
        {
            // A correct MD5 digest, from a client that knows the password - and still refused,
            // because MD5 is not what this device offered.
            Assert.AreEqual(HttpStatusCode.Unauthorized, await AuthenticateWithAsync("MD5"));
        }

        [TestMethod]
        public async Task RefusesAnAlgorithmNobodyImplements()
        {
            using (var http = new HttpClient())
            {
                var challenged = await http.SendAsync(Request());
                string challenge = challenged.Headers.WwwAuthenticate.First().Parameter;
                string nonce = HttpDigestAuthentication.GetValueFromHeader(challenge, "nonce", true);

                var request = Request();
                request.Headers.TryAddWithoutValidation("Authorization",
                    $"Digest username=\"{UserName}\", realm=\"{Realm}\", uri=\"{request.RequestUri.PathAndQuery}\", " +
                    $"algorithm=NOT-AN-ALGORITHM, nonce=\"{nonce}\", qop=auth, nc=00000001, cnonce=\"abc\", response=\"00\"");

                var response = await http.SendAsync(request);
                Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            }
        }
    }
}
