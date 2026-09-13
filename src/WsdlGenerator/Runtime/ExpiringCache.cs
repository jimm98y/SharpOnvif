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

namespace __RUNTIME__
{
    /// <summary>
    /// A keyed store whose entries stop existing at a moment fixed when they are written.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Digest authentication needs to remember two things for as long as a nonce lives and no
    /// longer: which nonces have been spent, and the nonce a -sess session was primed with. That
    /// is all this does. It replaces System.Runtime.Caching, whose one use here was an absolute
    /// expiration - everything else that package offers, and the dependency it costs on three
    /// target frameworks, went unused.
    /// </para>
    /// <para>
    /// Expired entries are dropped when they are looked up, and the whole store is swept when it
    /// has grown past the size of the last sweep, so a server that is issuing nonces steadily
    /// settles at roughly the number that are live at once rather than growing without bound.
    /// Every member is safe to call concurrently.
    /// </para>
    /// </remarks>
    internal sealed class ExpiringCache<TKey, TValue>
    {
        private const int InitialSweepThreshold = 64;

        private readonly Dictionary<TKey, Entry> _entries;
        private readonly object _sync = new object();
        private int _sweepThreshold = InitialSweepThreshold;

        public ExpiringCache(IEqualityComparer<TKey> comparer = null)
        {
            _entries = comparer == null ? new Dictionary<TKey, Entry>() : new Dictionary<TKey, Entry>(comparer);
        }

        /// <summary>How many entries are held, expired ones included. For tests.</summary>
        public int Count
        {
            get { lock (_sync) { return _entries.Count; } }
        }

        /// <summary>
        /// Reads an entry that has not expired.
        /// </summary>
        public bool TryGet(TKey key, out TValue value)
        {
            lock (_sync)
            {
                if (_entries.TryGetValue(key, out Entry entry))
                {
                    if (entry.ExpiresAt > DateTimeOffset.UtcNow)
                    {
                        value = entry.Value;
                        return true;
                    }

                    _entries.Remove(key);
                }

                value = default(TValue);
                return false;
            }
        }

        /// <summary>
        /// Writes an entry, replacing any entry already under that key. An entry whose expiry has
        /// already passed is written and immediately unreadable, which is what asking for it
        /// means.
        /// </summary>
        public void Set(TKey key, TValue value, DateTimeOffset expiresAt)
        {
            lock (_sync)
            {
                _entries[key] = new Entry(value, expiresAt);

                if (_entries.Count >= _sweepThreshold)
                {
                    Sweep();
                }
            }
        }

        public void Remove(TKey key)
        {
            lock (_sync)
            {
                _entries.Remove(key);
            }
        }

        /// <summary>Drops every expired entry. Called with the lock held.</summary>
        private void Sweep()
        {
            var now = DateTimeOffset.UtcNow;
            List<TKey> expired = null;

            foreach (var pair in _entries)
            {
                if (pair.Value.ExpiresAt <= now)
                {
                    expired = expired ?? new List<TKey>();
                    expired.Add(pair.Key);
                }
            }

            if (expired != null)
            {
                foreach (TKey key in expired) _entries.Remove(key);
            }

            // Sweeping again at every write once the live set is large would cost a full pass per
            // write. Waiting until the store has doubled keeps it amortized, and the store stays
            // within twice what is genuinely live.
            _sweepThreshold = Math.Max(InitialSweepThreshold, _entries.Count * 2);
        }

        private readonly struct Entry
        {
            public Entry(TValue value, DateTimeOffset expiresAt)
            {
                Value = value;
                ExpiresAt = expiresAt;
            }

            public TValue Value { get; }
            public DateTimeOffset ExpiresAt { get; }
        }
    }
}
