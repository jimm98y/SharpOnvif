using SharpOnvifCommon;
using SharpOnvifServer.Events;
using System;

namespace OnvifService.Onvif
{
    public class NotificationProducerImpl : NotificationProducerBase
    {
        public override SubscribeResponse1 Subscribe(SubscribeRequest request)
        {
            string notificationEndpoint = request.Subscribe.ConsumerReference.Address.Value;
            string subscriptionReferenceUri = $"http://{NetworkHelpers.GetIPv4NetworkInterface()}:5000/onvif/Events/SubManager";
            DateTime now = DateTime.UtcNow;
            DateTime termination = DateTime.UtcNow.Add(OnvifHelpers.FromTimeout(request.Subscribe.InitialTerminationTime).Value);

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
    }
}
