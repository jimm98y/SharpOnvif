using Microsoft.AspNetCore.Hosting.Server;
using SharpOnvifCommon;
using SharpOnvifServer;
using SharpOnvifServer.Events;
using System;

namespace OnvifService.Onvif
{
    public class SubscriptionManagerImpl : SubscriptionManagerBase
    {
        private readonly IServer _server;

        public SubscriptionManagerImpl(IServer server)
        {
            _server = server;
        }

        public override PullMessagesResponse PullMessages(PullMessagesRequest request)
        {
            return new PullMessagesResponse()
            {
                CurrentTime = System.DateTime.UtcNow,
                TerminationTime = System.DateTime.UtcNow.Add(OnvifHelpers.FromTimeout(request.Timeout)),
                NotificationMessage = new NotificationMessageHolderType[]
                {
                    new NotificationMessageHolderType()
                    {
                        Any = OnvifEvents.CreateNotificationMessage(
                            "tns1", 
                            "http://www.onvif.org/ver10/topics", 
                            "RuleEngine/CellMotionDetector/Motion",
                            DateTime.UtcNow, 
                            "Status", 
                            OnvifHelpers.DateTimeToString(DateTime.UtcNow))
                    }
                }
            };
        }

        public override RenewResponse1 Renew(RenewRequest request)
        {
            return new RenewResponse1()
            {
                RenewResponse = new RenewResponse()
                {
                    CurrentTime = DateTime.UtcNow,
                    TerminationTime = DateTime.UtcNow.Add(OnvifHelpers.FromTimeout(request.Renew.TerminationTime))
                }
            };
        }
    }
}
