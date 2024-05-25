using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Events
{
    // TODO: Review
    [DisableMustUnderstandValidation]
    public class EventsBase : NotificationConsumer
    {
        public virtual void Notify(Notify1 request)
        {
            throw new NotImplementedException();
        }
    }
}
