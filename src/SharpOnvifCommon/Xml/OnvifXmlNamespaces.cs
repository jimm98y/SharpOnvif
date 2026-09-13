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

namespace SharpOnvifCommon.Xml
{
    /// <summary>
    /// The Onvif half of the namespaces the serialization layer refers to by name. The other part
    /// of this class is generated and knows only about SOAP and XML itself.
    /// </summary>
    public static partial class OnvifXmlNamespaces
    {
        /// <summary>The shared Onvif schema, conventionally bound to the "tt" prefix.</summary>
        public const string OnvifSchema = "http://www.onvif.org/ver10/schema";

        /// <summary>The Onvif topic namespace, conventionally bound to the "tns1" prefix.</summary>
        public const string OnvifTopics = "http://www.onvif.org/ver10/topics";

        /// <summary>
        /// Declared on the envelope element of every Onvif message, whether or not its body uses
        /// them.
        /// </summary>
        /// <remarks>
        /// The bindings this replaces were CoreWCF-based and declared both, so devices and tools
        /// that have been talking to SharpOnvif keep seeing what they saw. "tns1" is needed for a
        /// second reason: an event topic is written as "tns1:Path", and a prefix used in element
        /// content has to be in scope wherever that content ends up.
        /// </remarks>
        public static readonly XmlNamespaceDeclaration[] EnvelopePrologue = new XmlNamespaceDeclaration[]
        {
            new XmlNamespaceDeclaration("tt", OnvifSchema),
            new XmlNamespaceDeclaration("tns1", OnvifTopics),
        };
    }
}
