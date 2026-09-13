// SharpOnvif
// Copyright (C) 2026 Lukas Volf
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.

using SharpOnvifServer.Dispatch;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpOnvifCommon;
using SharpOnvifServer.Events;
using System;
using System.Xml;
using SharpOnvifCommon.Onvif;

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

        /// <summary>
        /// The shortest subscription this device hands out.
        /// </summary>
        /// <remarks>
        /// Onvif lets the device decide: the client proposes an InitialTerminationTime and the
        /// device answers with the TerminationTime it actually granted. A client asking for a
        /// second gets a subscription that is gone before it can pull from it, so a floor is
        /// applied and reported back.
        /// </remarks>
        private static readonly TimeSpan MinimumSubscriptionLifetime = TimeSpan.FromSeconds(30);

        private static DateTime GrantedTermination(DateTime now, string requested)
        {
            DateTime termination = OnvifHelpers.FromAbsoluteOrRelativeDateTimeUTC(
                now, requested, now.AddMinutes(1));

            DateTime floor = now.Add(MinimumSubscriptionLifetime);
            return termination < floor ? floor : termination;
        }

        #region NotificationProducer

        public override SubscribeResponse Subscribe(SubscribeRequest request)
        {
            Uri endpointUri = OnvifOperationContext.RequestUri;

            string notificationEndpoint = request.ConsumerReference.Address.Value;

            DateTime now = DateTime.UtcNow;
            DateTime termination = GrantedTermination(now, request.InitialTerminationTime);

            // Basic uses the notification endpoint from the request
            var subscription = ActivatorUtilities.CreateInstance<SubscriptionManagerImpl>(
                _serviceProvider, termination, termination.Subtract(now), notificationEndpoint,
                TopicFilter.FromFilter(request.Filter));
            string subscriptionID = _eventSubscriptionManager.AddSubscription(subscription);
            string subscriptionReferenceUri = OnvifHelpers.ChangeUriPath(endpointUri, $"/onvif/Events/Subscription/{subscriptionID}/").ToString();

            _logger.LogDebug($"{nameof(EventsImpl)}: Subscribed Basic {subscriptionID} on {subscriptionReferenceUri}");

            return new SubscribeResponse()
            {
                SubscriptionReference = new EndpointReferenceType()
                {
                    Address = new AttributedURIType()
                    {
                        Value = subscriptionReferenceUri
                    }
                },
                CurrentTime = now,
                CurrentTimeSpecified = true,
                TerminationTime = termination,
                TerminationTimeSpecified = true
            };
        }

        #endregion // NotificationProducer

        #region EventPort

        public override CreatePullPointSubscriptionResponse CreatePullPointSubscription(CreatePullPointSubscriptionRequest request)
        {
            Uri endpointUri = OnvifOperationContext.RequestUri;

            DateTime now = DateTime.UtcNow;
            DateTime termination = GrantedTermination(now, request.InitialTerminationTime);

            // PullPoint uses "" for the notification endpoint
            var subscription = ActivatorUtilities.CreateInstance<SubscriptionManagerImpl>(
                _serviceProvider, termination, termination.Subtract(now), "",
                TopicFilter.FromFilter(request.Filter));
            string subscriptionID = _eventSubscriptionManager.AddSubscription(subscription);
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
