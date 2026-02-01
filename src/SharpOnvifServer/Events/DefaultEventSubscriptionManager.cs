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
using System.Threading;

namespace SharpOnvifServer.Events
{
    public class DefaultEventSubscriptionManager<T> : IDisposable, IEventSubscriptionManager<T> where T: class, IEventSubscription
    {
        private bool _disposedValue;

        private readonly Timer _expirationTimer;
        private int _subscriptionID = 1; // start with ID 1, 0 is used for an error state
        private object _syncRoot = new object();
        private Dictionary<int, T> _subscriptions = new Dictionary<int, T>();

        public DefaultEventSubscriptionManager()
        {
            _expirationTimer = new Timer(OnCheckExpiration, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60));
        }

        private void OnCheckExpiration(object state)
        {
            List<int> subscriptionsToRemove = new List<int>();
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

        public int AddSubscription(T subscription)
        {
            lock (_syncRoot)
            {
                _subscriptions.Add(_subscriptionID, subscription);
                return _subscriptionID++;
            }
        }

        public T GetSubscription(int subscriptionID)
        {
            lock (_syncRoot)
            {
                T ret = null;
                _subscriptions.TryGetValue(subscriptionID, out ret);
                return ret;
            }
        }

        public void RemoveSubscription(int subscriptionID)
        {
            lock (_syncRoot)
            {
                T subscription;
                if(_subscriptions.TryGetValue(subscriptionID, out subscription))
                {
                    subscription.Detach();
                    _subscriptions.Remove(subscriptionID);
                }
            }
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
