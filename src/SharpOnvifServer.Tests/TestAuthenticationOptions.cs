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
            var agreed = new OnvifAuthenticationSettings(DigestAuthentication.HttpDigest)
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
            var client = new OnvifAuthenticationSettings();

            Assert.AreEqual(client.Authentication, device.Onvif.Authentication);
            Assert.AreEqual(client.HttpDigestUserHash, device.Onvif.HttpDigestUserHash);
            CollectionAssert.AreEqual(client.HttpDigestAlgorithms, device.Onvif.HttpDigestAlgorithms);
            CollectionAssert.AreEqual(client.HttpDigestQop, device.Onvif.HttpDigestQop);
            CollectionAssert.AreEqual(client.PreAuthActions, device.Onvif.PreAuthActions);
        }

        [TestMethod]
        public void BindsFromConfiguration()
        {
            // The shared settings are a section of their own now, and the device's own settings
            // stay where they were. A configuration written for 0.10.0's earlier shape - all of
            // them flat - no longer reaches the shared half, which is why this is a documented
            // break rather than a silent one.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Digest:HttpDigestRealm"] = "My IP Camera",
                    ["Digest:HttpDigestNonceLifetimeMilliseconds"] = "12345",
                    ["Digest:Onvif:Authentication"] = "2",
                    ["Digest:Onvif:HttpDigestUserHash"] = "false",
                    ["Digest:Onvif:HttpDigestAlgorithms:0"] = "SHA-256",
                    ["Digest:Onvif:HttpDigestQop:0"] = "auth",
                    ["Digest:Onvif:PreAuthActions:0"] = "http://www.onvif.org/ver10/device/wsdl/GetServices",
                })
                .Build();

            var options = configuration.GetSection("Digest").Get<DigestAuthenticationSchemeOptions>();

            Assert.AreEqual("My IP Camera", options.HttpDigestRealm);
            Assert.AreEqual(12345, options.HttpDigestNonceLifetimeMilliseconds);

            Assert.AreEqual(DigestAuthentication.HttpDigest, options.Onvif.Authentication);
            Assert.IsFalse(options.Onvif.HttpDigestUserHash);
            CollectionAssert.Contains(options.Onvif.HttpDigestAlgorithms, "SHA-256");
            CollectionAssert.Contains(options.Onvif.HttpDigestQop, "auth");
            CollectionAssert.Contains(options.Onvif.PreAuthActions,
                "http://www.onvif.org/ver10/device/wsdl/GetServices");
        }

        [TestMethod]
        public void CopiesTheSettingsRatherThanSharingThem()
        {
            var original = new OnvifAuthenticationSettings(DigestAuthentication.HttpDigest)
            {
                HttpDigestAlgorithms = new List<string> { "MD5" },
            };

            var copy = new OnvifAuthenticationSettings(original);
            copy.HttpDigestAlgorithms.Add("SHA-256");
            copy.Authentication = DigestAuthentication.None;

            CollectionAssert.AreEqual(new[] { "MD5" }, original.HttpDigestAlgorithms,
                "the copy shares the original's list");
            Assert.AreEqual(DigestAuthentication.HttpDigest, original.Authentication);
        }
    }
}
