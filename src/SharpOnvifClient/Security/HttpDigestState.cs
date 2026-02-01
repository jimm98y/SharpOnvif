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
    }

    public class HttpDigestState : IHttpMessageState
    {
        private readonly object _syncRoot = new object();

        private IEnumerable<string> _headers = null;
        private int _nc = 1;
        private (string nonce, string cnonce)? _prime;

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
    }
}
