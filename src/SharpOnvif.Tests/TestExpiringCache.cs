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
using SharpOnvifCommon;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The cache Digest authentication keeps its nonce state in, which is all that
    /// System.Runtime.Caching was ever used for here.
    /// </summary>
    [TestClass]
    public sealed class TestExpiringCache
    {
        private static DateTimeOffset Soon => DateTimeOffset.UtcNow.AddMinutes(5);
        private static DateTimeOffset Already => DateTimeOffset.UtcNow.AddMinutes(-5);

        [TestMethod]
        public void ReadsBackWhatWasWritten()
        {
            var cache = new ExpiringCache<string, int>();
            cache.Set("a", 1, Soon);

            Assert.IsTrue(cache.TryGet("a", out int value));
            Assert.AreEqual(1, value);

            Assert.IsFalse(cache.TryGet("b", out value), "a key never written is absent");
            Assert.AreEqual(0, value, "the value is left at its default when there is none");
        }

        [TestMethod]
        public void ReplacesAnEntryUnderTheSameKey()
        {
            var cache = new ExpiringCache<string, int>();
            cache.Set("a", 1, Soon);
            cache.Set("a", 2, Soon);

            Assert.IsTrue(cache.TryGet("a", out int value));
            Assert.AreEqual(2, value);
            Assert.AreEqual(1, cache.Count, "replacing is not adding");
        }

        [TestMethod]
        public void WillNotReadAnEntryPastItsExpiry()
        {
            // This is the whole point of the cache: a nonce that has outlived its lifetime must
            // not still be answering for itself.
            var cache = new ExpiringCache<string, int>();
            cache.Set("a", 1, Already);

            Assert.IsFalse(cache.TryGet("a", out _));
            Assert.AreEqual(0, cache.Count, "an entry found expired is dropped there and then");
        }

        [TestMethod]
        public void Forgets()
        {
            var cache = new ExpiringCache<string, int>();
            cache.Set("a", 1, Soon);
            cache.Remove("a");

            Assert.IsFalse(cache.TryGet("a", out _));
            cache.Remove("a");
            cache.Remove("never written");
        }

        [TestMethod]
        public void DoesNotGrowWithEntriesThatHaveExpired()
        {
            // A server issues a nonce per challenge and never revisits most of them, so nothing
            // will come back to read an entry and find it expired. Without a sweep the store would
            // hold every nonce the process ever issued.
            var cache = new ExpiringCache<string, int>();

            for (int i = 0; i < 10000; i++)
            {
                cache.Set("nonce " + i, i, Already);
            }

            Assert.IsTrue(cache.Count < 200, $"the store kept {cache.Count} expired entries");
        }

        [TestMethod]
        public void KeepsWhatIsStillLiveWhileItSweeps()
        {
            var cache = new ExpiringCache<string, int>();
            cache.Set("live", 42, Soon);

            for (int i = 0; i < 10000; i++)
            {
                cache.Set("nonce " + i, i, Already);
            }

            Assert.IsTrue(cache.TryGet("live", out int value), "a sweep must not take a live entry with it");
            Assert.AreEqual(42, value);
        }

        [TestMethod]
        public void SurvivesBeingUsedFromEveryThreadAtOnce()
        {
            var cache = new ExpiringCache<int, int>();

            Parallel.For(0, 2000, i =>
            {
                cache.Set(i % 50, i, Soon);
                cache.TryGet(i % 50, out _);
                if (i % 7 == 0) cache.Remove(i % 50);
                cache.Set(i % 50 + 1000, i, Already);
            });

            Assert.IsTrue(cache.Count <= 200, "the sweep has to keep working under contention");
        }
    }
}
