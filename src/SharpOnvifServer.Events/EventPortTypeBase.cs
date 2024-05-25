using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Events
{
    [DisableMustUnderstandValidation]
    public class EventPortTypeBase : EventPortType
    {
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
    }
}
