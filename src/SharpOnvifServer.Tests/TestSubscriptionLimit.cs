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
using SharpOnvifServer;
using SharpOnvifServer.Events;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// How many subscriptions a device will hold.
    /// <para>
    /// One outlives the request that made it and is swept only when it expires, so a client that
    /// subscribes in a loop - a broken one as easily as a hostile one - leaves the device holding
    /// every subscription it managed to ask for.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestSubscriptionLimit
    {
        private sealed class Subscription : IEventSubscription
        {
            public DateTime ExpirationTime { get; set; } = DateTime.UtcNow.AddMinutes(10);

            public void Detach()
            {
            }
        }

        [TestMethod]
        public void HoldsAsManyAsItSaysAndRefusesTheNext()
        {
            using var manager = new DefaultEventSubscriptionManager<Subscription> { MaxSubscriptions = 3 };

            for (int i = 0; i < 3; i++)
                Assert.IsNotNull(manager.AddSubscription(new Subscription()));

            var refused = Assert.ThrowsExactly<OnvifServerFaultException>(
                () => manager.AddSubscription(new Subscription()));

            StringAssert.Contains(refused.Message, "subscriptions",
                "a client is told, rather than left with one that never fires");
        }

        [TestMethod]
        public void TakesAnotherOnceOneHasGone()
        {
            // The limit is on how many are held, not on how many have ever been made.
            using var manager = new DefaultEventSubscriptionManager<Subscription> { MaxSubscriptions = 1 };

            string first = manager.AddSubscription(new Subscription());
            Assert.ThrowsExactly<OnvifServerFaultException>(() => manager.AddSubscription(new Subscription()));

            manager.RemoveSubscription(first);

            Assert.IsNotNull(manager.AddSubscription(new Subscription()));
        }

        [TestMethod]
        public void HoldsWhatItAlwaysDidByDefault()
        {
            // The default has to be far above what any real client asks for: a manager, a display
            // wall and a recorder watching one camera is three.
            using var manager = new DefaultEventSubscriptionManager<Subscription>();

            Assert.IsTrue(manager.MaxSubscriptions >= 1000);
        }
    }
}
