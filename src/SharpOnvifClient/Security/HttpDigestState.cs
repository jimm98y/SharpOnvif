using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpOnvifClient.Security
{
    public class HttpDigestState
    {
        private readonly object _syncRoot = new object();

        private IEnumerable<string> _headers = null;
        public int _nc = 1;

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

        public void SetResponse(IEnumerable<string> headers)
        {
            lock (_syncRoot)
            {
                _headers = headers;
                _nc = 1;
            }
        }
    }
}
