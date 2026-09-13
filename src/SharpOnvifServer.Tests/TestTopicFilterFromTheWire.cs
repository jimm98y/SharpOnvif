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

using System.IO;
using System.Xml;
using SharpOnvifCommon.Xml;
using SharpOnvifServer.Events;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// A topic filter as it actually arrives: read off the wire by the generated reader, rather
    /// than built in a test.
    /// <para>
    /// The root of a topic path is a QName, so reading it needs the namespace the prefix was
    /// declared with - and that declaration is usually on the envelope, far above the element the
    /// expression sits in. A reader that hands back an element detached from its declarations
    /// loses it, and every filter then quietly matches topics in namespaces the subscriber never
    /// asked about. Nothing in a unit test that builds the filter by hand would show that.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestTopicFilterFromTheWire
    {
        private const string Topics = "http://www.onvif.org/ver10/topics";
        private const string Motion = "RuleEngine/CellMotionDetector/Motion";

        /// <summary>What a client sends to subscribe, with the prefixes declared on the envelope.</summary>
        private const string SubscribeEnvelope =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
            "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" " +
                        "xmlns:wsnt=\"http://docs.oasis-open.org/wsn/b-2\" " +
                        "xmlns:tns1=\"http://www.onvif.org/ver10/topics\">" +
              "<s:Body>" +
                "<CreatePullPointSubscription xmlns=\"http://www.onvif.org/ver10/events/wsdl\">" +
                  "<Filter>" +
                    "<wsnt:TopicExpression Dialect=\"http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet\">" +
                      "tns1:RuleEngine//." +
                    "</wsnt:TopicExpression>" +
                  "</Filter>" +
                  "<InitialTerminationTime>PT60S</InitialTerminationTime>" +
                "</CreatePullPointSubscription>" +
              "</s:Body>" +
            "</s:Envelope>";

        private static SharpOnvifServer.Events.CreatePullPointSubscriptionRequest ReadRequest(string envelope)
        {
            using (XmlReader xml = SoapEnvelope.CreateReader(new StringReader(envelope)))
            {
                Assert.IsTrue(SoapEnvelope.MoveToBody(xml), "the envelope has a body");

                return new OnvifXmlReader(xml).ReadElementObject(
                    () => new SharpOnvifServer.Events.CreatePullPointSubscriptionRequest());
            }
        }

        [TestMethod]
        public void ResolvesAPrefixDeclaredOnTheEnvelope()
        {
            var request = ReadRequest(SubscribeEnvelope);

            Assert.IsNotNull(request.Filter, "the filter has to survive being read");
            Assert.AreEqual("PT60S", request.InitialTerminationTime);

            TopicFilter filter = TopicFilter.FromFilter(request.Filter);

            Assert.IsFalse(filter.IsMatchAll, "the subscriber asked for a subtree, not for everything");
            Assert.IsTrue(filter.Matches(Topics, Motion));
            Assert.IsTrue(filter.Matches(Topics, "RuleEngine/TamperDetector/Tamper"));
            Assert.IsFalse(filter.Matches(Topics, "Device/HardwareFailure/FanFailure"),
                "the subscriber asked about the rule engine only");

            // The part that only the real reader can prove: tns1 was declared three elements
            // above the expression, and the namespace still has to be known.
            Assert.IsFalse(filter.Matches("http://example.com/topics", Motion),
                "the prefix resolved to nothing, so every namespace matches");
        }

        [TestMethod]
        public void TakesEveryTopicWhenTheClientNamedNoFilter()
        {
            string envelope = SubscribeEnvelope.Replace(
                "<Filter>" +
                  "<wsnt:TopicExpression Dialect=\"http://www.onvif.org/ver10/tev/topicExpression/ConcreteSet\">" +
                    "tns1:RuleEngine//." +
                  "</wsnt:TopicExpression>" +
                "</Filter>", string.Empty);

            var request = ReadRequest(envelope);

            Assert.IsTrue(TopicFilter.FromFilter(request.Filter).IsMatchAll);
        }
    }
}
