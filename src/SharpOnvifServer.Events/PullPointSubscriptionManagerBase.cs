using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Events
{
    [DisableMustUnderstandValidation]
    public class PullPointSubscriptionManagerBase : PullPointSubscription
    {
        public virtual UnsubscribeResponse1 Unsubscribe(UnsubscribeRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual PullMessagesResponse PullMessages(PullMessagesRequest request)
        {
            throw new System.NotImplementedException();
        }

        public virtual SeekResponse Seek(SeekRequest request)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetSynchronizationPoint()
        {
            throw new System.NotImplementedException();
        }

        // added - see https://github.com/onvif/specs/issues/506
        public virtual RenewResponse1 Renew(RenewRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
