using Microsoft.AspNetCore.Hosting.Server;
using SharpOnvifCommon;
using SharpOnvifServer;
using SharpOnvifServer.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            Task.Delay(5000).Wait(); // forced delay

            return new PullMessagesResponse()
            {
                CurrentTime = DateTime.UtcNow,
                TerminationTime = DateTime.UtcNow.Add(OnvifHelpers.FromTimeout(request.Timeout)),
                NotificationMessage = new NotificationMessageHolderType[]
                {
                    new NotificationMessageHolderType()
                    {
                        Any = OnvifEvents.CreateNotificationMessage(
                            new NotificationMessage()
                            {
                                TopicNamespacePrefix = "tns1",
                                TopicNamespace = "http://www.onvif.org/ver10/topics",
                                Topic = "RuleEngine/CellMotionDetector/Motion",
                                Created = DateTime.UtcNow,
                                Source = new Dictionary<string, string>()
                                {
                                    { "VideoSourceConfigurationToken", "VideoSourceToken" },
                                    { "VideoAnalyticsConfigurationToken", "VideoAnalyticsToken" },
                                    { "Rule", "MyMotionDetectorRule" }
                                },
                                Data = new Dictionary<string, string>()
                                {
                                    { "IsMotion", "true" }
                                }
                            })
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
