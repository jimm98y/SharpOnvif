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
using SharpOnvifServer;
using SharpOnvifServer.DeviceMgmt;
using SharpOnvifServer.Dispatch;
using SharpOnvifServer.Security;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// A device that demands credentials, configured however the test needs, so that an
    /// authentication scheme can be exercised the way it is actually used: a real client
    /// authenticating to a real server over a socket.
    /// </summary>
    internal sealed class AuthenticatedDevice : IAsyncDisposable
    {
        public const string UserName = "admin";
        public const string Password = "password";
        public const string Manufacturer = "ACME";

        private const string EndpointPath = "/onvif/device_service";

        private readonly WebApplication _app;

        private AuthenticatedDevice(WebApplication app, string endpoint)
        {
            _app = app;
            Endpoint = endpoint;
        }

        /// <summary>The address a client talks to.</summary>
        public string Endpoint { get; }

        /// <summary>The one user this device knows.</summary>
        private sealed class OneUser : IUserRepository
        {
            public UserInfo GetUser(string userName) =>
                string.Equals(userName, UserName, StringComparison.Ordinal)
                    ? new UserInfo { UserName = UserName, Password = Password }
                    : null;

            public Task<UserInfo> GetUserAsync(string userName) => Task.FromResult(GetUser(userName));

            public UserInfo GetUserByHash(string algorithm, string userNameHash, string realm)
            {
                // userhash=true: the client sends H(username:realm) instead of the name, so the
                // device has to recognise its users by that hash.
                string expected = SharpOnvifCommon.Security.HttpDigestAuthentication
                    .CreateUserNameHashRFC7616(algorithm, UserName, realm);

                return string.Equals(userNameHash, expected, StringComparison.OrdinalIgnoreCase)
                    ? new UserInfo { UserName = UserName, Password = Password }
                    : null;
            }

            public Task<UserInfo> GetUserByHashAsync(string algorithm, string userNameHash, string realm) =>
                Task.FromResult(GetUserByHash(algorithm, userNameHash, realm));
        }

        private sealed class DeviceImpl : DeviceBase
        {
            public override GetDeviceInformationResponse GetDeviceInformation() =>
                new GetDeviceInformationResponse { Manufacturer = Manufacturer, Model = "Test" };
        }

        /// <summary>Starts a device with the authentication options the test wants.</summary>
        public static async Task<AuthenticatedDevice> StartAsync(
            Action<DigestAuthenticationSchemeOptions> configure = null)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            builder.Services.AddSingleton<IUserRepository, OneUser>();
            builder.Services.AddOnvifDigestAuthentication(options =>
            {
                // A short realm of our own, so a test can tell this device from any other.
                options.HttpDigestRealm = "SharpOnvif Test";
                configure?.Invoke(options);
            });
            builder.Services.AddSingleton<DeviceImpl>();

            WebApplication app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapOnvifService<DeviceImpl>(EndpointPath);

            await app.StartAsync();

            string address = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>().Addresses.First();

            return new AuthenticatedDevice(app, address.TrimEnd('/') + EndpointPath);
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
