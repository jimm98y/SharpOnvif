using CoreWCF;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpOnvifCommon;
using SharpOnvifServer.Events;
using System;
using System.Xml;

namespace OnvifService.Onvif
{
    public class EventsImpl : EventsBase
    {
        private readonly IServer _server;
        private readonly ILogger<EventsImpl> _logger;
        private readonly IEventSubscriptionManager<SubscriptionManagerImpl> _eventSubscriptionManager;
        private readonly IServiceProvider _serviceProvider;

        public EventsImpl(IServer server, ILogger<EventsImpl> logger, IEventSubscriptionManager<SubscriptionManagerImpl> eventSubscriptionManager, IServiceProvider serviceProvider)
        {
            _server = server;
            _logger = logger;
            _eventSubscriptionManager = eventSubscriptionManager;
            _serviceProvider = serviceProvider;
        }

        #region NotificationProducer

        public override SubscribeResponse1 Subscribe(SubscribeRequest request)
        {
            Uri endpointUri = OperationContext.Current.IncomingMessageProperties.Via;

            string notificationEndpoint = request.Subscribe.ConsumerReference.Address.Value;

            DateTime now = DateTime.UtcNow;
            DateTime termination = OnvifHelpers.FromAbsoluteOrRelativeDateTimeUTC(now, request.Subscribe.InitialTerminationTime, now.AddMinutes(1));

            // Basic uses the notification endpoint from the request
            var subscription = ActivatorUtilities.CreateInstance<SubscriptionManagerImpl>(_serviceProvider, termination, termination.Subtract(now), notificationEndpoint);
            int subscriptionID = _eventSubscriptionManager.AddSubscription(subscription);
            string subscriptionReferenceUri = OnvifHelpers.ChangeUriPath(endpointUri, $"/onvif/Events/Subscription/{subscriptionID}/").ToString();

            _logger.LogDebug($"{nameof(EventsImpl)}: Subscribed Basic {subscriptionID} on {subscriptionReferenceUri}");

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
            Uri endpointUri = OperationContext.Current.IncomingMessageProperties.Via;

            DateTime now = DateTime.UtcNow;
            DateTime termination = OnvifHelpers.FromAbsoluteOrRelativeDateTimeUTC(now, request.InitialTerminationTime, now.AddMinutes(1));

            // PullPoint uses "" for the notification endpoint
            var subscription = ActivatorUtilities.CreateInstance<SubscriptionManagerImpl>(_serviceProvider, termination, termination.Subtract(now), "");
            int subscriptionID = _eventSubscriptionManager.AddSubscription(subscription);
            string subscriptionReferenceUri = OnvifHelpers.ChangeUriPath(endpointUri, $"/onvif/Events/PullPointSubscription/{subscriptionID}/").ToString();

            _logger.LogDebug($"{nameof(EventsImpl)}: Subscribed PullPoint {subscriptionID} on {subscriptionReferenceUri}");

            return new CreatePullPointSubscriptionResponse()
            {
                CurrentTime = now,
                TerminationTime = termination,
                SubscriptionReference = new EndpointReferenceType()
                {
                    Address = new AttributedURIType() 
                    {
                        Value = subscriptionReferenceUri
                    }
                }
            };
        }

        public override GetEventPropertiesResponse GetEventProperties(GetEventPropertiesRequest request)
        {
            return new GetEventPropertiesResponse()
            {
                FixedTopicSet = true,
                TopicSet = new TopicSetType()
                {
                    Any = CreateEventProperties()
                },
                TopicExpressionDialect = new string[]
                {
                    "http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet",
                    "http://docs.oasis-open.org/wsn/t-1/TopicExpression/Concrete"
                },
                MessageContentFilterDialect = new string[]
                {
                    "http://www.onvif.org/ver10/tev/messageContentFilter/ItemFilter"
                },
                MessageContentSchemaLocation = new string[]
                {
                    "http://www.onvif.org/onvif/ver10/schema/onvif.xsd"
                }
            };
        }

        private XmlElement[] CreateEventProperties()
        {
            string xml =
                "<tns1:RuleEngine " +
                "   xmlns:tns1=\"http://www.onvif.org/ver10/topics\" " +
                "   xmlns:tt=\"http://www.onvif.org/ver10/schema\" " +
                "   xmlns:wstop=\"http://docs.oasis-open.org/wsn/t-1\" " +
                "   wstop:topic=\"false\">\r\n" +
                "  <CellMotionDetector wstop:topic=\"false\">\r\n" +
                "    <Motion wstop:topic=\"true\">\r\n" +
                "      <tt:MessageDescription IsProperty=\"true\">\r\n" +
                "        <tt:Source>\r\n" +
                "          <tt:SimpleItemDescription Name=\"VideoSourceConfigurationToken\" Type=\"tt:ReferenceToken\"/>\r\n" +
                "          <tt:SimpleItemDescription Name=\"VideoAnalyticsConfigurationToken\" Type=\"tt:ReferenceToken\"/>\r\n" +
                "          <tt:SimpleItemDescription Name=\"Rule\" Type=\"xs:string\"/>\r\n" +
                "        </tt:Source>\r\n" +
                "        <tt:Data>\r\n" +
                "          <tt:SimpleItemDescription Name=\"IsMotion\" Type=\"xs:boolean\"/>\r\n" +
                "        </tt:Data>\r\n" +
                "      </tt:MessageDescription>\r\n" +
                "    </Motion>\r\n" +
                "  </CellMotionDetector>" +
                "</tns1:RuleEngine>\r\n";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return new XmlElement[] { (XmlElement)doc.FirstChild };
        }

        #endregion // EventPort
    }
}
