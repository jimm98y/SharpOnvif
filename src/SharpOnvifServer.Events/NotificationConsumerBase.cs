using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Events
{
    [DisableMustUnderstandValidation]
    public class NotificationConsumerBase : NotificationConsumer
    {
        public virtual void Notify(Notify1 request)
        {
            throw new NotImplementedException();
        }
    }
}
