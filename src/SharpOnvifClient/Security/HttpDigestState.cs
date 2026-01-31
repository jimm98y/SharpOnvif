using System;
using System.Collections.Generic;
using System.Threading;

namespace SharpOnvifClient.Security
{
    public class HttpDigestState
    {
        public int _nc = 1;
        public IEnumerable<string> Headers { get; private set; }

        public int PeekNC()
        {
            return _nc; 
        }

        public int GetAndUpdateNC()
        {
            return Interlocked.Increment(ref _nc);
        }

        public void SetResponse(IEnumerable<string> headers)
        {
            this.Headers = headers;
            Interlocked.Exchange(ref _nc, 1);
        }
    }
}
