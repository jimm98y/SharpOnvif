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
using SharpOnvifCommon.Security;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The parts of HTTP Digest that decide whether a request is genuine: that a nonce cannot be
    /// used twice, that a parameter is read from the right place, and that a peer cannot choose an
    /// algorithm the other side did not offer.
    /// </summary>
    [TestClass]
    public sealed class TestHttpDigestHardening
    {
        private const string Algorithm = "MD5";
        private const int SaltLength = 12;

        private static string FreshNonce(DateTimeOffset when) =>
            HttpDigestAuthentication.GenerateServerNonce(
                Algorithm, BinarySerializationType.Hex, when, null,
                HttpDigestAuthentication.CreateNonceSessionSalt(SaltLength));

        private static int Validate(string nonce, int nc, DateTimeOffset when) =>
            HttpDigestAuthentication.ValidateServerNonce(
                Algorithm, BinarySerializationType.Hex, nonce, nc, when, null, SaltLength, 30000, true);

        [DataRow(1, DisplayName = "first request of a session")]
        [DataRow(5, DisplayName = "after the client retried a lost request")]
        [DataRow(42, DisplayName = "well into a session")]
        [TestMethod]
        public void RefusesARequestReplayedWithTheSameNonceCount(int nonceCount)
        {
            // Whatever count a request arrives with, sending that same request again has to be
            // refused. Recording the first use as 1 rather than the count presented left every
            // count below it acceptable, so a captured request could be replayed once.
            var now = DateTimeOffset.UtcNow;
            string nonce = FreshNonce(now);

            Assert.AreEqual(0, Validate(nonce, nonceCount, now), "the first request has to be accepted");
            Assert.AreEqual(HttpDigestAuthentication.ERROR_NONCE_REUSE, Validate(nonce, nonceCount, now),
                "the identical request has to be refused the second time");
        }

        [TestMethod]
        public void RefusesACountThatDoesNotAdvance()
        {
            var now = DateTimeOffset.UtcNow;
            string nonce = FreshNonce(now);

            Assert.AreEqual(0, Validate(nonce, 7, now));
            Assert.AreEqual(HttpDigestAuthentication.ERROR_NONCE_REUSE, Validate(nonce, 3, now),
                "a count below the one already seen is a replay");
            Assert.AreEqual(0, Validate(nonce, 8, now), "the session continues from where it was");
        }

        [DataRow("Digest realm=\"r\", cnonce=\"CLIENT\", nonce=\"SERVER\"", DisplayName = "cnonce first")]
        [DataRow("Digest realm=\"r\", nonce=\"SERVER\", cnonce=\"CLIENT\"", DisplayName = "nonce first")]
        [TestMethod]
        public void ReadsTheServerNonceWhicheverOrderTheHeaderUses(string header)
        {
            // Field order is not constrained, and "nonce" appears inside "cnonce". Reading the
            // client's nonce where the server's belongs makes every such request fail to
            // authenticate.
            Assert.AreEqual("SERVER", HttpDigestAuthentication.GetValueFromHeader(header, "nonce", true));
            Assert.AreEqual("CLIENT", HttpDigestAuthentication.GetValueFromHeader(header, "cnonce", true));
        }

        [TestMethod]
        public void ReadsANonceThatIsNotConfusedWithANextNonce()
        {
            const string info = "nextnonce=\"NEXT\", qop=auth, cnonce=\"CLIENT\", nc=00000001";

            Assert.AreEqual("NEXT", HttpDigestAuthentication.GetValueFromHeader(info, "nextnonce", true));
            Assert.IsNull(HttpDigestAuthentication.GetValueFromHeader(info, "nonce", true),
                "there is no nonce parameter here, only a nextnonce");
        }

        [TestMethod]
        public void RefusesToComputeADigestForAnAlgorithmItDoesNotImplement()
        {
            // An unrecognised name used to fall through to MD5, so a peer could name anything and
            // get the weakest algorithm available without either side noticing.
            Assert.ThrowsExactly<NotSupportedException>(() => HttpDigestAuthentication.CreateWebDigestRFC7616(
                "NOT-AN-ALGORITHM", "u", "r", "p", false, "n", "GET", "/", 1, "c", "auth"));

            Assert.IsFalse(HttpDigestAuthentication.IsSupportedAlgorithm("NOT-AN-ALGORITHM"));
            Assert.IsFalse(HttpDigestAuthentication.IsSupportedAlgorithm("SHA-1"));
        }

        [DataRow("", DisplayName = "absent means MD5")]
        [DataRow("MD5")]
        [DataRow("MD5-sess")]
        [DataRow("SHA-256")]
        [DataRow("SHA-512-256")]
        [TestMethod]
        public void ComputesADigestForEveryAlgorithmItAdvertises(string algorithm)
        {
            string digest = HttpDigestAuthentication.CreateWebDigestRFC7616(
                algorithm, "u", "r", "p", false, "n", "GET", "/", 1, "c", "auth", null, "n", "c");

            Assert.IsFalse(string.IsNullOrEmpty(digest));
            Assert.IsTrue(HttpDigestAuthentication.IsSupportedAlgorithm(algorithm));
        }

        [TestMethod]
        public void ComparesDigestsWithoutRevealingHowFarTheyMatched()
        {
            Assert.IsTrue(HttpDigestAuthentication.FixedTimeEquals("abc123", "abc123"));
            Assert.IsFalse(HttpDigestAuthentication.FixedTimeEquals("abc123", "abc124"));
            Assert.IsFalse(HttpDigestAuthentication.FixedTimeEquals("abc123", "xbc123"));
            Assert.IsFalse(HttpDigestAuthentication.FixedTimeEquals("abc", "abc123"));
            Assert.IsFalse(HttpDigestAuthentication.FixedTimeEquals(null, "abc"));
            Assert.IsTrue(HttpDigestAuthentication.FixedTimeEquals(null, null));
        }
    }
}
