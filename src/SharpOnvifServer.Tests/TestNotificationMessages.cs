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

using System;
using System.Linq;
using System.Xml;
using SharpOnvifServer.Events;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// What a device puts in a notification. Onvif carries most of it as tt:SimpleItem name/value
    /// pairs, but anything with a shape to it - a rectangle, an analytics payload - is a
    /// tt:ElementItem, which a device with no way to write one simply cannot report.
    /// </summary>
    [TestClass]
    public sealed class TestNotificationMessages
    {
        private const string Tt = "http://www.onvif.org/ver10/schema";

        private static XmlElement Rectangle()
        {
            var dom = new XmlDocument();
            XmlElement shape = dom.CreateElement("tt", "Rectangle", Tt);
            shape.SetAttribute("left", "10");
            shape.SetAttribute("top", "20");
            return shape;
        }

        private static XmlElement MessageOf(NotificationMessage message) =>
            OnvifEvents.CreateMessageElement(message);

        private static XmlElement ItemList(XmlElement ttMessage, string name) =>
            ttMessage.ChildNodes.Cast<XmlNode>().OfType<XmlElement>()
                .First(e => e.LocalName == name && e.NamespaceURI == Tt);

        [TestMethod]
        public void WritesTheSimpleItemsItWasGiven()
        {
            XmlElement message = MessageOf(new NotificationMessage
            {
                Source = { { "VideoSourceConfigurationToken", "VideoSourceToken" } },
                Data = { { "IsMotion", "true" } },
            });

            XmlElement item = (XmlElement)ItemList(message, "Data").FirstChild;
            Assert.AreEqual("SimpleItem", item.LocalName);
            Assert.AreEqual("IsMotion", item.GetAttribute("Name"));
            Assert.AreEqual("true", item.GetAttribute("Value"));
        }

        [TestMethod]
        public void WritesAnElementItemForAValueWithAShape()
        {
            XmlElement message = MessageOf(new NotificationMessage
            {
                DataElements = { { "Shape", Rectangle() } },
            });

            XmlElement item = (XmlElement)ItemList(message, "Data").FirstChild;
            Assert.AreEqual("ElementItem", item.LocalName);
            Assert.AreEqual(Tt, item.NamespaceURI);
            Assert.AreEqual("Shape", item.GetAttribute("Name"));

            var content = (XmlElement)item.FirstChild;
            Assert.AreEqual("Rectangle", content.LocalName, "the element the caller gave has to be what is carried");
            Assert.AreEqual("10", content.GetAttribute("left"));
        }

        [TestMethod]
        public void PutsEverySimpleItemBeforeAnyElementItem()
        {
            // tt:ItemList declares SimpleItem before ElementItem, so a reader validating against
            // the schema rejects the other order.
            XmlElement message = MessageOf(new NotificationMessage
            {
                Data = { { "IsMotion", "true" }, { "Confidence", "90" } },
                DataElements = { { "Shape", Rectangle() } },
            });

            string[] order = ItemList(message, "Data").ChildNodes
                .Cast<XmlNode>().Select(n => n.LocalName).ToArray();

            CollectionAssert.AreEqual(new[] { "SimpleItem", "SimpleItem", "ElementItem" }, order);
        }

        [TestMethod]
        public void CarriesElementItemsOnTheSourceAsWell()
        {
            XmlElement message = MessageOf(new NotificationMessage
            {
                SourceElements = { { "Region", Rectangle() } },
            });

            XmlElement item = (XmlElement)ItemList(message, "Source").FirstChild;
            Assert.AreEqual("ElementItem", item.LocalName);
            Assert.AreEqual("Region", item.GetAttribute("Name"));
        }

        [TestMethod]
        public void SkipsAnElementItemWithNothingInIt()
        {
            XmlElement message = MessageOf(new NotificationMessage
            {
                Data = { { "IsMotion", "true" } },
                DataElements = { { "Shape", null } },
            });

            Assert.AreEqual(1, ItemList(message, "Data").ChildNodes.Count);
        }

        [TestMethod]
        public void StillWritesTheTopicAndTheTimeAroundThem()
        {
            var created = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var message = new NotificationMessage { Created = created, Data = { { "IsMotion", "true" } } };

            XmlElement tt = MessageOf(message);
            Assert.AreEqual("2026-01-01T12:00:00.000Z", tt.GetAttribute("UtcTime"));
            Assert.AreEqual("Changed", tt.GetAttribute("PropertyOperation"));

            XmlNode[] topic = OnvifEvents.CreateTopicContent(message);
            Assert.AreEqual("tns1:RuleEngine/CellMotionDetector/Motion", topic[0].Value);
        }
    }
}
