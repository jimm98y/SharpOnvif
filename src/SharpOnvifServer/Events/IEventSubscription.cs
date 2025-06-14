using System;

namespace SharpOnvifServer.Events
{
    /// <summary>
    /// Onvif Event Subscription represents the client's event subscription on the server. It holds the message queue (Pull Point) or the 
    ///  notification endpoint (Basic) and handles dispatching the events to the subscribers.
    /// </summary>
    public interface IEventSubscription
    {
        /// <summary>
        /// Subscription expiration time in UTC.
        /// </summary>
        DateTime ExpirationTime { get; set; }

        /// <summary>
        /// Detach subscription from the event source. Called before removing the subscription.
        /// </summary>
        void Detach();
    }
}
