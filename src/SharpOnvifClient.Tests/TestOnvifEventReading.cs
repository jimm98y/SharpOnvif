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

using System.Xml;
using SharpOnvifClient;
using SharpOnvifCommon.Onvif;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// Reading what a notification actually says.
    /// <para>
    /// An Onvif event reports its state in a named data item, and carries its source alongside -
    /// which video source, which rule, whether that rule is enabled. Those items have values of
    /// their own, so answering "is there motion" by looking for any true value anywhere in the
    /// message reports motion whenever any of them is true. In a security product that is a false
    /// alarm from a camera that said IsMotion=false.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestOnvifEventReading
    {
        private const string Tt = "http://www.onvif.org/ver10/schema";
        private const string MotionTopic = "tns1:RuleEngine/CellMotionDetector/Motion";

        private static NotificationMessageHolderType Notification(string topic, string source, string data)
        {
            var message = new XmlDocument();
            message.LoadXml(
                $"<tt:Message xmlns:tt=\"{Tt}\" UtcTime=\"2026-01-01T00:00:00Z\" PropertyOperation=\"Changed\">" +
                  $"<tt:Source>{source}</tt:Source>" +
                  $"<tt:Data>{data}</tt:Data>" +
                "</tt:Message>");

            var topics = new XmlDocument();

            return new NotificationMessageHolderType
            {
                Topic = topic == null ? null : new TopicExpressionType
                {
                    Any = new XmlNode[] { topics.CreateTextNode(topic) },
                },
                Message = message.DocumentElement,
            };
        }

        private static string Item(string name, string value) =>
            $"<tt:SimpleItem Name=\"{name}\" Value=\"{value}\"/>";

        [DataRow("true", true, DisplayName = "motion started")]
        [DataRow("false", false, DisplayName = "motion stopped")]
        [DataRow("1", true, DisplayName = "written as 1")]
        [DataRow("0", false, DisplayName = "written as 0")]
        [TestMethod]
        public void ReadsTheStateTheEventReports(string written, bool expected)
        {
            var notification = Notification(MotionTopic, Item("Rule", "MyRule"), Item("IsMotion", written));

            Assert.AreEqual(expected, OnvifEvents.IsMotionDetected(notification));
        }

        [TestMethod]
        public void DoesNotTakeAnythingElseInTheMessageForTheAnswer()
        {
            // The camera said there is no motion. Everything else in the message is beside the
            // point, including the parts of it that are true.
            var notification = Notification(
                MotionTopic,
                Item("VideoSourceConfigurationToken", "VideoSourceToken") + Item("Active", "true"),
                Item("IsMotion", "false"));

            Assert.AreEqual(false, OnvifEvents.IsMotionDetected(notification),
                "a true value elsewhere in the message was taken for motion");
        }

        [TestMethod]
        public void IgnoresANotificationAboutSomethingElse()
        {
            var notification = Notification("tns1:Device/HardwareFailure/FanFailure", "", Item("Failed", "true"));

            Assert.IsNull(OnvifEvents.IsMotionDetected(notification),
                "null says this notification is not about motion, which is not the same as no motion");
            Assert.IsNull(OnvifEvents.IsTamperDetected(notification));
        }

        [TestMethod]
        public void ReadsATopicUnderWhateverPrefixTheDeviceChose()
        {
            // tns1 is a convention, not a rule: a device may bind the topic namespace to anything.
            var notification = Notification(
                "onvif:RuleEngine/CellMotionDetector/Motion", "", Item("IsMotion", "true"));

            Assert.AreEqual(true, OnvifEvents.IsMotionDetected(notification));
        }

        [TestMethod]
        public void SurvivesANotificationThatIsMissingItsParts()
        {
            Assert.IsNull(OnvifEvents.IsMotionDetected((NotificationMessageHolderType)null));
            Assert.IsNull(OnvifEvents.IsMotionDetected(new NotificationMessageHolderType()),
                "a notification with no topic used to throw");
            Assert.IsNull(OnvifEvents.IsMotionDetected(new NotificationMessageHolderType
            {
                Topic = new TopicExpressionType(),
            }));

            var noMessage = Notification(MotionTopic, "", Item("IsMotion", "true"));
            noMessage.Message = null;
            Assert.IsNull(OnvifEvents.IsMotionDetected(noMessage));

            Assert.IsNull(OnvifEvents.IsMotionDetected((string)null));
            Assert.IsNull(OnvifEvents.IsMotionDetected(string.Empty));
        }

        [TestMethod]
        public void ReportsTamperAndSoundFromTheirOwnItems()
        {
            var tamper = Notification("tns1:RuleEngine/TamperDetector/Tamper", "", Item("IsTamper", "true"));
            Assert.AreEqual(true, OnvifEvents.IsTamperDetected(tamper));
            Assert.IsNull(OnvifEvents.IsMotionDetected(tamper));

            var sound = Notification("tns1:AudioAnalytics/Audio/DetectedSound", "", Item("IsSoundDetected", "false"));
            Assert.AreEqual(false, OnvifEvents.IsSoundDetected(sound));
        }

        [TestMethod]
        public void ReadsTheSameThingFromTheXmlABasicSubscriptionDelivers()
        {
            // A Basic subscription arrives as the raw body, which is all the listener has to
            // hand over.
            string body =
                "<?xml version=\"1.0\"?>" +
                "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" " +
                            "xmlns:wsnt=\"http://docs.oasis-open.org/wsn/b-2\" " +
                            $"xmlns:tt=\"{Tt}\" xmlns:tns1=\"http://www.onvif.org/ver10/topics\">" +
                  "<s:Body><wsnt:Notify><wsnt:NotificationMessage>" +
                    $"<wsnt:Topic Dialect=\"d\">{MotionTopic}</wsnt:Topic>" +
                    "<wsnt:Message>" +
                      "<tt:Message UtcTime=\"2026-01-01T00:00:00Z\">" +
                        "<tt:Source>" + Item("Rule", "MyRule") + Item("Active", "true") + "</tt:Source>" +
                        "<tt:Data>" + Item("IsMotion", "false") + "</tt:Data>" +
                      "</tt:Message>" +
                    "</wsnt:Message>" +
                  "</wsnt:NotificationMessage></wsnt:Notify></s:Body>" +
                "</s:Envelope>";

            Assert.AreEqual(false, OnvifEvents.IsMotionDetected(body),
                "a true value elsewhere in the envelope was taken for motion");

            Assert.AreEqual(true, OnvifEvents.IsMotionDetected(body.Replace(
                Item("IsMotion", "false"), Item("IsMotion", "true"))));

            Assert.IsNull(OnvifEvents.IsTamperDetected(body), "the envelope says nothing about tampering");
        }

        [TestMethod]
        public void ReportsTheTopicItRead()
        {
            Assert.AreEqual(MotionTopic,
                OnvifEvents.GetTopic(Notification(MotionTopic, "", Item("IsMotion", "true"))));

            Assert.IsNull(OnvifEvents.GetTopic(new NotificationMessageHolderType()));
            Assert.IsNull(OnvifEvents.GetTopic(null));
        }
    }
}
