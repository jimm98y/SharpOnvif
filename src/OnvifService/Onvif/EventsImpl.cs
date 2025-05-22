using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using SharpOnvifCommon;
using SharpOnvifServer;
using SharpOnvifServer.Events;
using System;
using System.Xml;

namespace OnvifService.Onvif
{
    public class EventsImpl : EventsBase
    {
        private readonly IServer _server;
        private readonly ILogger<EventsImpl> _logger;

        public EventsImpl(IServer server, ILogger<EventsImpl> logger)
        {
            _server = server;
            _logger = logger;
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
