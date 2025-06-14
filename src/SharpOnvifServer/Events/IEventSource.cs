using System;

namespace SharpOnvifServer.Events
{
    public class NotificationEventArgs : EventArgs
    {
        public DateTime Created { get; set; } = DateTime.UtcNow;

        public NotificationMessage Message { get; set; }
        
        public NotificationEventArgs(NotificationMessage message)
        {
            this.Message = message;
        }
    }

    public interface IEventSource
    {
        event EventHandler<NotificationEventArgs> OnEvent;
    }
}
