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
using System.Security.Cryptography;
using System.Threading;

namespace SharpOnvifServer.Events
{
    public class DefaultEventSubscriptionManager<T> : IDisposable, IEventSubscriptionManager<T> where T: class, IEventSubscription
    {
        private bool _disposedValue;

        /// <summary>
        /// Bytes of randomness in a subscription ID. A subscription is addressed by ID alone, so
        /// the ID is the only thing standing between one client and another client's events.
        /// </summary>
        private const int SubscriptionIdBytes = 16;

        /// <summary>
        /// Most subscriptions this device will hold at once.
        /// </summary>
        /// <remarks>
        /// A subscription outlives the request that made it and is swept only when it expires, so
        /// without a limit a client that subscribes in a loop leaves a device holding as many as
        /// it managed to ask for. Far above what a real client needs - a manager, a display wall
        /// and a recorder watching one camera is three - and low enough to bound the memory a
        /// device can be made to hold.
        /// </remarks>
        public int MaxSubscriptions { get; set; } = 1000;

        private readonly Timer _expirationTimer;
        private object _syncRoot = new object();

        // Ordinal: the ID is an opaque token, and two that differ by a byte are two subscriptions.
        private Dictionary<string, T> _subscriptions =
            new Dictionary<string, T>(StringComparer.Ordinal);

        public DefaultEventSubscriptionManager()
        {
            _expirationTimer = new Timer(OnCheckExpiration, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));
        }

        private void OnCheckExpiration(object state)
        {
            List<string> subscriptionsToRemove = new List<string>();
            lock(_syncRoot)
            {
                foreach(var subscription in _subscriptions)
                {
                    if(subscription.Value.ExpirationTime < DateTime.UtcNow)
                    {
                        subscriptionsToRemove.Add(subscription.Key);
                    }
                }
            }

            foreach(var subscriptionID in subscriptionsToRemove)
            {
                RemoveSubscription(subscriptionID);
            }
        }

        public string AddSubscription(T subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            lock (_syncRoot)
            {
                if (MaxSubscriptions > 0 && _subscriptions.Count >= MaxSubscriptions)
                {
                    // Told rather than dropped: a client that is refused can unsubscribe what it
                    // no longer needs, where one whose subscription silently never fires cannot.
                    OnvifErrors.ReturnReceiverError(
                        "The device is holding as many event subscriptions as it can.",
                        "TooManySubscriptions");
                }

                string subscriptionID = CreateSubscriptionID();
                _subscriptions.Add(subscriptionID, subscription);
                return subscriptionID;
            }
        }

        public T GetSubscription(string subscriptionID)
        {
            if (string.IsNullOrEmpty(subscriptionID))
                return null;

            lock (_syncRoot)
            {
                T ret = null;
                _subscriptions.TryGetValue(subscriptionID, out ret);
                return ret;
            }
        }

        public void RemoveSubscription(string subscriptionID)
        {
            if (string.IsNullOrEmpty(subscriptionID))
                return;

            T subscription;
            lock (_syncRoot)
            {
                if (!_subscriptions.TryGetValue(subscriptionID, out subscription))
                    return;

                _subscriptions.Remove(subscriptionID);
            }

            // Outside the lock: Detach is the implementation's own code, and running it here would
            // hold every other subscription for as long as it takes.
            subscription.Detach();
        }

        /// <summary>
        /// An ID a client cannot guess, rendered for a URL - it is handed out as the last segment
        /// of the subscription's address.
        /// </summary>
        private static string CreateSubscriptionID()
        {
            byte[] bytes = new byte[SubscriptionIdBytes];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }

            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        #region IDisposable implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _expirationTimer.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion // IDisposable implementation
    }
}
