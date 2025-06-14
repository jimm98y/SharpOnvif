using SharpOnvifCommon;
using System;
using System.Collections.Generic;
using System.Xml;

namespace SharpOnvifServer.Events
{
    public class NotificationMessage
    {
        public Dictionary<string, string> Source { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();

        public string TopicNamespacePrefix { get; set; } = "tns1";
        public string TopicNamespace { get; set; } = "http://www.onvif.org/ver10/topics";
        public string Topic { get; set; } = "RuleEngine/CellMotionDetector/Motion";

        public DateTime Created { get; set; } = DateTime.UtcNow;
    }

    public static class OnvifEvents
    {
        public const string ONVIF_SUBSCRIPTION_ID = "OnvifSubscriptionID";

        /*
        <wsnt:NotificationMessage>
            <wsnt:Topic Dialect="http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet">
                tns1:RuleEngine/CellMotionDetector/Motion
            </wsnt:Topic>
            <wsnt:Message>
                <tt:Message UtcTime="2024-05-25T23:59:38Z" PropertyOperation="Initialized">
                    <tt:Source>
                        <tt:SimpleItem Name="VideoSourceConfigurationToken" Value="VideoSourceToken"/>
                        <tt:SimpleItem Name="VideoAnalyticsConfigurationToken" Value="VideoAnalyticsToken"/>
                        <tt:SimpleItem Name="Rule" Value="MyMotionDetectorRule"/>
                    </tt:Source>
                    <tt:Data>
                        <tt:SimpleItem Name="IsMotion" Value="false"/>
                    </tt:Data>
                </tt:Message>
            </wsnt:Message>
        </wsnt:NotificationMessage>
        */

        public static XmlElement[] CreateNotificationMessage(NotificationMessage message)
        {
            return new XmlElement[] { CreateTopicNode(message), CreateMessageNode(message) };
        }

        private static XmlElement CreateTopicNode(NotificationMessage message)
        {
            XmlDocument dom = new XmlDocument();
            const string ns = "http://docs.oasis-open.org/wsn/b-2";

            XmlElement topicNode = dom.CreateElement("Topic", ns);
            topicNode.Attributes.Append(CreateAttribute(dom, "Dialect", "http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet"));

            // put the prefix into the element, we cannot modify the Soap envelope from here
            topicNode.SetAttribute($"xmlns:{message.TopicNamespacePrefix}", message.TopicNamespace);

            var xmlTextNode = dom.CreateTextNode($"{message.TopicNamespacePrefix}:{message.Topic}");
            topicNode.AppendChild(xmlTextNode);

            return topicNode;
        }

        private static XmlElement CreateMessageNode(NotificationMessage message)
        {
            XmlDocument dom = new XmlDocument();

            const string rootNs = "http://docs.oasis-open.org/wsn/b-2";
            XmlElement rootMessageNode = dom.CreateElement("Message", rootNs);

            const string ns = "http://www.onvif.org/ver10/schema";

            XmlElement messageNode = dom.CreateElement("Message", ns);
            messageNode.Attributes.Append(CreateAttribute(dom, "UtcTime", OnvifHelpers.DateTimeToString(message.Created)));
            messageNode.Attributes.Append(CreateAttribute(dom, "PropertyOperation", "Initialized"));

            XmlElement source = dom.CreateElement("Source", ns);

            foreach (var sourceItem in message.Source)
            {
                XmlElement simpleItem = dom.CreateElement("SimpleItem", ns);
                simpleItem.Attributes.Append(CreateAttribute(dom, "Name", sourceItem.Key));
                simpleItem.Attributes.Append(CreateAttribute(dom, "Value", sourceItem.Value));
                source.AppendChild(simpleItem);
            }

            messageNode.AppendChild(source);

            XmlElement data = dom.CreateElement("Data", ns);

            foreach (var dataItem in message.Data)
            {
                XmlElement simpleItem = dom.CreateElement("SimpleItem", ns);
                simpleItem.Attributes.Append(CreateAttribute(dom, "Name", dataItem.Key));
                simpleItem.Attributes.Append(CreateAttribute(dom, "Value", dataItem.Value));
                data.AppendChild(simpleItem);
            }

            messageNode.AppendChild(data);
            rootMessageNode.AppendChild(messageNode);

            return rootMessageNode;
        }

        private static XmlAttribute CreateAttribute(XmlDocument dom, string name, string value)
        {
            var attr = dom.CreateAttribute(name);
            attr.Value = value;
            return attr;
        }
    }
}
