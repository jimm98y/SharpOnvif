using SharpOnvifCommon;
using System;
using System.Collections.Generic;
using System.Xml;

namespace SharpOnvifServer
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

/*
<?xml version="1.0" encoding="UTF-8"?>
<env:Envelope xmlns:env="http://www.w3.org/2003/05/soap-envelope" xmlns:soapenc="http://www.w3.org/2003/05/soap-encoding" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:tt="http://www.onvif.org/ver10/schema" xmlns:tds="http://www.onvif.org/ver10/device/wsdl" xmlns:trt="http://www.onvif.org/ver10/media/wsdl" xmlns:timg="http://www.onvif.org/ver20/imaging/wsdl" xmlns:tev="http://www.onvif.org/ver10/events/wsdl" xmlns:tptz="http://www.onvif.org/ver20/ptz/wsdl" xmlns:tan="http://www.onvif.org/ver20/analytics/wsdl" xmlns:tst="http://www.onvif.org/ver10/storage/wsdl" xmlns:ter="http://www.onvif.org/ver10/error" xmlns:dn="http://www.onvif.org/ver10/network/wsdl" xmlns:tns1="http://www.onvif.org/ver10/topics" xmlns:tmd="http://www.onvif.org/ver10/deviceIO/wsdl" xmlns:wsdl="http://schemas.xmlsoap.org/wsdl" xmlns:wsoap12="http://schemas.xmlsoap.org/wsdl/soap12" xmlns:http="http://schemas.xmlsoap.org/wsdl/http" xmlns:d="http://schemas.xmlsoap.org/ws/2005/04/discovery" xmlns:wsadis="http://schemas.xmlsoap.org/ws/2004/08/addressing" xmlns:wsnt="http://docs.oasis-open.org/wsn/b-2" xmlns:wsa="http://www.w3.org/2005/08/addressing" xmlns:wstop="http://docs.oasis-open.org/wsn/t-1" xmlns:wsrf-bf="http://docs.oasis-open.org/wsrf/bf-2" xmlns:wsntw="http://docs.oasis-open.org/wsn/bw-2" xmlns:wsrf-rw="http://docs.oasis-open.org/wsrf/rw-2" xmlns:wsaw="http://www.w3.org/2006/05/addressing/wsdl" xmlns:wsrf-r="http://docs.oasis-open.org/wsrf/r-2" xmlns:trc="http://www.onvif.org/ver10/recording/wsdl" xmlns:tse="http://www.onvif.org/ver10/search/wsdl" xmlns:trp="http://www.onvif.org/ver10/replay/wsdl" xmlns:tnshik="http://www.hikvision.com/2011/event/topics" xmlns:hikwsd="http://www.onvifext.com/onvif/ext/ver10/wsdl" xmlns:hikxsd="http://www.onvifext.com/onvif/ext/ver10/schema" xmlns:tas="http://www.onvif.org/ver10/advancedsecurity/wsdl" xmlns:tr2="http://www.onvif.org/ver20/media/wsdl" xmlns:axt="http://www.onvif.org/ver20/analytics"><env:Header><wsa:Action>http://www.onvif.org/ver10/events/wsdl/PullPointSubscription/PullMessagesResponse</wsa:Action>
</env:Header>
<env:Body><tev:PullMessagesResponse><tev:CurrentTime>2024-05-25T23:59:39Z</tev:CurrentTime>
<tev:TerminationTime>2024-05-26T00:04:38Z</tev:TerminationTime>
<wsnt:NotificationMessage><wsnt:Topic Dialect="http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet">tns1:VideoSource/MotionAlarm</wsnt:Topic>
<wsnt:Message><tt:Message UtcTime="2024-05-25T23:59:38Z" PropertyOperation="Initialized"><tt:Source><tt:SimpleItem Name="Source" Value="VideoSource_1"/>
</tt:Source>
<tt:Data><tt:SimpleItem Name="State" Value="false"/>
</tt:Data>
</tt:Message>
</wsnt:Message>
</wsnt:NotificationMessage>
<wsnt:NotificationMessage><wsnt:Topic Dialect="http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet">tns1:VideoSource/GlobalSceneChange/ImagingService</wsnt:Topic>
<wsnt:Message><tt:Message UtcTime="2024-05-25T23:59:38Z" PropertyOperation="Initialized"><tt:Source><tt:SimpleItem Name="Source" Value="VideoSource_1"/>
</tt:Source>
<tt:Data><tt:SimpleItem Name="State" Value="false"/>
</tt:Data>
</tt:Message>
</wsnt:Message>
</wsnt:NotificationMessage>
<wsnt:NotificationMessage><wsnt:Topic Dialect="http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet">tns1:RuleEngine/CellMotionDetector/Motion</wsnt:Topic>
<wsnt:Message><tt:Message UtcTime="2024-05-25T23:59:38Z" PropertyOperation="Initialized"><tt:Source><tt:SimpleItem Name="VideoSourceConfigurationToken" Value="VideoSourceToken"/>
<tt:SimpleItem Name="VideoAnalyticsConfigurationToken" Value="VideoAnalyticsToken"/>
<tt:SimpleItem Name="Rule" Value="MyMotionDetectorRule"/>
</tt:Source>
<tt:Data><tt:SimpleItem Name="IsMotion" Value="false"/>
</tt:Data>
</tt:Message>
</wsnt:Message>
</wsnt:NotificationMessage>
<wsnt:NotificationMessage><wsnt:Topic Dialect="http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet">tns1:VideoSource/ImageTooDark/ImagingService</wsnt:Topic>
<wsnt:Message><tt:Message UtcTime="2024-05-25T23:59:38Z" PropertyOperation="Initialized"><tt:Source><tt:SimpleItem Name="Source" Value="VideoSourceToken"/>
</tt:Source>
<tt:Data><tt:SimpleItem Name="State" Value="false"/>
</tt:Data>
</tt:Message>
</wsnt:Message>
</wsnt:NotificationMessage>
<wsnt:NotificationMessage><wsnt:Topic Dialect="http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet">tns1:RuleEngine/FieldDetector/ObjectsInside</wsnt:Topic>
<wsnt:Message><tt:Message UtcTime="2024-05-25T23:59:38Z" PropertyOperation="Initialized"><tt:Source><tt:SimpleItem Name="VideoSourceConfigurationToken" Value="VideoSourceToken"/>
<tt:SimpleItem Name="VideoAnalyticsConfigurationToken" Value="VideoAnalyticsToken"/>
<tt:SimpleItem Name="Rule" Value="MyFieldDetector1"/>
</tt:Source>
<tt:Key><tt:SimpleItem Name="ObjectId" Value="0"/>
</tt:Key>
<tt:Data><tt:SimpleItem Name="IsInside" Value="false"/>
</tt:Data>
</tt:Message>
</wsnt:Message>
</wsnt:NotificationMessage>
<wsnt:NotificationMessage><wsnt:Topic Dialect="http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet">tns1:Monitoring/OperatingTime/LastReset</wsnt:Topic>
<wsnt:Message><tt:Message UtcTime="2024-05-25T23:59:38Z" PropertyOperation="Initialized"><tt:Data><tt:SimpleItem Name="Status" Value="2022-10-13T06:47:30Z"/>
</tt:Data>
</tt:Message>
</wsnt:Message>
</wsnt:NotificationMessage>
<wsnt:NotificationMessage><wsnt:Topic Dialect="http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet">tns1:Monitoring/OperatingTime/LastReboot</wsnt:Topic>
<wsnt:Message><tt:Message UtcTime="2024-05-25T23:59:38Z" PropertyOperation="Initialized"><tt:Data><tt:SimpleItem Name="Status" Value="2022-10-13T06:47:30Z"/>
</tt:Data>
</tt:Message>
</wsnt:Message>
</wsnt:NotificationMessage>
<wsnt:NotificationMessage><wsnt:Topic Dialect="http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet">tns1:Monitoring/OperatingTime/LastClockSynchronization</wsnt:Topic>
<wsnt:Message><tt:Message UtcTime="2024-05-25T23:59:38Z" PropertyOperation="Initialized"><tt:Data><tt:SimpleItem Name="Status" Value="2022-10-13T06:47:30Z"/>
</tt:Data>
</tt:Message>
</wsnt:Message>
</wsnt:NotificationMessage>
<wsnt:NotificationMessage><wsnt:Topic Dialect="http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet">tns1:Device/HardwareFailure/StorageFailure</wsnt:Topic>
<wsnt:Message><tt:Message UtcTime="2024-05-25T23:59:38Z" PropertyOperation="Initialized"><tt:Source><tt:SimpleItem Name="Token" Value=""/>
</tt:Source>
<tt:Data><tt:SimpleItem Name="Failed" Value="false"/>
</tt:Data>
</tt:Message>
</wsnt:Message>
</wsnt:NotificationMessage>
<wsnt:NotificationMessage><wsnt:Topic Dialect="http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet">tns1:Monitoring/ProcessorUsage</wsnt:Topic>
<wsnt:Message><tt:Message UtcTime="2024-05-25T23:59:38Z" PropertyOperation="Initialized"><tt:Source><tt:SimpleItem Name="Token" Value="Processor_Usage"/>
</tt:Source>
<tt:Data><tt:SimpleItem Name="Value" Value="37"/>
</tt:Data>
</tt:Message>
</wsnt:Message>
</wsnt:NotificationMessage>
</tev:PullMessagesResponse>
</env:Body>
</env:Envelope>
*/