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

using System.Linq;
using SharpOnvifCommon;
using SharpOnvifCommon.Xml;

namespace SharpOnvif.Tests
{
    /// <summary>
    /// The envelope every message travels in.
    /// <para>
    /// What it declares up front is not the same for every schema, so it is decided when the
    /// runtime is generated rather than written into it. These are the values this build was
    /// generated with: nothing in the runtime's own source would notice them going missing.
    /// </para>
    /// </summary>
    [TestClass]
    public sealed class TestSoapEnvelope
    {
        [TestMethod]
        public void DeclaresTheOnvifPrefixesOnEveryEnvelope()
        {
            // The bindings this replaces declared both, so devices and tools that have been
            // talking to SharpOnvif keep seeing what they saw. "tns1" is needed for a second
            // reason: an event topic is written as "tns1:Path", and a prefix used in element
            // content has to be in scope wherever that content ends up.
            string envelope = SoapEnvelope.Write(null, writer => writer.WriteStartElement(
                "http://www.onvif.org/ver10/device/wsdl", "GetDeviceInformation"));

            StringAssert.Contains(envelope, "xmlns:tt=\"http://www.onvif.org/ver10/schema\"");
            StringAssert.Contains(envelope, "xmlns:tns1=\"http://www.onvif.org/ver10/topics\"");
        }

        [TestMethod]
        public void DeclaresThemWhateverTheMessageIs()
        {
            // Including when the body has no use for them, which is the case that would pass
            // unnoticed if the declarations were written only where they happen to be needed.
            string empty = SoapEnvelope.Write(null, null);

            StringAssert.Contains(empty, "xmlns:tt=");
            StringAssert.Contains(empty, "xmlns:tns1=");
        }

        [TestMethod]
        public void NamesThePrefixesItWasGeneratedFor()
        {
            // The same two namespaces, reachable by name rather than only by prefix.
            Assert.AreEqual("http://www.onvif.org/ver10/schema", OnvifXmlNamespaces.OnvifSchema);
            Assert.AreEqual("http://www.onvif.org/ver10/topics", OnvifXmlNamespaces.OnvifTopics);

            CollectionAssert.AreEquivalent(
                new[] { "tt", "tns1" },
                RuntimeDefaults.EnvelopePrologue.Select(d => d.Prefix).ToArray());
        }

        [TestMethod]
        public void KnowsWhichActionsNeedNoCredentials()
        {
            // The other half of what the runtime is generated with. A client sends these
            // unauthenticated, so losing them makes it authenticate calls a device answers
            // without credentials - which some devices reject.
            CollectionAssert.Contains(
                RuntimeDefaults.PreAuthActions,
                "http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime");

            Assert.AreEqual(7, RuntimeDefaults.PreAuthActions.Length, "the PRE_AUTH category");
        }
    }
}
