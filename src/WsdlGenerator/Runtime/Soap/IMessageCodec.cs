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
using System.Collections.Generic;
using System.IO;
using __RUNTIME__.Xml;

namespace __RUNTIME__.Soap
{
    /// <summary>
    /// The envelope a message travels in: what goes around the body on the way out, and what has
    /// to be got past on the way back.
    /// </summary>
    /// <remarks>
    /// The client knows that a call is a body written into an envelope and a reply read out of
    /// one. It does not know what an envelope looks like - SOAP 1.2 is one answer, and the one
    /// Onvif takes, but it is an answer rather than the question.
    /// </remarks>
    public interface IMessageCodec
    {
        /// <summary>What the request's Content-Type says the message is.</summary>
        string ContentType { get; }

        /// <summary>
        /// Writes a complete envelope around a body.
        /// </summary>
        /// <param name="prologue">Prefixes to declare on the envelope element, or null for none.</param>
        /// <param name="writeHeaders">
        /// Called inside the header, or null - in which case no header is written at all, rather
        /// than an empty one.
        /// </param>
        /// <param name="writeBody">Called inside the body.</param>
        string WriteEnvelope(
            IEnumerable<XmlNamespaceDeclaration> prologue,
            Action<IXmlWriter> writeHeaders,
            Action<IXmlWriter> writeBody);

        /// <summary>
        /// Reads a reply's body into <paramref name="into"/>, or returns false when the body is
        /// empty. A body carrying a fault is raised as an exception rather than returned.
        /// </summary>
        /// <param name="into">
        /// The contract to read into, or null to read the reply only far enough to find a fault in
        /// it - which is what an operation whose reply says nothing still has to do.
        /// </param>
        /// <param name="resolveXmlType">Resolves an xsi:type to an instance, and may be null.</param>
        bool ReadEnvelopeBody(Stream stream, OnvifContract into, Func<string, string, OnvifContract> resolveXmlType);
    }
}
