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
using System.Threading;
using System.Threading.Tasks;
using SharpOnvifServer.Events;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// How a subscription is identified. The ID is the whole of it: Onvif addresses a subscription
    /// by putting the ID in the URL, so whoever can name an ID can read that subscription's events
    /// and cancel it.
    /// </summary>
    [TestClass]
    public sealed class TestEventSubscriptionManager
    {
        private sealed class Subscription : IEventSubscription
        {
            public DateTime ExpirationTime { get; set; } = DateTime.UtcNow.AddMinutes(10);
            public int Detached;
            public ManualResetEventSlim DetachEntered = new ManualResetEventSlim();
            public ManualResetEventSlim ReleaseDetach = new ManualResetEventSlim(true);

            public void Detach()
            {
                Interlocked.Increment(ref Detached);
                DetachEntered.Set();
                ReleaseDetach.Wait(TimeSpan.FromSeconds(10));
            }
        }

        [TestMethod]
        public void HandsOutIdsThatCannotBeGuessed()
        {
            // Sequential ids meant a client could reach every other subscription on the device by
            // counting: /onvif/Events/PullPointSubscription/1/, /2/, and so on.
            var manager = new DefaultEventSubscriptionManager<Subscription>();

            var ids = new List<string>();
            for (int i = 0; i < 200; i++) ids.Add(manager.AddSubscription(new Subscription()));

            Assert.AreEqual(ids.Count, ids.Distinct(StringComparer.Ordinal).Count(), "an id was handed out twice");

            foreach (string id in ids)
            {
                Assert.IsTrue(id.Length >= 20, $"'{id}' is too short to be unguessable");
                Assert.IsFalse(int.TryParse(id, out _), $"'{id}' is a number, so the next one can be guessed");

                foreach (char c in id)
                {
                    Assert.IsTrue(char.IsLetterOrDigit(c) || c == '-' || c == '_',
                        $"'{c}' in '{id}' does not belong in the last segment of an address");
                }
            }
        }

        [TestMethod]
        public void FindsASubscriptionOnlyByItsOwnId()
        {
            var manager = new DefaultEventSubscriptionManager<Subscription>();
            var mine = new Subscription();

            string id = manager.AddSubscription(mine);

            Assert.AreSame(mine, manager.GetSubscription(id));
            Assert.IsNull(manager.GetSubscription("1"), "counting must not reach a subscription");
            Assert.IsNull(manager.GetSubscription(id + "x"));
            string differentCase = id.ToUpperInvariant();
            if (!string.Equals(differentCase, id, StringComparison.Ordinal))
            {
                Assert.IsNull(manager.GetSubscription(differentCase),
                    "ids are opaque tokens and are compared exactly, so case matters");
            }
            Assert.IsNull(manager.GetSubscription(null));
            Assert.IsNull(manager.GetSubscription(""));
        }

        [TestMethod]
        public void ForgetsASubscriptionItRemoved()
        {
            var manager = new DefaultEventSubscriptionManager<Subscription>();
            var subscription = new Subscription();
            string id = manager.AddSubscription(subscription);

            manager.RemoveSubscription(id);

            Assert.IsNull(manager.GetSubscription(id));
            Assert.AreEqual(1, subscription.Detached, "the subscription has to be detached from its source");

            manager.RemoveSubscription(id);
            Assert.AreEqual(1, subscription.Detached, "removing twice must not detach twice");

            manager.RemoveSubscription("never existed");
            manager.RemoveSubscription(null);
        }

        [TestMethod]
        [Timeout(30000)]
        public void KeepsServingWhileOneSubscriptionIsDetaching()
        {
            // Detach is the implementation's own code - it may close a socket or wait on a device.
            // Running it under the manager's lock stopped every other subscription until it
            // returned, which on a shared event source is every client on the device.
            var manager = new DefaultEventSubscriptionManager<Subscription>();

            var slow = new Subscription { ReleaseDetach = new ManualResetEventSlim(false) };
            string slowId = manager.AddSubscription(slow);
            string otherId = manager.AddSubscription(new Subscription());

            Task removing = Task.Run(() => manager.RemoveSubscription(slowId));

            Assert.IsTrue(slow.DetachEntered.Wait(TimeSpan.FromSeconds(10)), "Detach was never called");

            // While that one is still inside Detach, the manager has to keep answering.
            Assert.IsNotNull(manager.GetSubscription(otherId), "a slow Detach held up every other subscription");
            Assert.IsNotNull(manager.AddSubscription(new Subscription()));

            slow.ReleaseDetach.Set();
            Assert.IsTrue(removing.Wait(TimeSpan.FromSeconds(10)));
        }
    }
}
