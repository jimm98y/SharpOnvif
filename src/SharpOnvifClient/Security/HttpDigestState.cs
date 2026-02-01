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

using SharpOnvifCommon.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;

namespace SharpOnvifClient.Security
{
    public interface IHttpMessageState
    {
        int PeekNC();
        int GetAndUpdateNC();
        string[] GetHeaders();
        void SetHeaders(IEnumerable<string> headers);
        (string nonce, string cnonce)? GetNoncePrime();
        void SetNoncePrime((string nonce, string cnonce)? nonce);
        void SetNextNonce(string nextnonce);
        string GetNextNonce();
    }

    public class HttpDigestState : IHttpMessageState
    {
        private readonly object _syncRoot = new object();

        private IEnumerable<string> _headers = null;
        private int _nc = 1;
        private (string nonce, string cnonce)? _prime;
        private string _nextNonce;

        public (string nonce, string cnonce)? GetNoncePrime()
        {
            lock(_syncRoot)
            {
                return _prime;
            }
        }

        public void SetNoncePrime((string nonce, string cnonce)? prime)
        {
            lock(_syncRoot)
            {
                _prime = prime;
            }
        }

        public int PeekNC()
        {
            return _nc; 
        }

        public int GetAndUpdateNC()
        {
            lock (_syncRoot)
            {
                int nc = _nc;
                _nc++;
                return nc;
            }
        }

        public string[] GetHeaders()
        {
            lock (_syncRoot)
            {
                return _headers?.ToArray();
            }
        }

        public void SetHeaders(IEnumerable<string> headers)
        {
            ValidateHeaders(_headers?.ToArray(), headers?.ToArray());
            lock (_syncRoot)
            {
                _headers = headers;
                _nc = 1;
                _prime = null;
                _nextNonce = null;
            }
        }

        private void ValidateHeaders(string[] oldHeaders, string[] newHeaders)
        {
            if(newHeaders == null)
            {
                throw new ArgumentNullException(nameof(newHeaders));
            }

            // the first time stale is FALSE because it's the first WWW-Authenticate response
            if(oldHeaders != null)
            {
                bool wasStale = false;
                for (int i = 0; i < oldHeaders.Length; i++)
                {
                    wasStale |= (HttpDigestAuthentication.GetValueFromHeader(oldHeaders[i], "stale", false) ?? "").ToUpperInvariant() == "TRUE";
                }

                bool isStale = false;
                for (int i = 0; i < newHeaders.Length; i++)
                {
                    isStale |= (HttpDigestAuthentication.GetValueFromHeader(newHeaders[i], "stale", false) ?? "").ToUpperInvariant() == "TRUE";
                }

                if(wasStale == false && isStale == false)
                {
                    throw new InvalidCredentialException();
                }
            }
        }

        public void SetNextNonce(string nextnonce)
        {
            lock (_syncRoot)
            {
                _nextNonce = nextnonce;
            }
        }

        public string GetNextNonce()
        {
            lock (_syncRoot)
            {
                return _nextNonce;
            }
        }
    }
}
