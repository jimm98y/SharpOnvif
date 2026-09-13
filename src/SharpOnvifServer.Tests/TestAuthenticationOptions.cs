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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharpOnvifServer;
using SharpOnvifCommon.Security;
using SharpOnvifServer.Security;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The settings a device and a client have to agree on are described by one type, carried by
    /// both. What a device offers and what a client understands are the same vocabulary, and
    /// keeping two copies of it is how they drift - which is what happened to the
    /// `DigestAuthentication` enum before it was collapsed into one.
    /// </summary>
    [TestClass]
    public sealed class TestAuthenticationOptions
    {
        [TestMethod]
        public void TakesTheSettingsAClientWouldBeGiven()
        {
            // The point of sharing the type: one description of a negotiation, handed to either
            // side of it.
            var agreed = new OnvifAuthenticationOptions(DigestAuthentication.HttpDigest)
            {
                HttpDigestAlgorithms = new List<string> { "SHA-512-256" },
                HttpDigestQop = new List<string> { "auth-int" },
                HttpDigestUserHash = false,
            };

            var options = new DigestAuthenticationSchemeOptions { Onvif = agreed };

            Assert.AreEqual(DigestAuthentication.HttpDigest, options.Onvif.Authentication);
            CollectionAssert.AreEqual(new[] { "SHA-512-256" }, options.Onvif.HttpDigestAlgorithms);
            CollectionAssert.AreEqual(new[] { "auth-int" }, options.Onvif.HttpDigestQop);
            Assert.IsFalse(options.Onvif.HttpDigestUserHash);
        }

        [TestMethod]
        public void KeepsTheDevicesOwnSettingsToItself()
        {
            // The realm, the nonce lifetime and the rest are the device's business; the offset is
            // the client's. Sharing the negotiation does not mean sharing everything.
            var options = new DigestAuthenticationSchemeOptions
            {
                HttpDigestRealm = "Front door",
                HttpDigestNonceLifetimeMilliseconds = 5000,
                WsUsernameTokenMaxTimeDeltaInMilliseconds = 1000,
            };

            Assert.AreEqual("Front door", options.HttpDigestRealm);
            Assert.AreEqual(5000, options.HttpDigestNonceLifetimeMilliseconds);
            Assert.AreEqual(1000, options.WsUsernameTokenMaxTimeDeltaInMilliseconds);
        }

        [TestMethod]
        public void SurvivesBeingGivenNoSharedSettings()
        {
            var options = new DigestAuthenticationSchemeOptions { Onvif = null };

            Assert.IsNotNull(options.Onvif, "the handler would throw on the next read");
            Assert.IsNotNull(options.Onvif.HttpDigestAlgorithms);
        }

        [TestMethod]
        public void DefaultsTheSameWayOnBothSides()
        {
            // They are one object now, so this is a guard against somebody giving the device its
            // own defaults again: a device that offers what its own client does not ask for.
            var device = new DigestAuthenticationSchemeOptions();
            var client = new OnvifAuthenticationOptions();

            Assert.AreEqual(client.Authentication, device.Onvif.Authentication);
            Assert.AreEqual(client.HttpDigestUserHash, device.Onvif.HttpDigestUserHash);
            CollectionAssert.AreEqual(client.HttpDigestAlgorithms, device.Onvif.HttpDigestAlgorithms);
            CollectionAssert.AreEqual(client.HttpDigestQop, device.Onvif.HttpDigestQop);
            CollectionAssert.AreEqual(client.PreAuthActions, device.Onvif.PreAuthActions);
        }

        [TestMethod]
        public void BindsTheDeviceAndWhatItNegotiatesFromOneSection()
        {
            // Two objects in code, one element in the file. Which of the two a setting belongs to
            // is the library's business, not the reader's.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Digest:HttpDigestRealm"] = "My IP Camera",
                    ["Digest:HttpDigestNonceLifetimeMilliseconds"] = "12345",
                    ["Digest:WsUsernameTokenMaxTimeDeltaInMilliseconds"] = "1000",
                    ["Digest:Authentication"] = "2",
                    ["Digest:HttpDigestUserHash"] = "false",
                    ["Digest:HttpDigestAlgorithms:0"] = "SHA-256",
                    ["Digest:HttpDigestQop:0"] = "auth",
                    ["Digest:PreAuthActions:0"] = "http://www.onvif.org/ver10/device/wsdl/GetServices",
                })
                .Build();

            DigestAuthenticationSchemeOptions options = Configured(configuration.GetSection("Digest"));

            // The device's own.
            Assert.AreEqual("My IP Camera", options.HttpDigestRealm);
            Assert.AreEqual(12345, options.HttpDigestNonceLifetimeMilliseconds);
            Assert.AreEqual(1000, options.WsUsernameTokenMaxTimeDeltaInMilliseconds);

            // And what it negotiates, off the same element.
            Assert.AreEqual(DigestAuthentication.HttpDigest, options.Onvif.Authentication);
            Assert.IsFalse(options.Onvif.HttpDigestUserHash);
            CollectionAssert.AreEqual(new[] { "SHA-256" }, options.Onvif.HttpDigestAlgorithms);
            CollectionAssert.AreEqual(new[] { "auth" }, options.Onvif.HttpDigestQop);
            CollectionAssert.AreEqual(
                new[] { "http://www.onvif.org/ver10/device/wsdl/GetServices" }, options.Onvif.PreAuthActions);
        }

        [TestMethod]
        public void DoesNotAdvertiseADefaultTwiceWhenTheFileRestatesIt()
        {
            // Binding a list onto a default appends to it. A file that spells out the defaults -
            // as the sample's does - would otherwise make the device offer each algorithm twice.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Digest:HttpDigestAlgorithms:0"] = "MD5",
                    ["Digest:HttpDigestAlgorithms:1"] = "SHA-256",
                    ["Digest:HttpDigestQop:0"] = "auth",
                })
                .Build();

            DigestAuthenticationSchemeOptions options = Configured(configuration.GetSection("Digest"));

            CollectionAssert.AllItemsAreUnique(options.Onvif.HttpDigestAlgorithms);
            CollectionAssert.AllItemsAreUnique(options.Onvif.HttpDigestQop);
            CollectionAssert.Contains(options.Onvif.HttpDigestAlgorithms, "SHA-256");
        }

        [TestMethod]
        public void StartsOnDefaultsWhenThereIsNoConfiguration()
        {
            DigestAuthenticationSchemeOptions options = Configured(null);

            Assert.AreEqual("IP Camera", options.HttpDigestRealm);
            CollectionAssert.AreEqual(
                new OnvifAuthenticationOptions().HttpDigestAlgorithms, options.Onvif.HttpDigestAlgorithms);
        }

        /// <summary>The options a device ends up with, as the registration builds them.</summary>
        private static DigestAuthenticationSchemeOptions Configured(IConfiguration section)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOnvifDigestAuthentication(section);

            return services.BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<DigestAuthenticationSchemeOptions>>()
                .Get(OnvifAuthenticationDefaults.AuthenticationScheme);
        }

        [TestMethod]
        public void CopiesTheSettingsRatherThanSharingThem()
        {
            var original = new OnvifAuthenticationOptions(DigestAuthentication.HttpDigest)
            {
                HttpDigestAlgorithms = new List<string> { "MD5" },
            };

            var copy = new OnvifAuthenticationOptions(original);
            copy.HttpDigestAlgorithms.Add("SHA-256");
            copy.Authentication = DigestAuthentication.None;

            CollectionAssert.AreEqual(new[] { "MD5" }, original.HttpDigestAlgorithms,
                "the copy shares the original's list");
            Assert.AreEqual(DigestAuthentication.HttpDigest, original.Authentication);
        }
    }
}
