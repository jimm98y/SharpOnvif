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
using SharpOnvifCommon.Onvif;
using SharpOnvifServer.Events;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// Which notifications a subscriber actually asked for. A device that ignores the filter sends
    /// a client watching for motion everything else it produces as well.
    /// </summary>
    [TestClass]
    public sealed class TestTopicFilter
    {
        private const string Topics = "http://www.onvif.org/ver10/topics";
        private const string Motion = "RuleEngine/CellMotionDetector/Motion";

        /// <summary>The Filter a client sends, with the topic expression written as it would be.</summary>
        private static FilterType Filter(string expression, string dialect = OnvifEvents.TopicDialectConcreteSet)
        {
            var dom = new XmlDocument();
            dom.LoadXml(
                "<wsnt:Filter xmlns:wsnt=\"http://docs.oasis-open.org/wsn/b-2\" " +
                $"xmlns:tns1=\"{Topics}\">" +
                (dialect == null
                    ? $"<wsnt:TopicExpression>{expression}</wsnt:TopicExpression>"
                    : $"<wsnt:TopicExpression Dialect=\"{dialect}\">{expression}</wsnt:TopicExpression>") +
                "</wsnt:Filter>");

            var any = new XmlElement[dom.DocumentElement.ChildNodes.Count];
            for (int i = 0; i < any.Length; i++) any[i] = (XmlElement)dom.DocumentElement.ChildNodes[i];

            return new FilterType { Any = any };
        }

        [TestMethod]
        public void AsksForEverythingWhenNothingWasAsked()
        {
            Assert.IsTrue(TopicFilter.FromFilter(null).IsMatchAll);
            Assert.IsTrue(TopicFilter.FromFilter(new FilterType()).IsMatchAll);
            Assert.IsTrue(TopicFilter.FromFilter(new FilterType { Any = new XmlElement[0] }).IsMatchAll);

            Assert.IsTrue(TopicFilter.MatchAll.Matches(Topics, Motion));
        }

        [TestMethod]
        public void DeliversOnlyTheTopicThatWasAskedFor()
        {
            TopicFilter filter = TopicFilter.FromFilter(Filter("tns1:" + Motion));

            Assert.IsFalse(filter.IsMatchAll);
            Assert.IsTrue(filter.Matches(Topics, Motion));
            Assert.IsFalse(filter.Matches(Topics, "Device/HardwareFailure/FanFailure"),
                "a subscriber watching for motion is not asking about the fan");
            Assert.IsFalse(filter.Matches(Topics, "RuleEngine/CellMotionDetector"),
                "the parent of the topic is not the topic");
            Assert.IsFalse(filter.Matches(Topics, Motion + "/Extra"),
                "a child of the topic is not the topic either");
        }

        [TestMethod]
        public void DeliversASubtreeWhenAsked()
        {
            // What a client watching a whole branch sends.
            TopicFilter filter = TopicFilter.FromFilter(Filter("tns1:RuleEngine//."));

            Assert.IsTrue(filter.Matches(Topics, "RuleEngine"));
            Assert.IsTrue(filter.Matches(Topics, Motion));
            Assert.IsTrue(filter.Matches(Topics, "RuleEngine/TamperDetector/Tamper"));
            Assert.IsFalse(filter.Matches(Topics, "Device/HardwareFailure/FanFailure"));
        }

        [TestMethod]
        public void TakesAnyOneLevelForAStar()
        {
            TopicFilter filter = TopicFilter.FromFilter(Filter("tns1:RuleEngine/*/Motion"));

            Assert.IsTrue(filter.Matches(Topics, Motion));
            Assert.IsTrue(filter.Matches(Topics, "RuleEngine/MyOwnDetector/Motion"));
            Assert.IsFalse(filter.Matches(Topics, "RuleEngine/Motion"), "a star stands for one level, not none");
            Assert.IsFalse(filter.Matches(Topics, "RuleEngine/A/B/Motion"), "nor for two");
        }

        [TestMethod]
        public void DeliversAnyOfTheTopicsInASet()
        {
            TopicFilter filter = TopicFilter.FromFilter(
                Filter("tns1:" + Motion + "|tns1:Device/HardwareFailure/FanFailure"));

            Assert.IsTrue(filter.Matches(Topics, Motion));
            Assert.IsTrue(filter.Matches(Topics, "Device/HardwareFailure/FanFailure"));
            Assert.IsFalse(filter.Matches(Topics, "Device/Trigger/Relay"));
        }

        [TestMethod]
        public void ReadsTheExpressionAcrossLines()
        {
            // Clients do send it indented, because it sits inside a pretty-printed envelope.
            TopicFilter filter = TopicFilter.FromFilter(Filter("\n        tns1:" + Motion + "\n      "));

            Assert.IsTrue(filter.Matches(Topics, Motion));
        }

        [TestMethod]
        public void HoldsThePrefixToTheNamespaceItWasWrittenWith()
        {
            TopicFilter filter = TopicFilter.FromFilter(Filter("tns1:" + Motion));

            Assert.IsFalse(filter.Matches("http://example.com/topics", Motion),
                "the same path in another namespace is another topic");
        }

        [TestMethod]
        public void SendsEverythingWhenItCannotReadTheExpression()
        {
            // A dialect we cannot evaluate. Sending too much leaves a subscriber working; sending
            // nothing looks exactly like a device with no events.
            TopicFilter filter = TopicFilter.FromFilter(
                Filter("boolean(ancestor-or-self::tns1:RuleEngine)", "http://docs.oasis-open.org/wsn/t-1/TopicExpression/XPath"));

            Assert.IsTrue(filter.IsMatchAll);
            Assert.IsTrue(filter.Matches(Topics, "anything/at/all"));
        }

        [TestMethod]
        public void MatchesTheMessageADeviceIsAboutToSend()
        {
            TopicFilter filter = TopicFilter.FromFilter(Filter("tns1:RuleEngine//."));

            Assert.IsTrue(filter.Matches(new NotificationMessage { Topic = Motion }));
            Assert.IsFalse(filter.Matches(new NotificationMessage { Topic = "Device/Trigger/Relay" }));
            Assert.IsFalse(filter.Matches(null));
        }
    }
}
