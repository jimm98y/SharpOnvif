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
using System.Threading.Tasks;
using SharpOnvifClient;
using SharpOnvifClient.Security;
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Xml;
using ClientOptions = SharpOnvifClient.Security.DigestAuthenticationSchemeOptions;
using DeviceOptions = SharpOnvifServer.Security.DigestAuthenticationSchemeOptions;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The options a client is configured with, beside the ones a device is.
    /// <para>
    /// Both carry what the two sides have to agree on in one shared type, and their own business
    /// beside it. Two shapes for one negotiation is how the descriptions of it drift apart.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestClientAuthenticationOptions
    {
        [TestMethod]
        public void IsShapedLikeTheOptionsADeviceIsGiven()
        {
            var client = new ClientOptions();
            var device = new DeviceOptions();

            // The same type, describing the same negotiation, reached by the same name.
            Assert.IsInstanceOfType<OnvifAuthenticationSettings>(client.Onvif);
            Assert.IsInstanceOfType<OnvifAuthenticationSettings>(device.Onvif);

            Assert.AreEqual(device.Onvif.Authentication, client.Onvif.Authentication);
            CollectionAssert.AreEqual(device.Onvif.HttpDigestAlgorithms, client.Onvif.HttpDigestAlgorithms);
            CollectionAssert.AreEqual(device.Onvif.HttpDigestQop, client.Onvif.HttpDigestQop);
            Assert.AreEqual(device.Onvif.HttpDigestUserHash, client.Onvif.HttpDigestUserHash);
            CollectionAssert.AreEqual(device.Onvif.PreAuthActions, client.Onvif.PreAuthActions);
        }

        [TestMethod]
        public void KeepsTheClientsOwnSettingsToItself()
        {
            // A clock offset is the client's business, as a realm and a nonce lifetime are the
            // device's. It sits beside what is negotiated rather than inside it.
            var client = new ClientOptions { UtcNowOffset = TimeSpan.FromMinutes(3) };

            Assert.AreEqual(TimeSpan.FromMinutes(3), client.UtcNowOffset);
        }

        [TestMethod]
        public void SurvivesBeingGivenNoSharedSettings()
        {
            var options = new ClientOptions { Onvif = null };

            Assert.IsNotNull(options.Onvif, "the client would throw on the next read");
        }

        [TestMethod]
        public void NamesTheSchemesItWasConstructedWith()
        {
            var options = new ClientOptions(DigestAuthentication.HttpDigest);

            Assert.AreEqual(DigestAuthentication.HttpDigest, options.Onvif.Authentication);
        }

        [TestMethod]
        public void ReadsTheOptionsOnceAndKeepsWhatItRead()
        {
            // Half of these used to reach the client by reference and half by value, so changing
            // them afterwards changed the schemes it offered but not the clock offset it stamped.
            // Neither does anything now: they are the options it was built with, not a channel.
            var options = new ClientOptions(DigestAuthentication.WsUsernameToken)
            {
                UtcNowOffset = TimeSpan.FromMinutes(1),
            };

            using var client = new Probe("http://127.0.0.1:1/onvif/device_service", "u", "p", options);

            options.Onvif.Authentication = DigestAuthentication.None;
            options.Onvif.HttpDigestAlgorithms.Clear();
            options.UtcNowOffset = TimeSpan.FromHours(9);

            Assert.AreEqual(DigestAuthentication.WsUsernameToken, client.Options.Onvif.Authentication);
            Assert.AreNotEqual(0, client.Options.Onvif.HttpDigestAlgorithms.Count);
            Assert.AreEqual(TimeSpan.FromMinutes(1), client.Options.UtcNowOffset);
        }

        /// <summary>Reaches the options the client kept, which are protected rather than public.</summary>
        private sealed class Probe : SimpleOnvifClient
        {
            public Probe(string uri, string userName, string password, ClientOptions authentication)
                : base(uri, userName, password, authentication)
            {
            }

            public ClientOptions Options { get { return _authentication; } }
        }

        [TestMethod]
        public async Task SendsTheOffsetItWasConfiguredWith()
        {
            // The offset has to travel from these options into the token the client stamps, which
            // is the only thing that says the plumbing behind the new shape is connected. The
            // device's window is narrow and the offset is far outside it, so a token carrying it
            // is refused - and one would not be sent at all if the offset were being dropped.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
            {
                options.Onvif.Authentication = DigestAuthentication.WsUsernameToken;
                options.WsUsernameTokenMaxTimeDeltaInMilliseconds = 2000;
            });

            using var client = new SimpleOnvifClient(
                device.Endpoint,
                AuthenticatedDevice.UserName,
                AuthenticatedDevice.Password,
                new ClientOptions(DigestAuthentication.WsUsernameToken)
                {
                    UtcNowOffset = TimeSpan.FromMinutes(-10),
                });

            await Assert.ThrowsExactlyAsync<SoapFaultException>(() => client.GetDeviceInformationAsync());
        }

        [TestMethod]
        public async Task AuthenticatesWithAnOffsetInsideTheDevicesWindow()
        {
            // The other direction, so the test above is failing for the offset rather than for
            // the client being broken.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
            {
                options.Onvif.Authentication = DigestAuthentication.WsUsernameToken;
                options.WsUsernameTokenMaxTimeDeltaInMilliseconds = 300000;
            });

            using var client = new SimpleOnvifClient(
                device.Endpoint,
                AuthenticatedDevice.UserName,
                AuthenticatedDevice.Password,
                new ClientOptions(DigestAuthentication.WsUsernameToken)
                {
                    UtcNowOffset = TimeSpan.FromMinutes(1),
                });

            var info = await client.GetDeviceInformationAsync();

            Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer);
        }
    }
}
