using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using SharpOnvifCommon;
using SharpOnvifServer.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnvifService.Onvif
{
    /// <summary>
    /// Onvif Event Subscription.
    /// </summary>
    public class SubscriptionManagerImpl : SubscriptionManagerBase, IEventSubscription
    {
        private readonly IServer _server;
        private readonly ILogger<SubscriptionManagerImpl> _logger;
        private readonly string _notificationEndpoint; // endpoint where to send Basic subscription notifications
        private readonly TimeSpan _expirationDelta;

        /// <summary>
        /// Subscription expiration time in UTC.
        /// </summary>
        public DateTime ExpirationUTC { get; set; }

        public SubscriptionManagerImpl(DateTime expirationUTC, TimeSpan expirationDelta, string notificationEndpoint, IServer server, ILogger<SubscriptionManagerImpl> logger)
        {
            ExpirationUTC = expirationUTC;
            _expirationDelta = expirationDelta;
            _notificationEndpoint = notificationEndpoint; // for Basic subscriptions only
            _server = server;
            _logger = logger;
        }

        public override PullMessagesResponse PullMessages(PullMessagesRequest request)
        {
            if (!string.IsNullOrEmpty(_notificationEndpoint))
                throw new InvalidOperationException($"{nameof(SubscriptionManagerImpl)}: {nameof(PullMessages)} is not supported on Basic event subscription!");

            Task.Delay(5000).Wait(); // forced delay

            DateTime now = DateTime.UtcNow;
            DateTime expiration = now.Add(_expirationDelta); // TODO: review if adding the initial Delta is correct
            
            // extend the expiration of this subscription
            ExpirationUTC = expiration;

            return new PullMessagesResponse()
            {
                CurrentTime = now,
                TerminationTime = expiration,
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
            DateTime now = DateTime.UtcNow;
            DateTime expiration = OnvifHelpers.FromAbsoluteOrRelativeDateTimeUTC(now, request.Renew.TerminationTime, now.AddMinutes(1));

            // extend the expiration of this subscription
            ExpirationUTC = expiration;

            return new RenewResponse1()
            {
                RenewResponse = new RenewResponse()
                {
                    CurrentTime = DateTime.UtcNow,
                    TerminationTime = expiration
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
