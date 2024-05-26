using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Events
{
    /// <summary>
    /// Contains default implemetation of all event interfaces.
    /// </summary>
    /// <remarks>
    /// Because of CoreWCF limitations when multiple services cannot share the binding URI, we have to implement all interfaces that
    ///  should be available on a single endpoint here.
    /// </remarks>
    [DisableMustUnderstandValidation]
    public class EventsBase : NotificationProducer, EventPortType, PullPoint, PullPointSubscription
    {
        #region NotificationProducer

        public virtual GetCurrentMessageResponse1 GetCurrentMessage(GetCurrentMessageRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual SubscribeResponse1 Subscribe(SubscribeRequest request)
        {
            throw new NotImplementedException();
        }

        #endregion // NotificationProducer

        #region EventPortType

        public virtual void AddEventBroker(EventBrokerConfig EventBroker)
        {
            throw new NotImplementedException();
        }

        public virtual CreatePullPointSubscriptionResponse CreatePullPointSubscription(CreatePullPointSubscriptionRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteEventBrokerResponse DeleteEventBroker(DeleteEventBrokerRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "EventBroker")]
        public virtual GetEventBrokersResponse GetEventBrokers(GetEventBrokersRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetEventPropertiesResponse GetEventProperties(GetEventPropertiesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        #endregion // EventPortType

        #region PullPoint

        public virtual DestroyPullPointResponse1 DestroyPullPoint(DestroyPullPointRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetMessagesResponse1 GetMessages(GetMessagesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void Notify(Notify1 request)
        {
            throw new NotImplementedException();
        }

        #endregion // PullPoint

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

        public virtual UnsubscribeResponse1 Unsubscribe(UnsubscribeRequest request)
        {
            throw new System.NotImplementedException();
        }

        #endregion // PullPointSubscription
    }
}
