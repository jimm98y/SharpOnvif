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
using SharpOnvifClient.DeviceMgmt;
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Soap;
using SharpOnvifCommon.Xml;

using ClientAuth = SharpOnvifCommon.Security.DigestAuthentication;
using DeviceAuth = SharpOnvifServer.Security.DigestAuthentication;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// HTTP Digest, in every variant the library says it supports: each hashing algorithm, each
    /// quality of protection, with and without username hashing.
    /// <para>
    /// A client and a server that each implement the specification correctly can still fail to
    /// authenticate each other, so each combination is exercised over a socket rather than
    /// compared against a vector. The vectors are checked separately, in the RFC tests.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestHttpDigestVariants
    {
        /// <summary>Everything the device offers and the client says it understands.</summary>
        public static IEnumerable<object[]> EveryVariant()
        {
            foreach (string algorithm in new[]
            {
                "MD5", "MD5-sess", "SHA-256", "SHA-256-sess", "SHA-512-256", "SHA-512-256-sess",
            })
            {
                foreach (string qop in new[] { "auth", "auth-int" })
                {
                    yield return new object[] { algorithm, qop };
                }
            }
        }

        private static DeviceClient Client(
            AuthenticatedDevice device,
            string password = AuthenticatedDevice.Password,
            bool userHash = true)
        {
            return new DeviceClient(device.Endpoint, new OnvifClientSettings
            {
                Credentials = new System.Net.NetworkCredential(AuthenticatedDevice.UserName, password),
                Authentication = new OnvifAuthenticationSettings(ClientAuth.HttpDigest)
                {
                    HttpDigestUserHash = userHash,
                },
            });
        }

        /// <summary>A device that offers exactly one algorithm and one quality of protection.</summary>
        private static Task<AuthenticatedDevice> DeviceOffering(
            string algorithm, string qop, bool userHash = true, double nonceLifetimeMs = 30000)
        {
            return AuthenticatedDevice.StartAsync(options =>
            {
                options.Authentication = DeviceAuth.HttpDigest;
                options.HttpDigestAlgorithms = new List<string> { algorithm };
                options.HttpDigestQop = new List<string> { qop };
                options.HttpDigestUserHash = userHash;
                options.HttpDigestNonceLifetimeMilliseconds = nonceLifetimeMs;
            });
        }

        /// <summary>
        /// What the device challenges an unauthenticated request with. Read directly, so that a
        /// test claiming to exercise SHA-512-256 can prove the device really asked for it - a
        /// device that quietly used MD5 throughout would pass every row otherwise.
        /// </summary>
        private static async Task<string> ChallengeFor(AuthenticatedDevice device)
        {
            using var http = new System.Net.Http.HttpClient();
            var content = new System.Net.Http.StringContent(
                "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                "<s:Body><GetDeviceInformation xmlns=\"http://www.onvif.org/ver10/device/wsdl\"/></s:Body>" +
                "</s:Envelope>");
            content.Headers.ContentType =
                System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/soap+xml; charset=utf-8");

            var response = await http.PostAsync(device.Endpoint, content);

            Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode,
                "an unauthenticated request has to be challenged");

            return string.Join(" ", response.Headers.WwwAuthenticate);
        }

        /// <summary>
        /// Checks the challenge names exactly this variant.
        /// </summary>
        /// <remarks>
        /// The algorithm is compared as a whole token, not as a substring: "algorithm=MD5" is a
        /// prefix of "algorithm=MD5-sess", so a loose check would let one variant pass for
        /// another. RFC 7616 makes MD5 the default, and this device omits the parameter for it
        /// rather than spelling it out, which is what an older client expects to see.
        /// </remarks>
        private static void AssertChallenged(string challenge, string algorithm, string qop)
        {
            StringAssert.Contains(challenge, "qop=\"" + qop + "\"",
                $"the device did not challenge with qop={qop}");

            var named = System.Text.RegularExpressions.Regex.Match(challenge, @"algorithm=([^,\s]+)");

            if (algorithm == "MD5")
            {
                Assert.IsFalse(named.Success,
                    $"MD5 is the default and is left unsaid, but the challenge named '{named.Groups[1].Value}'");
                return;
            }

            Assert.IsTrue(named.Success, $"the device did not challenge with {algorithm}");
            Assert.AreEqual(algorithm, named.Groups[1].Value,
                $"the device challenged with {named.Groups[1].Value} rather than {algorithm}");
        }

        [DynamicData(nameof(EveryVariant), DynamicDataSourceType.Method)]
        [TestMethod]
        public async Task AuthenticatesWithEveryAlgorithmAndQop(string algorithm, string qop)
        {
            await using var device = await DeviceOffering(algorithm, qop);

            // The device really is asking for this variant, not quietly falling back to another.
            AssertChallenged(await ChallengeFor(device), algorithm, qop);

            using var client = Client(device);

            var info = await client.GetDeviceInformationAsync();

            Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer,
                $"{algorithm} with qop={qop} did not authenticate");
        }

        [DynamicData(nameof(EveryVariant), DynamicDataSourceType.Method)]
        [TestMethod]
        public async Task RefusesTheWrongPasswordWithEveryAlgorithmAndQop(string algorithm, string qop)
        {
            // The other half of the same arithmetic: a variant that accepts everybody is no more
            // use than one that accepts nobody.
            await using var device = await DeviceOffering(algorithm, qop);
            using var client = Client(device, password: "not the password");

            await Assert.ThrowsExactlyAsync<OnvifFaultException>(() => client.GetDeviceInformationAsync(),
                $"{algorithm} with qop={qop} accepted the wrong password");
        }

        [DynamicData(nameof(EveryVariant), DynamicDataSourceType.Method)]
        [TestMethod]
        public async Task KeepsAuthenticatingAcrossCallsWithEveryAlgorithmAndQop(string algorithm, string qop)
        {
            // The second call reuses the nonce with a higher count, which is the part the
            // -sess algorithms and the replay protection both have opinions about.
            await using var device = await DeviceOffering(algorithm, qop);
            using var client = Client(device);

            for (int call = 0; call < 3; call++)
            {
                var info = await client.GetDeviceInformationAsync();
                Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer,
                    $"{algorithm} with qop={qop} failed on call {call}");
            }
        }

        [DataRow(true, DisplayName = "usernames hashed")]
        [DataRow(false, DisplayName = "usernames in the clear")]
        [TestMethod]
        public async Task AuthenticatesWhicheverWayTheUsernameIsSent(bool userHash)
        {
            // With userhash the client sends H(username:realm) instead of the name, so the device
            // has to find its user by that hash - a different lookup, not just a different string.
            await using var device = await DeviceOffering("SHA-256", "auth", userHash);
            using var client = Client(device, userHash: userHash);

            var info = await client.GetDeviceInformationAsync();

            Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer);
        }

        [TestMethod]
        public async Task AuthenticatesWhenTheClientWillNotHashItsUsernameButTheDeviceOffersIt()
        {
            // userhash is an offer, not a demand: a client that ignores it sends its name plainly
            // and must still get in.
            await using var device = await DeviceOffering("SHA-256", "auth", userHash: true);
            using var client = Client(device, userHash: false);

            var info = await client.GetDeviceInformationAsync();

            Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer);
        }

        [TestMethod]
        public async Task AuthenticatesAgainAfterTheNonceHasExpired()
        {
            // A nonce has a lifetime. When it runs out the device challenges again and the client
            // starts over, which has to be invisible to the caller.
            await using var device = await DeviceOffering("MD5", "auth", nonceLifetimeMs: 900);
            using var client = Client(device);

            Assert.AreEqual(AuthenticatedDevice.Manufacturer,
                (await client.GetDeviceInformationAsync()).Manufacturer);

            await Task.Delay(TimeSpan.FromSeconds(2));

            Assert.AreEqual(AuthenticatedDevice.Manufacturer,
                (await client.GetDeviceInformationAsync()).Manufacturer,
                "the client did not recover from an expired nonce");
        }

        [TestMethod]
        public async Task LetsTheClientChooseFromWhatTheDeviceOffers()
        {
            // What a real exchange looks like: the device lists what it will take, in its order of
            // preference, and the client picks the first it knows.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
            {
                options.Authentication = DeviceAuth.HttpDigest;
                options.HttpDigestAlgorithms = new List<string>
                {
                    "MD5", "MD5-sess", "SHA-256", "SHA-256-sess", "SHA-512-256", "SHA-512-256-sess",
                };
                options.HttpDigestQop = new List<string> { "auth", "auth-int" };
            });

            using var client = Client(device);

            var info = await client.GetDeviceInformationAsync();

            Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer);
        }

        [TestMethod]
        public async Task RefusesADigestWhenTheDeviceOnlyTakesTheOlderScheme()
        {
            await using var device = await AuthenticatedDevice.StartAsync(options =>
                options.Authentication = DeviceAuth.WsUsernameToken);

            using var client = Client(device);

            await Assert.ThrowsExactlyAsync<OnvifFaultException>(() => client.GetDeviceInformationAsync());
        }
    }
}
