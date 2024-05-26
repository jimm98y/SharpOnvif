using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Events
{
    [DisableMustUnderstandValidation]
    public class SubscriptionManagerBase : SubscriptionManager, PausableSubscriptionManager, PullPointSubscription
    {
        #region SubscriptionManager

        public virtual RenewResponse1 Renew(RenewRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual UnsubscribeResponse1 Unsubscribe(UnsubscribeRequest request)
        {
            throw new NotImplementedException();
        }

        #endregion // SubscriptionManager

        #region PausableSubscriptionManager

        public virtual PauseSubscriptionResponse1 PauseSubscription(PauseSubscriptionRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual ResumeSubscriptionResponse1 ResumeSubscription(ResumeSubscriptionRequest request)
        {
            throw new NotImplementedException();
        }

        #endregion // PausableSubscriptionManager

        #region PullPointSubscription

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

        #endregion // PullPointSubscription
    }
}
