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

using SharpOnvifCommon;
using System;
using System.Collections.Generic;
using System.Xml;

namespace SharpOnvifServer.Events
{
    public class NotificationMessage
    {
        /// <summary>What produced the event, as tt:SimpleItem name/value pairs.</summary>
        public Dictionary<string, string> Source { get; set; } = new Dictionary<string, string>();

        /// <summary>What the event says, as tt:SimpleItem name/value pairs.</summary>
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Source items whose value is a whole element rather than a string, written as
        /// tt:ElementItem.
        /// </summary>
        /// <remarks>
        /// A tt:SimpleItem carries a string in an attribute and can say no more than that. Onvif
        /// events that report a shape, a rectangle or an analytics payload use tt:ElementItem
        /// instead, which holds one element of the schema's own choosing.
        /// </remarks>
        public Dictionary<string, XmlElement> SourceElements { get; set; } = new Dictionary<string, XmlElement>();

        /// <summary>Data items whose value is a whole element, written as tt:ElementItem.</summary>
        public Dictionary<string, XmlElement> DataElements { get; set; } = new Dictionary<string, XmlElement>();

        public string TopicNamespacePrefix { get; set; } = "tns1";
        public string TopicNamespace { get; set; } = "http://www.onvif.org/ver10/topics";
        public string Topic { get; set; } = "RuleEngine/CellMotionDetector/Motion";

        public DateTime Created { get; set; } = DateTime.UtcNow;
    }

    public static class OnvifEvents
    {
        /// <summary>
        /// Key under which <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> carries the
        /// subscription an addressed request belongs to. The value is the string the address
        /// ended with.
        /// </summary>
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

        /// <summary>The topic dialect Onvif uses for a concrete topic path.</summary>
        public const string TopicDialectConcreteSet = "http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet";

        /// <summary>
        /// Builds the Topic and Message elements of a notification as raw XML.
        /// </summary>
        public static XmlElement[] CreateNotificationMessage(NotificationMessage message, string propertyOperation = "Changed")
        {
            return new XmlElement[] { CreateTopicNode(message), CreateMessageNode(message, propertyOperation) };
        }

        /// <summary>
        /// The content of a wsnt:Topic element: the topic path written against the prefix the
        /// message names. The prefix is declared on the SOAP envelope.
        /// </summary>
        public static XmlNode[] CreateTopicContent(NotificationMessage message)
        {
            XmlDocument dom = new XmlDocument();
            return new XmlNode[] { dom.CreateTextNode($"{message.TopicNamespacePrefix}:{message.Topic}") };
        }

        /// <summary>
        /// The content of a wsnt:Message element: the tt:Message carrying the event's source and
        /// data items.
        /// </summary>
        public static XmlElement CreateMessageElement(NotificationMessage message, string propertyOperation = "Changed")
        {
            XmlElement wrapper = CreateMessageNode(message, propertyOperation);

            // CreateMessageNode builds the wsnt:Message wrapper; its single child is the payload.
            return wrapper.FirstChild as XmlElement;
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

        private static XmlElement CreateMessageNode(NotificationMessage message, string propertyOperation)
        {
            XmlDocument dom = new XmlDocument();

            const string rootNs = "http://docs.oasis-open.org/wsn/b-2";
            XmlElement rootMessageNode = dom.CreateElement("Message", rootNs);

            const string ns = "http://www.onvif.org/ver10/schema";

            XmlElement messageNode = dom.CreateElement("tt", "Message", ns);
            messageNode.Attributes.Append(CreateAttribute(dom, "UtcTime", OnvifHelpers.DateTimeToString(message.Created)));

            if (!string.IsNullOrEmpty(propertyOperation))
            {
                messageNode.Attributes.Append(CreateAttribute(dom, "PropertyOperation", propertyOperation));
            }

            messageNode.AppendChild(CreateItemList(dom, ns, "Source", message.Source, message.SourceElements));
            messageNode.AppendChild(CreateItemList(dom, ns, "Data", message.Data, message.DataElements));
            rootMessageNode.AppendChild(messageNode);

            return rootMessageNode;
        }

        /// <summary>
        /// A tt:ItemList - the Source or the Data of a message. The schema puts every SimpleItem
        /// before any ElementItem, so they are written in that order.
        /// </summary>
        private static XmlElement CreateItemList(
            XmlDocument dom,
            string ns,
            string elementName,
            Dictionary<string, string> simpleItems,
            Dictionary<string, XmlElement> elementItems)
        {
            XmlElement list = dom.CreateElement("tt", elementName, ns);

            if (simpleItems != null)
            {
                foreach (var item in simpleItems)
                {
                    XmlElement simpleItem = dom.CreateElement("tt", "SimpleItem", ns);
                    simpleItem.Attributes.Append(CreateAttribute(dom, "Name", item.Key));
                    simpleItem.Attributes.Append(CreateAttribute(dom, "Value", item.Value));
                    list.AppendChild(simpleItem);
                }
            }

            if (elementItems != null)
            {
                foreach (var item in elementItems)
                {
                    if (item.Value == null) continue;

                    XmlElement elementItem = dom.CreateElement("tt", "ElementItem", ns);
                    elementItem.Attributes.Append(CreateAttribute(dom, "Name", item.Key));

                    // The content was built against a document of its own, so it is imported
                    // rather than appended: a node belongs to one document at a time.
                    elementItem.AppendChild(dom.ImportNode(item.Value, deep: true));
                    list.AppendChild(elementItem);
                }
            }

            return list;
        }

        private static XmlAttribute CreateAttribute(XmlDocument dom, string name, string value)
        {
            var attr = dom.CreateAttribute(name);
            attr.Value = value;
            return attr;
        }
    }
}
