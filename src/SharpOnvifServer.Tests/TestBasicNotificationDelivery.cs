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

using SharpOnvifCommon.Onvif;
using SharpOnvifCommon.Soap;
using SharpOnvifCommon.Xml;
using SharpOnvifServer.Events;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// A Basic subscription is a device posting a wsnt:Notify to an address the subscriber gave
    /// it, so the two halves of this library have to agree on what that message looks like: the
    /// server writes one, the client reads one, and nothing in between checks them against each
    /// other.
    /// <para>
    /// This pins the writing the sample device does - NotifyRequest into a Notify body, written
    /// with the generated writer - against the client that receives it. It cannot see the sample's
    /// own copy of those lines, which is not referenced from here; what it says is that the pieces
    /// they are built from fit together.
    /// </para>
    /// <para>
    /// Only the envelope assertion tells the fixed message from the one that came before it. The
    /// sample used to send a bare NotificationMessageHolderType, which is not a SOAP message and
    /// names no action, and this client's reader is lenient enough to have found the event in it
    /// regardless - so reading it back proves rather less than it appears to, and the shape is
    /// asserted separately for the consumers that are not this one.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestBasicNotificationDelivery
    {
        private const string WsBaseNotification = "http://docs.oasis-open.org/wsn/b-2";
        // The bare path: the prefix comes from the message's own TopicNamespacePrefix.
        private const string MotionTopic = "RuleEngine/CellMotionDetector/Motion";

        private static NotificationMessage Motion(bool moving)
        {
            var message = new NotificationMessage { Topic = MotionTopic };
            message.Source.Add("VideoSourceConfigurationToken", "VideoSourceToken");
            message.Data.Add("IsMotion", moving ? "true" : "false");
            return message;
        }

        /// <summary>The envelope a Basic subscription puts on the wire.</summary>
        private static string Notify(NotificationMessage message)
        {
            var notify = new NotifyRequest(
                new[]
                {
                    new NotificationMessageHolderType
                    {
                        Topic = new TopicExpressionType
                        {
                            Dialect = OnvifEvents.TopicDialectConcreteSet,
                            Any = OnvifEvents.CreateTopicContent(message),
                        },
                        Message = OnvifEvents.CreateMessageElement(message),
                    },
                },
                null);

            return SoapMessageCodec.Instance.WriteEnvelope(
                OnvifXmlNamespaces.EnvelopePrologue,
                null,
                writer =>
                {
                    writer.WriteStartElement(WsBaseNotification, "Notify");
                    writer.WriteContent(notify);
                    writer.WriteEndElement();
                });
        }

        [TestMethod]
        public void SendsAMotionEventTheClientReads()
        {
            Assert.AreEqual(true, SharpOnvifClient.OnvifEvents.IsMotionDetected(Notify(Motion(true))));
            Assert.AreEqual(false, SharpOnvifClient.OnvifEvents.IsMotionDetected(Notify(Motion(false))));
        }

        [TestMethod]
        public void PutsTheMessageWhereAConsumerLooksForIt()
        {
            string envelope = Notify(Motion(true));

            // Written with the wsnt namespace as the default rather than behind a prefix, which
            // is a choice of the writer's and means the same thing to a reader.
            StringAssert.Contains(envelope, "<Notify xmlns=\"" + WsBaseNotification + "\">",
                "the wsnt:Notify wrapper is missing");
            StringAssert.Contains(envelope, "<NotificationMessage>",
                "the notification is not inside a NotificationMessage");
            StringAssert.Contains(envelope, MotionTopic, "the topic did not reach the wire");
            StringAssert.Contains(envelope, "IsMotion", "the payload did not reach the wire");
        }

        [TestMethod]
        public void SaysNothingAboutWhatItDidNotReport()
        {
            // The reader answers from the topic as well as the payload, so a motion notification
            // has to leave a client asking about tampering with no answer rather than a wrong one.
            Assert.IsNull(SharpOnvifClient.OnvifEvents.IsTamperDetected(Notify(Motion(true))));
        }
    }
}
