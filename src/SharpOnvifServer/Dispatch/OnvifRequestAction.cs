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
using System.IO;
using System.Xml;
using SharpOnvifCommon.Xml;

namespace SharpOnvifServer.Dispatch
{
    /// <summary>
    /// The action a request names in its Content-Type.
    /// </summary>
    /// <remarks>
    /// Read in one place because two things decide from it: which operation runs, and - for the
    /// operations Onvif puts in its PRE_AUTH class - whether the device asks for a password at
    /// all. Two readings of one header is how a request comes to authenticate as one operation
    /// and execute as another.
    /// </remarks>
    internal static class OnvifRequestAction
    {
        private const string Parameter = "action=";

        /// <summary>
        /// The action, or null when the header names none - or names more than one, which there
        /// is no honest way to choose between.
        /// </summary>
        public static string FromContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return null;

            string action = null;

            // By parameter rather than by substring, so that a parameter merely ending in
            // "action=" is not mistaken for this one.
            foreach (string parameter in contentType.Split(';'))
            {
                string trimmed = parameter.Trim();
                if (!trimmed.StartsWith(Parameter, StringComparison.OrdinalIgnoreCase)) continue;

                // A second one and the caller has told us two different things. Refusing to guess
                // is the whole point: whichever of the two a reader picked, the other reader might
                // pick the other.
                if (action != null) return null;

                // Devices and tools quote this inconsistently, with single quotes, double quotes
                // or none at all.
                action = trimmed.Substring(Parameter.Length).Trim().Trim('"', '\'');
            }

            return string.IsNullOrEmpty(action) ? null : action;
        }

        /// <summary>
        /// Reads a wsa:Action header out of the envelope. Onvif Device Manager sends the action
        /// this way for event subscriptions rather than in the Content-Type header.
        /// </summary>
        public static string FromEnvelope(byte[] envelope)
        {
            try
            {
                using (XmlReader xml = SoapEnvelope.CreateReader(new MemoryStream(envelope, false)))
                {
                    ReadToBody(xml, out string action);
                    return action;
                }
            }
            catch (XmlException)
            {
                // Malformed envelopes are reported by the deserialisation path, which produces a
                // better message than anything this method could.
                return null;
            }
        }

        /// <summary>
        /// Reads a request envelope up to the first element in its Body, reporting on the way the
        /// action its Header carries, if it carries one. Returns false for a document that is not
        /// an envelope or whose Body is empty.
        /// </summary>
        /// <remarks>
        /// Authentication reads the header action through this too, so the two cannot disagree
        /// about which operation a request named: whether a request needs a password is decided
        /// by that action. The Header counts only where SOAP 1.2 puts it, before the Body.
        /// </remarks>
        public static bool ReadToBody(XmlReader xml, out string headerAction)
        {
            headerAction = null;

            xml.MoveToContent();
            if (xml.NodeType != XmlNodeType.Element
                || xml.LocalName != "Envelope"
                || xml.NamespaceURI != OnvifXmlNamespaces.SoapEnvelope
                || xml.IsEmptyElement)
            {
                return false;
            }

            int envelopeDepth = xml.Depth;
            bool headerRead = false;
            xml.Read();

            while (!xml.EOF)
            {
                if (xml.NodeType == XmlNodeType.EndElement && xml.Depth == envelopeDepth) return false;

                if (xml.NodeType != XmlNodeType.Element)
                {
                    xml.Read();
                    continue;
                }

                if (xml.Depth == envelopeDepth + 1 && xml.NamespaceURI == OnvifXmlNamespaces.SoapEnvelope)
                {
                    if (xml.LocalName == "Body") return MoveToFirstChild(xml);

                    if (xml.LocalName == "Header" && !headerRead)
                    {
                        headerRead = true;
                        headerAction = ReadHeaderAction(xml);
                        continue;
                    }
                }

                xml.Skip();
            }

            return false;
        }

        /// <summary>
        /// The text of the first Action element inside the Header the reader is on, leaving the
        /// reader after the Header.
        /// </summary>
        private static string ReadHeaderAction(XmlReader xml)
        {
            if (xml.IsEmptyElement)
            {
                xml.Read();
                return null;
            }

            string action = null;
            int headerDepth = xml.Depth;
            xml.Read();

            while (!xml.EOF)
            {
                if (xml.NodeType == XmlNodeType.EndElement && xml.Depth == headerDepth)
                {
                    xml.Read();
                    break;
                }

                if (action == null && xml.NodeType == XmlNodeType.Element && xml.LocalName == "Action")
                {
                    action = xml.ReadElementContentAsString().Trim();
                    continue;
                }

                xml.Read();
            }

            return action;
        }

        /// <summary>Moves from the Body start tag to its first element child.</summary>
        private static bool MoveToFirstChild(XmlReader xml)
        {
            if (xml.IsEmptyElement) return false;

            int bodyDepth = xml.Depth;
            while (xml.Read())
            {
                if (xml.NodeType == XmlNodeType.EndElement && xml.Depth == bodyDepth) return false;
                if (xml.NodeType == XmlNodeType.Element) return true;
            }

            return false;
        }
    }
}
