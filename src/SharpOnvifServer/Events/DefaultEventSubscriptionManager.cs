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
                    if(subscription.Value.ExpirationUTC < DateTime.UtcNow)
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
            T ret = null;
            lock (_syncRoot)
            {
                _subscriptions.TryGetValue(subscriptionID, out ret);
            }
            return ret;
        }

        public void RemoveSubscription(int subscriptionID)
        {
            lock (_syncRoot)
            {
                _subscriptions.Remove(subscriptionID);
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
