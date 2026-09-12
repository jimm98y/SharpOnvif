using System;
using System.IO;
using System.Text;
using System.Xml;

namespace SharpOnvifCommon.Xml
{
    /// <summary>
    /// Reads and writes the SOAP 1.2 envelopes Onvif exchanges.
    /// <para>
    /// The envelope is written with the same prefixes the previous CoreWCF-based implementation
    /// used: <c>SOAP-ENV</c> for the envelope and a <c>tt</c> declaration for the shared Onvif
    /// schema on the envelope element itself. Some devices and tools rely on seeing <c>tt</c>
    /// declared up front, which is why it is emitted even when the body does not use it.
    /// </para>
    /// </summary>
    public static class SoapEnvelope
    {
        public const string ContentType = "application/soap+xml";

        private static readonly XmlWriterSettings WriterSettings = new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(false),
            Indent = false,
            NewLineHandling = NewLineHandling.None,
        };

        private static readonly XmlReaderSettings ReaderSettings = new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = false,
            DtdProcessing = DtdProcessing.Prohibit,
            CloseInput = false,
        };

        /// <summary>
        /// Writes a complete envelope. <paramref name="writeHeaders"/> may be null; it is called
        /// inside the Header element, and the header is omitted entirely when nothing is written.
        /// </summary>
        public static string Write(Action<OnvifXmlWriter> writeHeaders, Action<OnvifXmlWriter> writeBody)
        {
            var buffer = new StringWriterUtf8();
            using (XmlWriter xml = XmlWriter.Create(buffer, WriterSettings))
            {
                xml.WriteStartElement("SOAP-ENV", "Envelope", OnvifXmlNamespaces.SoapEnvelope);

                // Declared up front so the whole document can use the conventional prefixes.
                // Some devices and tools expect to see "tt" declared on the envelope, and event
                // topics are written as "tns1:Path", which needs the topic namespace in scope.
                xml.WriteAttributeString("xmlns", "tt", OnvifXmlNamespaces.Xmlns, OnvifXmlNamespaces.OnvifSchema);
                xml.WriteAttributeString("xmlns", "tns1", OnvifXmlNamespaces.Xmlns, OnvifXmlNamespaces.OnvifTopics);

                var writer = new OnvifXmlWriter(xml);

                if (writeHeaders != null)
                {
                    xml.WriteStartElement("SOAP-ENV", "Header", OnvifXmlNamespaces.SoapEnvelope);
                    writeHeaders(writer);
                    xml.WriteEndElement();
                }

                xml.WriteStartElement("SOAP-ENV", "Body", OnvifXmlNamespaces.SoapEnvelope);
                if (writeBody != null) writeBody(writer);
                xml.WriteEndElement();

                xml.WriteEndElement();
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Moves a reader to the first element inside the SOAP body, or returns false when the
        /// body is empty. Throws <see cref="OnvifFaultException"/> when the body carries a fault.
        /// </summary>
        public static bool MoveToBody(XmlReader reader)
        {
            if (!MoveToEnvelopeChild(reader, "Body"))
                throw new OnvifFaultException("The response is not a SOAP envelope with a Body.");

            // Position on the first element child of Body.
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement) return false;
                if (reader.NodeType != XmlNodeType.Element) continue;

                if (reader.LocalName == "Fault" && reader.NamespaceURI == OnvifXmlNamespaces.SoapEnvelope)
                    throw SoapFault.Read(reader).ToException();

                return true;
            }

            return false;
        }

        /// <summary>Positions the reader on the named direct child of Envelope, if present.</summary>
        public static bool MoveToEnvelopeChild(XmlReader reader, string localName)
        {
            reader.MoveToContent();
            if (reader.NodeType != XmlNodeType.Element
                || reader.LocalName != "Envelope"
                || reader.NamespaceURI != OnvifXmlNamespaces.SoapEnvelope)
            {
                return false;
            }

            int envelopeDepth = reader.Depth;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == envelopeDepth) return false;
                if (reader.NodeType != XmlNodeType.Element || reader.Depth != envelopeDepth + 1) continue;
                if (reader.LocalName == localName && reader.NamespaceURI == OnvifXmlNamespaces.SoapEnvelope) return true;

                // Not the one we want; skip its subtree and keep looking.
                reader.Skip();
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == envelopeDepth) return false;
                if (reader.NodeType == XmlNodeType.Element && reader.Depth == envelopeDepth + 1
                    && reader.LocalName == localName && reader.NamespaceURI == OnvifXmlNamespaces.SoapEnvelope)
                {
                    return true;
                }
            }

            return false;
        }

        public static XmlReader CreateReader(Stream stream)
        {
            return XmlReader.Create(stream, ReaderSettings);
        }

        public static XmlReader CreateReader(TextReader text)
        {
            return XmlReader.Create(text, ReaderSettings);
        }

        /// <summary>
        /// A StringWriter that reports UTF-8, so the XML declaration the writer emits matches the
        /// encoding the payload is actually sent in.
        /// </summary>
        private sealed class StringWriterUtf8 : StringWriter
        {
            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }
        }
    }
}
