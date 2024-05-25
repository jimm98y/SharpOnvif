using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Events
{
    [DisableMustUnderstandValidation]
    public class NotificationProducerBase : NotificationProducer
    {
        public virtual GetCurrentMessageResponse1 GetCurrentMessage(GetCurrentMessageRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual SubscribeResponse1 Subscribe(SubscribeRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
