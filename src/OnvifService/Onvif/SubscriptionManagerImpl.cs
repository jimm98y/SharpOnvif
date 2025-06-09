using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SubscriptionManagerImpl> _logger;

        public SubscriptionManagerImpl(IServer server, ILogger<SubscriptionManagerImpl> logger)
        {
            _server = server;
            _logger = logger;
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

        public override UnsubscribeResponse1 Unsubscribe(UnsubscribeRequest request)
        {
            return new UnsubscribeResponse1()
            {
                UnsubscribeResponse = new UnsubscribeResponse()
            };
        }
    }
}
