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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SharpOnvifCommon.Security;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The two pieces of state HTTP Digest keeps on the server: the private key that makes a nonce
    /// unforgeable, and the record of which nonces have been spent.
    /// <para>
    /// These tests replace the process-wide nonce private key, so they run on their own.
    /// </para>
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class TestDigestNonceState
    {
        private const string Algorithm = "MD5";
        private const int SaltLength = 12;

        private static string FreshNonce(DateTimeOffset when) =>
            HttpDigestAuthentication.GenerateServerNonce(
                Algorithm, BinarySerializationType.Hex, when, null,
                HttpDigestAuthentication.CreateNonceSessionSalt(SaltLength));

        private static Task<int> Validate(string nonce, int nc, DateTimeOffset when, INonceReplayStore store) =>
            HttpDigestAuthentication.ValidateServerNonceAsync(
                Algorithm, BinarySerializationType.Hex, nonce, nc, when, null, SaltLength, 30000, true, store);

        [TestCleanup]
        public void RestoreARandomKey()
        {
            // Leaving a key these tests chose in place would let a later run of the process be
            // predicted from this source file.
            HttpDigestAuthentication.RegenerateNoncePrivateKey();
        }

        [TestMethod]
        public void KeepsTheNoncePrivateKeyToItself()
        {
            // Whoever can read the key can mint nonces the server will accept as its own, and
            // whoever can write it can invalidate every session at will. Neither belongs on the
            // public surface of a static class that any code in the process can reach.
            IEnumerable<string> exposed = typeof(HttpDigestAuthentication)
                .GetMembers(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(member => member.Name.IndexOf("PrivateKey", StringComparison.OrdinalIgnoreCase) >= 0)
                .Where(member => member is FieldInfo || member is PropertyInfo)
                .Select(member => member.Name);

            CollectionAssert.AreEqual(new string[0], exposed.ToArray(),
                "the nonce private key must not be readable or writable as a field or a property");
        }

        [TestMethod]
        public async Task StopsAcceptingNoncesItIssuedUnderAnOlderKey()
        {
            var now = DateTimeOffset.UtcNow;
            string nonce = FreshNonce(now);

            HttpDigestAuthentication.RegenerateNoncePrivateKey();

            Assert.AreEqual(HttpDigestAuthentication.ERROR_NONCE_INVALID,
                await Validate(nonce, 1, now, new MemoryNonceReplayStore()),
                "a nonce is validated by recomputing it, so a new key retires every nonce in flight");
        }

        [TestMethod]
        public async Task AcceptsANonceFromAnotherInstanceGivenTheSameKey()
        {
            // What a deployment behind a load balancer needs: every instance holds the same key,
            // so a nonce minted by whichever instance issued the challenge validates at whichever
            // instance the next request lands on.
            byte[] sharedKey = new byte[32];
            for (int i = 0; i < sharedKey.Length; i++) sharedKey[i] = (byte)(i * 7 + 1);

            HttpDigestAuthentication.SetNoncePrivateKey(sharedKey);
            var now = DateTimeOffset.UtcNow;
            string issuedByTheFirstInstance = FreshNonce(now);

            // The second instance is given the same key from the same secret store.
            HttpDigestAuthentication.SetNoncePrivateKey(sharedKey);

            Assert.AreEqual(0, await Validate(issuedByTheFirstInstance, 1, now, new MemoryNonceReplayStore()));
        }

        [TestMethod]
        public async Task CopiesTheKeyItIsGivenSoTheCallerCanClearItsOwn()
        {
            byte[] key = new byte[32];
            for (int i = 0; i < key.Length; i++) key[i] = (byte)(i + 1);

            HttpDigestAuthentication.SetNoncePrivateKey(key);
            var now = DateTimeOffset.UtcNow;
            string nonce = FreshNonce(now);

            // A caller that reads the key out of a secret store should wipe its own copy. Holding
            // the caller's array would turn that into a silent change of key.
            Array.Clear(key, 0, key.Length);

            Assert.AreEqual(0, await Validate(nonce, 1, now, new MemoryNonceReplayStore()));
        }

        [TestMethod]
        public void RefusesAKeyItCannotUse()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => HttpDigestAuthentication.SetNoncePrivateKey(null));
            Assert.ThrowsExactly<ArgumentException>(() => HttpDigestAuthentication.SetNoncePrivateKey(new byte[0]));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => HttpDigestAuthentication.RegenerateNoncePrivateKey(0));
            Assert.ThrowsExactly<ArgumentNullException>(() => HttpDigestAuthentication.NonceReplayStore = null);
        }

        [TestMethod]
        public async Task SpendsANonceInTheStoreItWasGiven()
        {
            var store = new CountingStore();
            var now = DateTimeOffset.UtcNow;
            string nonce = FreshNonce(now);

            Assert.AreEqual(0, await Validate(nonce, 1, now, store));

            Assert.AreEqual(1, store.Calls, "the store passed in has to be the one consulted");
            Assert.AreEqual(nonce, store.LastNonce);
            Assert.AreEqual(1, store.LastCount);
        }

        [TestMethod]
        public async Task DoesNotSpendANonceThatFailedAnEarlierCheck()
        {
            // A nonce that is expired, forged or malformed is refused before the store is touched,
            // so a flood of junk cannot fill it.
            var store = new CountingStore();
            var now = DateTimeOffset.UtcNow;

            Assert.AreEqual(HttpDigestAuthentication.ERROR_NONCE_EXPIRED,
                await Validate(FreshNonce(now.AddMinutes(-5)), 1, now, store));
            Assert.AreEqual(0, store.Calls);
        }

        [TestMethod]
        public async Task RemembersSpentNoncesOnlyWithinOneStore()
        {
            // The hazard the abstraction exists for: two instances each keeping the record in
            // their own memory both accept the same captured request. Sharing one store is what
            // makes replay protection hold across them.
            var now = DateTimeOffset.UtcNow;
            string nonce = FreshNonce(now);

            var instanceA = new MemoryNonceReplayStore();
            var instanceB = new MemoryNonceReplayStore();

            Assert.AreEqual(0, await Validate(nonce, 4, now, instanceA));
            Assert.AreEqual(0, await Validate(nonce, 4, now, instanceB),
                "a store of its own is a store that has never seen this request");

            var shared = new MemoryNonceReplayStore();
            Assert.AreEqual(0, await Validate(nonce, 4, now, shared));
            Assert.AreEqual(HttpDigestAuthentication.ERROR_NONCE_REUSE, await Validate(nonce, 4, now, shared),
                "one store for both instances is what refuses the replay");
        }

        [TestMethod]
        public async Task FallsBackToTheProcessWideStore()
        {
            var previous = HttpDigestAuthentication.NonceReplayStore;
            var store = new CountingStore();
            try
            {
                HttpDigestAuthentication.NonceReplayStore = store;

                var now = DateTimeOffset.UtcNow;
                Assert.AreEqual(0, await Validate(FreshNonce(now), 1, now, null));
                Assert.AreEqual(1, store.Calls);
            }
            finally
            {
                HttpDigestAuthentication.NonceReplayStore = previous;
            }
        }

        private sealed class CountingStore : INonceReplayStore
        {
            public int Calls;
            public string LastNonce;
            public int LastCount;

            public Task<bool> TryUseNonceAsync(string nonce, int nonceCount, DateTimeOffset expiresAt, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref Calls);
                LastNonce = nonce;
                LastCount = nonceCount;
                return Task.FromResult(true);
            }
        }
    }
}
