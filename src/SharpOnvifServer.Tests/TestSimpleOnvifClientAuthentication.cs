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
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Soap;
using SharpOnvifCommon.Xml;
using DeviceOptions = SharpOnvifServer.Security.DigestAuthenticationSchemeOptions;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// How <see cref="SimpleOnvifClient"/> is told to authenticate.
    /// <para>
    /// It is told with the same objects a generated client is, and the description inside them is
    /// the same one a device is configured with. One vocabulary for a negotiation, rather than a
    /// type per place it is mentioned.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestSimpleOnvifClientAuthentication
    {
        [TestMethod]
        public void StartsWhereTheDeviceStarts()
        {
            // Two sides of one negotiation, described by one type. A client that asked for what no
            // device offers, or a device that offered what no client asks for, would be this test
            // failing.
            var client = new OnvifAuthenticationSettings().Options;
            var device = new DeviceOptions().Onvif;

            Assert.AreEqual(device.Authentication, client.Authentication);
            Assert.AreEqual(device.HttpDigestUserHash, client.HttpDigestUserHash);
            CollectionAssert.AreEqual(device.HttpDigestAlgorithms, client.HttpDigestAlgorithms);
            CollectionAssert.AreEqual(device.HttpDigestQop, client.HttpDigestQop);
            CollectionAssert.AreEqual(device.PreAuthActions, client.PreAuthActions);
        }

        [TestMethod]
        public void KeepsWhatItWasGiven()
        {
            // The caller's object, as given. Copying it would make a plain
            // OnvifAuthenticationSettings of anything derived from one - including the name in
            // SharpOnvifClient.Security, and anything a caller writes of their own.
            var authentication = new SharpOnvifClient.Security.DigestAuthenticationSchemeOptions(
                DigestAuthentication.WsUsernameToken);

            using var client = new Probe("http://127.0.0.1:1/onvif/device_service", "u", "p", authentication);

            Assert.AreSame(authentication, client.Authentication);
        }

        [TestMethod]
        public void TakesTheNameClientCodeAlreadyWrites()
        {
            // The constructor 0.9.x code calls, spelled the way it spells it. This compiling is
            // the whole of the test.
            using var client = new SimpleOnvifClient(
                "http://127.0.0.1:1/onvif/device_service", "admin", "password",
                new SharpOnvifClient.Security.DigestAuthenticationSchemeOptions(
                    DigestAuthentication.WsUsernameToken | DigestAuthentication.HttpDigest));

            Assert.IsNotNull(client);
        }

        [TestMethod]
        public async Task StampsItsTokenWithTheOffsetItWasGiven()
        {
            // A camera whose clock is wrong is what the offset is for, and the only way to see it
            // reaching the token is a device that refuses what falls outside its window.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
            {
                options.Onvif.Authentication = DigestAuthentication.WsUsernameToken;
                options.WsUsernameTokenMaxTimeDeltaInMilliseconds = 2000;
            });

            using var client = new SimpleOnvifClient(
                device.Endpoint, AuthenticatedDevice.UserName, AuthenticatedDevice.Password,
                new OnvifAuthenticationSettings(DigestAuthentication.WsUsernameToken));

            client.SetCameraUtcNowOffset(TimeSpan.FromMinutes(-10));

            await Assert.ThrowsExactlyAsync<SoapFaultException>(() => client.GetDeviceInformationAsync());
        }

        [TestMethod]
        public async Task AuthenticatesWithAnOffsetInsideTheDevicesWindow()
        {
            // The other direction, so the test above is failing for the offset rather than for the
            // client being broken.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
            {
                options.Onvif.Authentication = DigestAuthentication.WsUsernameToken;
                options.WsUsernameTokenMaxTimeDeltaInMilliseconds = 300000;
            });

            using var client = new SimpleOnvifClient(
                device.Endpoint, AuthenticatedDevice.UserName, AuthenticatedDevice.Password,
                new OnvifAuthenticationSettings(DigestAuthentication.WsUsernameToken));

            client.SetCameraUtcNowOffset(TimeSpan.FromMinutes(1));

            var info = await client.GetDeviceInformationAsync();

            Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer);
        }

        [TestMethod]
        public async Task RefusesAnOffsetForASchemeThatCarriesNoTime()
        {
            // HTTP Digest stamps nothing, so there is nothing for an offset to correct and saying
            // otherwise would be quietly ignored.
            await using var device = await AuthenticatedDevice.StartAsync();

            using var client = new SimpleOnvifClient(
                device.Endpoint, AuthenticatedDevice.UserName, AuthenticatedDevice.Password,
                new OnvifAuthenticationSettings(DigestAuthentication.HttpDigest));

            Assert.ThrowsExactly<NotSupportedException>(
                () => client.SetCameraUtcNowOffset(TimeSpan.FromMinutes(1)));
        }

        /// <summary>Reaches what the client kept, which is protected rather than public.</summary>
        private sealed class Probe : SimpleOnvifClient
        {
            public Probe(string uri, string userName, string password, OnvifAuthenticationSettings authentication)
                : base(uri, userName, password, authentication)
            {
            }

            public IClientAuthentication Authentication { get { return _settings.Authentication; } }
        }
    }
}
