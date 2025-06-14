using System;

namespace SharpOnvifServer.Events
{
    public interface IEventSubscription
    {
        public DateTime ExpirationUTC { get; set; }
    }
}
