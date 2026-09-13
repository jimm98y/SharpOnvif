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
using SharpOnvifCommon.Onvif;
using SharpOnvifCommon.Xml;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// An element the schema did not name, read into the wildcard member a contract keeps for
    /// exactly that, and what happens to the elements after it.
    /// <para>
    /// Reading a wildcard element lifts a whole subtree out of the document, which is the one
    /// place in the reader where more than one node is consumed at a time. Leaving the reader on
    /// the closing tag of what was lifted made the enclosing contract take that tag for its own
    /// and stop: everything after the wildcard was silently dropped, and so was everything after
    /// the contract that contained it.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestReadingWildcardElements
    {
        private const string Tt = "http://www.onvif.org/ver10/schema";

        private static T Read<T>(string xml) where T : OnvifContract, new()
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
            {
                reader.MoveToContent();
                return new OnvifXmlReader(reader).ReadElementObject(() => new T());
            }
        }

        [TestMethod]
        public void KeepsReadingAfterAnElementItDidNotExpect()
        {
            var configuration = Read<VideoEncoderConfiguration>(
                $"<Configuration xmlns=\"{Tt}\">" +
                  "<Name>main</Name>" +
                  "<UseCount>1</UseCount>" +
                  "<Encoding>H264</Encoding>" +
                  "<SomethingTheSchemaDoesNotName><Inner>x</Inner></SomethingTheSchemaDoesNotName>" +
                  "<SessionTimeout>PT60S</SessionTimeout>" +
                "</Configuration>");

            Assert.AreEqual("main", configuration.Name);
            Assert.AreEqual(VideoEncoding.H264, configuration.Encoding);
            Assert.AreEqual("PT60S", configuration.SessionTimeout,
                "the element after the unexpected one was dropped");

            Assert.IsNotNull(configuration.Any);
            Assert.AreEqual(1, configuration.Any.Length);
            Assert.AreEqual("SomethingTheSchemaDoesNotName", configuration.Any[0].LocalName,
                "the unexpected element is kept, not discarded");
            Assert.AreEqual("x", configuration.Any[0].InnerText);
        }

        [TestMethod]
        public void KeepsReadingAfterAnEmptyOne()
        {
            // An empty element has no closing tag of its own, so the reader ends up somewhere
            // different again.
            var configuration = Read<VideoEncoderConfiguration>(
                $"<Configuration xmlns=\"{Tt}\">" +
                  "<Name>main</Name>" +
                  "<Unexpected/>" +
                  "<SessionTimeout>PT60S</SessionTimeout>" +
                "</Configuration>");

            Assert.AreEqual("main", configuration.Name);
            Assert.AreEqual("PT60S", configuration.SessionTimeout);
            Assert.AreEqual(1, configuration.Any.Length);
        }

        [TestMethod]
        public void KeepsReadingAfterSeveralOfThem()
        {
            var configuration = Read<VideoEncoderConfiguration>(
                $"<Configuration xmlns=\"{Tt}\">" +
                  "<Name>main</Name>" +
                  "<First/><Second>2</Second><Third/>" +
                  "<SessionTimeout>PT60S</SessionTimeout>" +
                "</Configuration>");

            Assert.AreEqual("PT60S", configuration.SessionTimeout);
            Assert.AreEqual(3, configuration.Any.Length);
            Assert.AreEqual("First", configuration.Any[0].LocalName);
            Assert.AreEqual("Second", configuration.Any[1].LocalName);
            Assert.AreEqual("Third", configuration.Any[2].LocalName);
        }

        [TestMethod]
        public void ReadsEveryElementFromAReaderThatCannotSayWhereItIs()
        {
            // The reader tells a handler that consumed its element from one that did not by
            // watching where in the document it is, and not every XmlReader can say - an
            // XmlNodeReader over an XmlDocument reports nothing. Taking "no position" for "did not
            // move" skipped the element after every one that was read, which is every second
            // element of every document.
            var dom = new XmlDocument();
            dom.LoadXml(
                $"<Configuration xmlns=\"{Tt}\">" +
                  "<Name>main</Name>" +
                  "<UseCount>3</UseCount>" +
                  "<Encoding>H264</Encoding>" +
                  "<SessionTimeout>PT60S</SessionTimeout>" +
                "</Configuration>");

            using (var nodeReader = new XmlNodeReader(dom))
            {
                Assert.IsFalse(nodeReader is IXmlLineInfo, "this test is pointless if the reader can say");

                nodeReader.MoveToContent();
                var configuration = new OnvifXmlReader(nodeReader)
                    .ReadElementObject(() => new VideoEncoderConfiguration());

                Assert.AreEqual("main", configuration.Name);
                Assert.AreEqual(3, configuration.UseCount, "the second element was dropped");
                Assert.AreEqual(VideoEncoding.H264, configuration.Encoding);
                Assert.AreEqual("PT60S", configuration.SessionTimeout, "the fourth element was dropped");
            }
        }

        [DataRow("<Root><Type xmlns:p=\"urn:example\">p:Thing</Type></Root>",
                 DisplayName = "declared on the element carrying the name")]
        [DataRow("<Root xmlns:p=\"urn:example\"><Type>p:Thing</Type></Root>",
                 DisplayName = "declared on an ancestor")]
        [TestMethod]
        public void ResolvesAQualifiedNameWhereverItsPrefixWasDeclared(string xml)
        {
            // Reading the content is what moves the reader off the element, so a prefix declared
            // on that element is out of scope by the time the name is resolved - and the name came
            // back in no namespace at all.
            using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
            {
                reader.MoveToContent();
                reader.Read();

                XmlQualifiedName name = new OnvifXmlReader(reader).ReadElementQualifiedName();

                Assert.IsNotNull(name);
                Assert.AreEqual("Thing", name.Name);
                Assert.AreEqual("urn:example", name.Namespace);
            }
        }

        [TestMethod]
        public void CarriesThePrefixesTheElementWasWrittenUnder()
        {
            // The lifted element leaves its declarations behind. Onvif leans on them: a topic is
            // the text "tns1:RuleEngine/...", and the prefix is declared on the envelope.
            var configuration = Read<VideoEncoderConfiguration>(
                "<Configuration xmlns=\"" + Tt + "\" xmlns:tns1=\"http://www.onvif.org/ver10/topics\">" +
                  "<Name>main</Name>" +
                  "<Unexpected>tns1:RuleEngine/CellMotionDetector/Motion</Unexpected>" +
                "</Configuration>");

            XmlElement lifted = configuration.Any[0];

            Assert.AreEqual("http://www.onvif.org/ver10/topics", lifted.GetNamespaceOfPrefix("tns1"),
                "the prefix its content refers to has to still mean something");
        }
    }
}
