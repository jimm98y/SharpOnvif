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
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Soap;
using SharpOnvifCommon.Xml;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// WS-UsernameToken, the older of the two Onvif authentication schemes: the credentials travel
    /// in a SOAP security header rather than in an HTTP one, as
    /// <c>Base64(SHA1(nonce + created + password))</c>.
    /// <para>
    /// Tested through a real client and a real server, because the digest arithmetic agreeing with
    /// the specification is not the same thing as the two sides agreeing with each other. It does
    /// not test the arithmetic: both sides compute it with the same code, so they agree whatever
    /// it produces. TestWsUsernamePassword pins that against the specification's values, which is
    /// what says a real camera will accept the token.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestWsUsernameTokenEndToEnd
    {
        private static DeviceClient Client(
            AuthenticatedDevice device,
            string password = AuthenticatedDevice.Password,
            TimeSpan utcNowOffset = default)
        {
            return new DeviceClient(device.Endpoint, new OnvifClientSettings
            {
                Credentials = new System.Net.NetworkCredential(AuthenticatedDevice.UserName, password),
                Authentication = new OnvifClientAuthentication(DigestAuthentication.WsUsernameToken),
                UtcNowOffset = utcNowOffset,
            });
        }

        [TestMethod]
        public async Task AuthenticatesWithATokenTheDeviceAccepts()
        {
            await using var device = await AuthenticatedDevice.StartAsync(
                options => options.Onvif.Authentication = DigestAuthentication.WsUsernameToken);

            using var client = Client(device);

            var info = await client.GetDeviceInformationAsync();

            Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer);
        }

        [TestMethod]
        public async Task StaysAuthenticatedAcrossSeveralCalls()
        {
            // Each call carries its own token with its own nonce and timestamp - there is no
            // session - so a scheme that worked once has to keep working.
            await using var device = await AuthenticatedDevice.StartAsync(
                options => options.Onvif.Authentication = DigestAuthentication.WsUsernameToken);

            using var client = Client(device);

            for (int call = 0; call < 3; call++)
            {
                var info = await client.GetDeviceInformationAsync();
                Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer, $"call {call}");
            }
        }

        [TestMethod]
        public async Task RefusesTheWrongPassword()
        {
            await using var device = await AuthenticatedDevice.StartAsync(
                options => options.Onvif.Authentication = DigestAuthentication.WsUsernameToken);

            using var client = Client(device, password: "not the password");

            await Assert.ThrowsExactlyAsync<SoapFaultException>(() => client.GetDeviceInformationAsync());
        }

        [TestMethod]
        public async Task RefusesARequestCarryingNoToken()
        {
            await using var device = await AuthenticatedDevice.StartAsync(
                options => options.Onvif.Authentication = DigestAuthentication.WsUsernameToken);

            using var anonymous = new DeviceClient(device.Endpoint);

            await Assert.ThrowsExactlyAsync<SoapFaultException>(() => anonymous.GetDeviceInformationAsync());
        }

        [TestMethod]
        public async Task RefusesATokenTimestampedTooLongAgo()
        {
            // The Created time is what stops a captured token being replayed for ever, so a device
            // refuses one that is too old. Anything beyond the window is stale.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
            {
                options.Onvif.Authentication = DigestAuthentication.WsUsernameToken;
                options.WsUsernameTokenMaxTimeDeltaInMilliseconds = 2000;
            });

            using var client = Client(device, utcNowOffset: TimeSpan.FromMinutes(-10));

            await Assert.ThrowsExactlyAsync<SoapFaultException>(() => client.GetDeviceInformationAsync());
        }

        [TestMethod]
        public async Task RefusesATokenTimestampedInTheFuture()
        {
            await using var device = await AuthenticatedDevice.StartAsync(options =>
            {
                options.Onvif.Authentication = DigestAuthentication.WsUsernameToken;
                options.WsUsernameTokenMaxTimeDeltaInMilliseconds = 2000;
            });

            using var client = Client(device, utcNowOffset: TimeSpan.FromMinutes(10));

            await Assert.ThrowsExactlyAsync<SoapFaultException>(() => client.GetDeviceInformationAsync());
        }

        [TestMethod]
        public async Task AcceptsAnOffsetThatCorrectsACameraWithAWrongClock()
        {
            // The offset exists for the opposite case: a device whose own clock is wrong. The
            // client stamps its token in the device's time rather than its own, and the device -
            // which believes its clock - accepts it. Here the device's clock is right and the
            // window is wide, so an offset inside the window still authenticates.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
            {
                options.Onvif.Authentication = DigestAuthentication.WsUsernameToken;
                options.WsUsernameTokenMaxTimeDeltaInMilliseconds = 300000; // five minutes
            });

            using var client = Client(device, utcNowOffset: TimeSpan.FromMinutes(1));

            var info = await client.GetDeviceInformationAsync();

            Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer);
        }

        [TestMethod]
        public async Task AcceptsAnyAgeWhenTheDeviceDoesNotCheckTheClock()
        {
            // A negative window disables the check, for a device that cannot keep time at all.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
            {
                options.Onvif.Authentication = DigestAuthentication.WsUsernameToken;
                options.WsUsernameTokenMaxTimeDeltaInMilliseconds = -1;
            });

            using var client = Client(device, utcNowOffset: TimeSpan.FromHours(-3));

            var info = await client.GetDeviceInformationAsync();

            Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer);
        }

        [TestMethod]
        public async Task StillAuthenticatesWhenTheDeviceAlsoOffersHttpDigest()
        {
            // What a real device does: both schemes accepted, the client choosing. A client that
            // only knows WS-UsernameToken has to get in.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
                options.Onvif.Authentication = DigestAuthentication.WsUsernameToken | DigestAuthentication.HttpDigest);

            using var client = Client(device);

            var info = await client.GetDeviceInformationAsync();

            Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer);
        }

        [TestMethod]
        public async Task RefusesATokenWhenTheDeviceOnlyTakesHttpDigest()
        {
            // The other way round: a device that has switched the old scheme off must not accept
            // it, however well-formed the token is.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
                options.Onvif.Authentication = DigestAuthentication.HttpDigest);

            using var client = Client(device);

            await Assert.ThrowsExactlyAsync<SoapFaultException>(() => client.GetDeviceInformationAsync());
        }
    }
}
