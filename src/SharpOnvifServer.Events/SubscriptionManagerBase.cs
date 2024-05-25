using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Events
{
    [DisableMustUnderstandValidation]
    public class SubscriptionManagerBase : SubscriptionManager
    {
        public virtual RenewResponse1 Renew(RenewRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual UnsubscribeResponse1 Unsubscribe(UnsubscribeRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
