using Microsoft.AspNetCore.Hosting.Server;
using SharpOnvifCommon;
using SharpOnvifServer;
using SharpOnvifServer.Events;
using System;

namespace OnvifService.Onvif
{
    public class EventsImpl : EventsBase
    {
        private readonly IServer _server;

        public EventsImpl(IServer server)
        {
            _server = server;
        }

        #region NotificationProducer

        public override SubscribeResponse1 Subscribe(SubscribeRequest request)
        {
            string notificationEndpoint = request.Subscribe.ConsumerReference.Address.Value;
            string subscriptionReferenceUri = $"{_server.GetHttpEndpoint()}/onvif/Events/SubManager";
            DateTime now = DateTime.UtcNow;
            DateTime termination = DateTime.UtcNow.Add(OnvifHelpers.FromTimeout(request.Subscribe.InitialTerminationTime));

            return new SubscribeResponse1(new SubscribeResponse()
            {
                 SubscriptionReference = new EndpointReferenceType()
                 {
                     Address = new AttributedURIType()
                     {
                         Value = subscriptionReferenceUri
                     }
                 },
                 CurrentTime = now,
                 TerminationTime = termination
            });
        }

        #endregion // NotificationProducer

        #region EventPort

        public override CreatePullPointSubscriptionResponse CreatePullPointSubscription(CreatePullPointSubscriptionRequest request)
        {
            string subscriptionReferenceUri = $"{_server.GetHttpEndpoint()}/onvif/Events/SubManager";
            return new CreatePullPointSubscriptionResponse()
            {
                CurrentTime = System.DateTime.UtcNow,
                TerminationTime = System.DateTime.UtcNow.Add(OnvifHelpers.FromTimeout(request.InitialTerminationTime)),
                SubscriptionReference = new EndpointReferenceType()
                {
                    Address = new AttributedURIType() { Value = subscriptionReferenceUri }
                }
            };
        }

        #endregion // EventPort
    }
}
