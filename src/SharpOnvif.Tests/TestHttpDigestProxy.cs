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

using SharpOnvifClient.Security;
using System.Security.Authentication;
using System.ServiceModel.Security;
using System.Threading.Tasks;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// How HttpDigestProxy reacts to the MessageSecurityException WCF throws - it has to retry the call
    ///  once the challenge is known, and to stay out of the way when the failure was not a challenge.
    /// </summary>
    [TestClass]
    public sealed class TestHttpDigestProxy
    {
        private const string Challenge = "Digest realm=\"IP Camera\", qop=\"auth, auth-int\", nonce=\"abc\", opaque=\"o\", stale=FALSE";
        private const string StaleChallenge = "Digest realm=\"IP Camera\", qop=\"auth, auth-int\", nonce=\"def\", opaque=\"o\", stale=TRUE";
        private const string Forbidden = "The HTTP request was forbidden with client authentication scheme 'Anonymous'.";

        private static IFaultingChannel CreateProxy(string exceptionMessage, out FaultingChannel channel, out HttpDigestState state)
        {
            channel = new FaultingChannel(exceptionMessage);
            state = new HttpDigestState();
            return HttpDigestProxy<IFaultingChannel>.CreateProxy(channel, state);
        }

        [DataRow(false, DisplayName = "Task")]
        [DataRow(true, DisplayName = "Task<T>")]
        [TestMethod]
        public async Task TestRetryAfterChallenge(bool withResult)
        {
            string message = HttpDigestChallengeSource.CreateExceptionMessage(HttpDigestMessages.Cs, Challenge);
            var proxy = CreateProxy(message, out var channel, out var state);

            // the retry fails as well because the channel always faults
            await Assert.ThrowsExactlyAsync<MessageSecurityException>(
                () => withResult ? proxy.CallWithResultAsync() : proxy.CallAsync());

            Assert.AreEqual(2, channel.Calls, "the call has to be retried once the challenge is known");
            CollectionAssert.AreEqual(new[] { Challenge }, state.GetHeaders());
        }

        /// <summary>
        /// A 403 is reported as a MessageSecurityException too. There is nothing to retry, and the original
        ///  error has to reach the caller instead of being replaced by a parsing failure.
        /// </summary>
        [DataRow(false, DisplayName = "Task")]
        [DataRow(true, DisplayName = "Task<T>")]
        [TestMethod]
        public async Task TestRethrowWithoutChallenge(bool withResult)
        {
            var proxy = CreateProxy(Forbidden, out var channel, out var state);

            var exception = await Assert.ThrowsExactlyAsync<MessageSecurityException>(
                () => withResult ? proxy.CallWithResultAsync() : proxy.CallAsync());

            Assert.AreEqual(Forbidden, exception.Message);
            Assert.AreEqual(1, channel.Calls, "there is no challenge, so there is nothing to retry");
            Assert.IsNull(state.GetHeaders());
        }

        /// <summary>
        /// The nonce expires after a while and the device answers with stale=TRUE, which is a request to
        ///  authenticate again rather than a rejection of the credentials.
        /// </summary>
        [TestMethod]
        public void TestStaleChallengeIsAccepted()
        {
            var state = new HttpDigestState();

            HttpDigestChallengeSource.GetChallenges(
                HttpDigestChallengeSource.CreateExceptionMessage(HttpDigestMessages.Cs, Challenge), state);

            HttpDigestChallengeSource.GetChallenges(
                HttpDigestChallengeSource.CreateExceptionMessage(HttpDigestMessages.ZhHans, StaleChallenge), state);

            CollectionAssert.AreEqual(new[] { StaleChallenge }, state.GetHeaders());
        }

        /// <summary>
        /// A challenge that is repeated without stale=TRUE means the credentials were not accepted.
        /// </summary>
        [TestMethod]
        public void TestRepeatedChallengeIsRejected()
        {
            var state = new HttpDigestState();

            HttpDigestChallengeSource.GetChallenges(
                HttpDigestChallengeSource.CreateExceptionMessage(HttpDigestMessages.Cs, Challenge), state);

            Assert.ThrowsExactly<InvalidCredentialException>(() =>
                HttpDigestChallengeSource.GetChallenges(
                    HttpDigestChallengeSource.CreateExceptionMessage(HttpDigestMessages.Cs, Challenge), state));
        }
    }
}
