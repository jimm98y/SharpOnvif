using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Events
{
    [DisableMustUnderstandValidation]
    public class PausableSubscriptionManagerBase : PausableSubscriptionManager
    {
        public virtual PauseSubscriptionResponse1 PauseSubscription(PauseSubscriptionRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual RenewResponse1 Renew(RenewRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual ResumeSubscriptionResponse1 ResumeSubscription(ResumeSubscriptionRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual UnsubscribeResponse1 Unsubscribe(UnsubscribeRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
