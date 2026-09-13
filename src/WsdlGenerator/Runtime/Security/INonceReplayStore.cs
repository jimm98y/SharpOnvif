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
using System.Threading;
using System.Threading.Tasks;

namespace __RUNTIME__.Security
{
    /// <summary>
    /// Remembers which server nonces have already been spent, so that a captured request cannot be
    /// replayed against the server that issued the nonce.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Replay protection is state, and every instance that answers the same challenge has to see
    /// the same state. <see cref="MemoryNonceReplayStore"/>, the default, keeps that state in the
    /// process - correct for a single device or a single server instance, and only for those. Run
    /// two instances behind a load balancer and a request the issuing instance refuses is accepted
    /// by its neighbour, which is to say replay protection is gone. A deployment like that has to
    /// supply a store all of its instances can read, and give them all the same nonce private key
    /// through <see cref="HttpDigestAuthentication.SetNoncePrivateKey(byte[])"/> so that a nonce
    /// issued by one instance validates at another.
    /// </para>
    /// <para>
    /// Install a store per authentication scheme through
    /// <c>DigestAuthenticationSchemeOptions.HttpDigestNonceReplayStore</c>, or process-wide through
    /// <see cref="HttpDigestAuthentication.NonceReplayStore"/>. Implementations are called
    /// concurrently and must be thread safe.
    /// </para>
    /// </remarks>
    public interface INonceReplayStore
    {
        /// <summary>
        /// Spends <paramref name="nonce"/> at <paramref name="nonceCount"/>.
        /// </summary>
        /// <param name="nonce">The server nonce the client presented.</param>
        /// <param name="nonceCount">
        /// The nonce count the client presented, already decoded from its hexadecimal form. It is
        /// zero for RFC 2069 clients, which do not count.
        /// </param>
        /// <param name="expiresAt">
        /// When the nonce stops being valid. The store may forget it from this moment on; the
        /// nonce's own lifetime check refuses it thereafter.
        /// </param>
        /// <param name="cancellationToken">Cancels the operation.</param>
        /// <returns>
        /// <c>true</c> when the count is ahead of every count already seen for this nonce, meaning
        /// the request is fresh; <c>false</c> when the count has been seen, meaning the request is
        /// a replay.
        /// </returns>
        Task<bool> TryUseNonceAsync(string nonce, int nonceCount, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    }

    /// <summary>
    /// The default <see cref="INonceReplayStore"/>, holding spent nonces in the memory of this one
    /// process. See the remarks on <see cref="INonceReplayStore"/> for when that is not enough.
    /// </summary>
    public sealed class MemoryNonceReplayStore : INonceReplayStore
    {
        private static readonly Task<bool> Fresh = Task.FromResult(true);
        private static readonly Task<bool> Replayed = Task.FromResult(false);

        // Ordinal: a nonce is an opaque token, and two that differ by a byte are two nonces.
        private readonly ExpiringCache<string, int> _spent =
            new ExpiringCache<string, int>(StringComparer.Ordinal);

        // Reading the highest count seen and recording a new one is a single decision.
        private readonly object _syncRoot = new object();

        /// <inheritdoc/>
        public Task<bool> TryUseNonceAsync(string nonce, int nonceCount, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(nonce))
                throw new ArgumentNullException(nameof(nonce));

            cancellationToken.ThrowIfCancellationRequested();

            // Record the count that was actually presented, not 1. Recording 1 leaves every count
            // below the one seen still acceptable, so a captured request whose count is higher -
            // which is what a client sends after retrying a lost request - could be replayed once.
            lock (_syncRoot)
            {
                if (_spent.TryGet(nonce, out int seen) && nonceCount <= seen)
                    return Replayed;

                _spent.Set(nonce, nonceCount, expiresAt);
                return Fresh;
            }
        }
    }
}
