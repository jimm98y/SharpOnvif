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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using SharpOnvifCommon.Security;
using SharpOnvifCommon.Soap;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// How the client answers an HTTP Digest challenge.
    /// <para>
    /// The first call to a device goes out unauthenticated and is expected to be refused; the
    /// challenge that comes back is cached so later calls carry credentials straight away.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestHttpDigestHandler
    {
        private const string Realm = "IP Camera(FN636)";
        private const string Challenge = "realm=\"" + Realm + "\", qop=\"auth, auth-int\", nonce=\"abc123\", opaque=\"00000000\", stale=FALSE";

        private static HttpClient CreateClient(FakeTransport transport, out HttpDigestHandler handler)
        {
            var credentials = new NetworkCredential("admin", "password");
            handler = new HttpDigestHandler(credentials, new OnvifAuthenticationSettings(), transport);
            return new HttpClient(handler);
        }

        private static Task<HttpResponseMessage> PostAsync(HttpClient client)
        {
            return client.PostAsync("http://192.168.1.10/onvif/device_service", new StringContent("<body/>"));
        }

        [TestMethod]
        public async Task RetriesOnceWithCredentialsAfterAChallenge()
        {
            var transport = new FakeTransport((request, call) =>
                call == 1 ? FakeTransport.Challenge(Challenge) : FakeTransport.Ok());

            var client = CreateClient(transport, out _);
            var response = await PostAsync(client);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(2, transport.Requests.Count, "the refused request has to be sent again");
            Assert.IsNull(transport.Authorizations[0], "the first request cannot know the challenge yet");

            string authorization = transport.Authorizations[1];
            StringAssert.StartsWith(authorization, "Digest ");
            StringAssert.Contains(authorization, "username=\"admin\"");
            StringAssert.Contains(authorization, "realm=\"" + Realm + "\"");
            StringAssert.Contains(authorization, "nonce=\"abc123\"");
            StringAssert.Contains(authorization, "uri=\"/onvif/device_service\"");
            StringAssert.Contains(authorization, "nc=00000001");
        }

        [TestMethod]
        public async Task ReusesTheChallengeForLaterCalls()
        {
            var transport = new FakeTransport((request, call) =>
                call == 1 ? FakeTransport.Challenge(Challenge) : FakeTransport.Ok());

            var client = CreateClient(transport, out _);
            await PostAsync(client);
            await PostAsync(client);
            await PostAsync(client);

            // One challenge, one retry, then two calls that were authenticated from the start.
            Assert.AreEqual(4, transport.Requests.Count);
            Assert.IsNotNull(transport.Authorizations[2], "a later call must not need a second challenge");
            Assert.IsNotNull(transport.Authorizations[3]);

            // The nonce count has to advance, because a device rejects a replayed one.
            StringAssert.Contains(transport.Authorizations[1], "nc=00000001");
            StringAssert.Contains(transport.Authorizations[2], "nc=00000002");
            StringAssert.Contains(transport.Authorizations[3], "nc=00000003");
        }

        [TestMethod]
        public async Task DoesNotLoopWhenTheCredentialsAreWrong()
        {
            var transport = new FakeTransport((request, call) => FakeTransport.Challenge(Challenge));

            var client = CreateClient(transport, out _);
            var response = await PostAsync(client);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.AreEqual(2, transport.Requests.Count, "one retry, then the refusal is reported");
        }

        [TestMethod]
        public async Task SendsTheBodyAgainOnTheRetry()
        {
            var transport = new FakeTransport((request, call) =>
                call == 1 ? FakeTransport.Challenge(Challenge) : FakeTransport.Ok());

            var client = CreateClient(transport, out _);
            await PostAsync(client);

            string retried = await transport.Requests[1].Content.ReadAsStringAsync();
            Assert.AreEqual("<body/>", retried, "the retry has to carry the original body");
        }

        [TestMethod]
        public async Task IgnoresAChallengeWhoseAlgorithmIsNotSupported()
        {
            var transport = new FakeTransport((request, call) =>
                FakeTransport.Challenge("realm=\"r\", qop=\"auth\", algorithm=SHA-1, nonce=\"n\""));

            var credentials = new NetworkCredential("admin", "password");
            var settings = new OnvifAuthenticationSettings();
            settings.HttpDigestAlgorithms.Clear();
            settings.HttpDigestAlgorithms.Add("MD5");

            var client = new HttpClient(new HttpDigestHandler(credentials, settings, transport));
            var response = await PostAsync(client);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.AreEqual(1, transport.Requests.Count, "there is nothing to retry with");
        }

        [TestMethod]
        public async Task IgnoresAChallengeWithoutQop()
        {
            // The Onvif core specification does not permit the qop-less RFC 2069 form.
            var transport = new FakeTransport((request, call) =>
                FakeTransport.Challenge("realm=\"r\", nonce=\"n\""));

            var client = CreateClient(transport, out _);
            var response = await PostAsync(client);

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.AreEqual(1, transport.Requests.Count);
        }

        [TestMethod]
        public async Task StartsOverWhenTheDeviceIssuesANewNonce()
        {
            const string Second = "realm=\"" + Realm + "\", qop=\"auth\", nonce=\"def456\", stale=TRUE";

            var transport = new FakeTransport((request, call) =>
            {
                if (call == 1) return FakeTransport.Challenge(Challenge);
                if (call == 3) return FakeTransport.Challenge(Second);
                return FakeTransport.Ok();
            });

            var client = CreateClient(transport, out _);
            await PostAsync(client);
            await PostAsync(client);

            // The new nonce restarts the count, because it is the device's counter, not ours.
            StringAssert.Contains(transport.Authorizations[3], "nonce=\"def456\"");
            StringAssert.Contains(transport.Authorizations[3], "nc=00000001");
        }

        [TestMethod]
        public async Task RestartsTheCountWhenTheDeviceHandsOutANextNonce()
        {
            // A nonce count states how many requests have been sent with that nonce, so a nonce
            // the client has not used before starts at one - whether it arrived in a challenge or
            // in a nextnonce. Carrying the previous count over describes something untrue, and a
            // device that checks it rejects the request.
            var transport = new FakeTransport((request, call) =>
            {
                if (call == 1) return FakeTransport.Challenge(Challenge);

                var response = FakeTransport.Ok();
                if (call == 2) response.Headers.TryAddWithoutValidation("Authentication-Info", "nextnonce=\"second\"");
                return response;
            });

            var client = CreateClient(transport, out _);
            await PostAsync(client);   // challenge, then nc=1 on the first nonce
            await PostAsync(client);   // first request on the nonce the device handed out
            await PostAsync(client);   // second request on it

            StringAssert.Contains(transport.Authorizations[1], "nonce=\"abc123\"");
            StringAssert.Contains(transport.Authorizations[1], "nc=00000001");

            StringAssert.Contains(transport.Authorizations[2], "nonce=\"second\"");
            StringAssert.Contains(transport.Authorizations[2], "nc=00000001", "a new nonce starts its own count");

            StringAssert.Contains(transport.Authorizations[3], "nonce=\"second\"");
            StringAssert.Contains(transport.Authorizations[3], "nc=00000002");
        }

        [TestMethod]
        public async Task UsesTheNextNonceTheDeviceOffers()
        {
            var transport = new FakeTransport((request, call) =>
            {
                if (call == 1) return FakeTransport.Challenge(Challenge);

                var response = FakeTransport.Ok();
                response.Headers.TryAddWithoutValidation("Authentication-Info", "nextnonce=\"xyz789\"");
                return response;
            });

            var client = CreateClient(transport, out _);
            await PostAsync(client);
            await PostAsync(client);

            StringAssert.Contains(transport.Authorizations[1], "nonce=\"abc123\"");
            StringAssert.Contains(transport.Authorizations[2], "nonce=\"xyz789\"",
                "a device that hands out a nextnonce expects the next request to use it");
        }
    }
}
