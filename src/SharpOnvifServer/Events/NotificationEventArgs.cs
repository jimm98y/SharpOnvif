using System;

namespace SharpOnvifServer.Events
{
    public class NotificationEventArgs : EventArgs
    {
        public DateTime Created { get; } = DateTime.UtcNow;

        public NotificationMessage Message { get; }

        public NotificationEventArgs(NotificationMessage message)
        {
            this.Message = message ?? throw new ArgumentNullException(nameof(message));
        }
    }
}
