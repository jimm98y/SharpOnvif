using System;

namespace SharpOnvifServer.Events
{
    /// <summary>
    /// Event source generates events that can be subscribed by the clients.
    /// </summary>
    public interface IEventSource
    {
        /// <summary>
        /// Raised when a new event is generated. Subscribed by all currently active subscriptions.
        /// </summary>
        event EventHandler<NotificationEventArgs> OnEvent;
    }
}
