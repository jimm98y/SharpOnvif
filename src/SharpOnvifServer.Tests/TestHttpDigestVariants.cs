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
                Authentication = new OnvifAuthenticationSettings(
                    new OnvifAuthenticationOptions(DigestAuthentication.HttpDigest)
                    {
                        HttpDigestUserHash = userHash,
                    }),
            });
        }

        /// <summary>A device that offers exactly one algorithm and one quality of protection.</summary>
        private static Task<AuthenticatedDevice> DeviceOffering(
            string algorithm, string qop, bool userHash = true, double nonceLifetimeMs = 30000)
        {
            return AuthenticatedDevice.StartAsync(options =>
            {
                options.Onvif.Authentication = DigestAuthentication.HttpDigest;
                options.Onvif.HttpDigestAlgorithms = new List<string> { algorithm };
                options.Onvif.HttpDigestQop = new List<string> { qop };
                options.Onvif.HttpDigestUserHash = userHash;
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

            await Assert.ThrowsExactlyAsync<SoapFaultException>(() => client.GetDeviceInformationAsync(),
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

        /// <summary>Sends its content in two halves, with a pause between them.</summary>
        /// <remarks>
        /// A body that arrives all at once is read all at once whatever the reading code does, so
        /// a request that arrives in pieces is the only way to tell the two apart.
        /// </remarks>
        private sealed class HalfAtATime : System.Net.Http.HttpContent
        {
            private readonly byte[] _body;

            public HalfAtATime(byte[] body)
            {
                _body = body;
                Headers.ContentType =
                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/soap+xml; charset=utf-8");
            }

            protected override async Task SerializeToStreamAsync(
                System.IO.Stream stream, System.Net.TransportContext context)
            {
                int half = _body.Length / 2;

                await stream.WriteAsync(_body, 0, half);
                await stream.FlushAsync();
                await Task.Delay(250);
                await stream.WriteAsync(_body, half, _body.Length - half);
                await stream.FlushAsync();
            }

            /// <summary>
            /// Unknown, so the request is chunked and the halves go out as they are written
            /// rather than being buffered whole first.
            /// </summary>
            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }
        }

        [TestMethod]
        public async Task AuthenticatesARequestThatArrivesInPieces()
        {
            // auth-int digests the body, so the device has to read all of it before it can check
            // anything. Reading only what the first read happens to bring digests a prefix, which
            // refuses an honest request for a reason nothing in it explains - and only a sender
            // that pauses mid-body shows the difference.
            await using var device = await DeviceOffering("SHA-256", "auth-int", userHash: false);

            string challenge = await ChallengeFor(device);
            string nonce = Value(challenge, "nonce");
            string realm = Value(challenge, "realm");
            string opaque = Value(challenge, "opaque");
            string path = new Uri(device.Endpoint).AbsolutePath;
            const string CNonce = "0a4f113b";

            byte[] body = System.Text.Encoding.UTF8.GetBytes(
                "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                "<!--" + new string('x', 128 * 1024) + "-->" +
                "<s:Body><GetDeviceInformation xmlns=\"http://www.onvif.org/ver10/device/wsdl\"/></s:Body>" +
                "</s:Envelope>");

            string response = SharpOnvifCommon.Security.HttpDigestAuthentication.CreateWebDigestRFC7616(
                "SHA-256", AuthenticatedDevice.UserName, realm, AuthenticatedDevice.Password, false,
                nonce, "POST", path, 1, CNonce, "auth-int", body);

            string authorization = SharpOnvifCommon.Security.HttpDigestAuthentication.CreateAuthorizationRFC7616(
                AuthenticatedDevice.UserName, realm, nonce, path, response, opaque,
                "SHA-256", "auth-int", 1, CNonce, userhash: false);

            using var http = new System.Net.Http.HttpClient();
            var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, device.Endpoint)
            {
                Content = new HalfAtATime(body),
            };
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

            using var reply = await http.SendAsync(request);

            Assert.AreEqual(System.Net.HttpStatusCode.OK, reply.StatusCode,
                "the device digested what it had read so far rather than the request it was sent");
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
                options.Onvif.Authentication = DigestAuthentication.HttpDigest;
                options.Onvif.HttpDigestAlgorithms = new List<string>
                {
                    "MD5", "MD5-sess", "SHA-256", "SHA-256-sess", "SHA-512-256", "SHA-512-256-sess",
                };
                options.Onvif.HttpDigestQop = new List<string> { "auth", "auth-int" };
            });

            using var client = Client(device);

            var info = await client.GetDeviceInformationAsync();

            Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer);
        }

        [TestMethod]
        public async Task AuthenticatesWhenTheDeviceTookTheOlderSchemeAway()
        {
            // What a client is by default: it knows both schemes and lets the device choose. A
            // device that has switched WS-UsernameToken off still has to let it in on digest -
            // and the token it carries anyway, for a scheme the device no longer takes, must not
            // be what gets it refused.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
                options.Onvif.Authentication = DigestAuthentication.HttpDigest);

            using var client = new DeviceClient(device.Endpoint, new OnvifClientSettings
            {
                Credentials = new System.Net.NetworkCredential(
                    AuthenticatedDevice.UserName, AuthenticatedDevice.Password),
            });

            var info = await client.GetDeviceInformationAsync();

            Assert.AreEqual(AuthenticatedDevice.Manufacturer, info.Manufacturer);
        }

        [TestMethod]
        public async Task StillChecksTheTokenWhenTheDeviceTakesBoth()
        {
            // The other side of ignoring a token nobody asked for: where the device does take the
            // older scheme, credentials presented both ways both have to hold up. Here the digest
            // is good and the token is stale, and that is still a refusal.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
            {
                options.Onvif.Authentication =
                    DigestAuthentication.HttpDigest | DigestAuthentication.WsUsernameToken;
                options.WsUsernameTokenMaxTimeDeltaInMilliseconds = 2000;
            });

            using var client = new DeviceClient(device.Endpoint, new OnvifClientSettings
            {
                Credentials = new System.Net.NetworkCredential(
                    AuthenticatedDevice.UserName, AuthenticatedDevice.Password),
                UtcNowOffset = TimeSpan.FromMinutes(-10),
            });

            await Assert.ThrowsExactlyAsync<SoapFaultException>(() => client.GetDeviceInformationAsync(),
                "a token that does not hold up must not be waved through because the digest did");
        }

        /// <summary>
        /// A request carrying credentials both ways: a digest of the right shape but the wrong
        /// answer, and a UsernameToken that is genuinely valid.
        /// </summary>
        /// <remarks>
        /// Built by hand because a real client never gets into this state on purpose - on a device
        /// that takes both, its first request authenticates on the token alone and it is never
        /// challenged. That is exactly why the case needs pinning: nothing else reaches it.
        /// </remarks>
        private static async Task<System.Net.HttpStatusCode> PostBothAsync(
            AuthenticatedDevice device, string digestResponse)
        {
            using var http = new System.Net.Http.HttpClient();

            string challenge = await ChallengeFor(device);
            string nonce = Value(challenge, "nonce");
            string realm = Value(challenge, "realm");
            string opaque = Value(challenge, "opaque");

            // The token is written by the same code a real client writes it with, rather than
            // spelled out here: a hand-copied one would be this test's idea of the format instead
            // of the library's, and would go on passing after the real one drifted away from it.
            var content = new System.Net.Http.StringContent(
                SharpOnvifCommon.Xml.SoapEnvelope.Write(
                    SharpOnvifCommon.Xml.OnvifXmlNamespaces.EnvelopePrologue,
                    header => SharpOnvifCommon.Soap.WsUsernameToken.Write(
                        header, AuthenticatedDevice.UserName, AuthenticatedDevice.Password, TimeSpan.Zero),
                    body => body.WriteEmptyElement(
                        "http://www.onvif.org/ver10/device/wsdl", "GetDeviceInformation")));
            content.Headers.ContentType =
                System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/soap+xml; charset=utf-8");

            var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, device.Endpoint)
            {
                Content = content,
            };

            string path = new Uri(device.Endpoint).AbsolutePath;
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Digest",
                $"username=\"{AuthenticatedDevice.UserName}\", realm=\"{realm}\", nonce=\"{nonce}\", " +
                $"uri=\"{path}\", response=\"{digestResponse}\", qop=auth, nc=00000001, cnonce=\"0a4f113b\"" +
                (opaque == null ? "" : $", opaque=\"{opaque}\""));

            using var response = await http.SendAsync(request);
            return response.StatusCode;
        }

        private static string Value(string challenge, string name)
        {
            var match = System.Text.RegularExpressions.Regex.Match(challenge, name + "=\"([^\"]*)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        [TestMethod]
        public async Task RefusesAGoodTokenWhenTheDigestBesideItIsWrong()
        {
            // Onvif has the digest checked first, and a client presenting credentials both ways
            // has to be right both ways. A wrong digest used to fall past its own check and be
            // excused by the token further down the request, which made the digest optional for
            // anybody who could produce a token.
            await using var device = await AuthenticatedDevice.StartAsync(options =>
                options.Onvif.Authentication =
                    DigestAuthentication.HttpDigest | DigestAuthentication.WsUsernameToken);

            Assert.AreEqual(
                System.Net.HttpStatusCode.Unauthorized,
                await PostBothAsync(device, new string('0', 32)),
                "a digest that does not hold up must refuse the request, token or no token");
        }

        [TestMethod]
        public async Task RefusesADigestWhenTheDeviceOnlyTakesTheOlderScheme()
        {
            await using var device = await AuthenticatedDevice.StartAsync(options =>
                options.Onvif.Authentication = DigestAuthentication.WsUsernameToken);

            using var client = Client(device);

            await Assert.ThrowsExactlyAsync<SoapFaultException>(() => client.GetDeviceInformationAsync());
        }
    }
}
