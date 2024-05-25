using SharpOnvifServer.Security;

namespace SharpOnvifServer.Events
{
    [DisableMustUnderstandValidation]
    public class PullPointSubscriptionBase : PullPointSubscription
    {
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

        public virtual UnsubscribeResponse1 Unsubscribe(UnsubscribeRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}
