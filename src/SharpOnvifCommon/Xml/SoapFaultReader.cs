using System;
using System.Xml;

namespace SharpOnvifCommon.Xml
{
    /// <summary>
    /// Reads a <see cref="SoapFault"/> off the wire. The fault itself is part of what a client
    /// hands its caller and so is generated; picking one out of a reply is this library's job.
    /// </summary>
    public static class SoapFaultReader
    {
        /// <summary>Reads a fault from a reader positioned on the SOAP Fault element.</summary>
        public static SoapFault Read(XmlReader reader)
        {
            var fault = new SoapFault();
            int faultDepth = reader.Depth;

            if (reader.IsEmptyElement)
            {
                reader.Read();
                return fault;
            }

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == faultDepth) { reader.Read(); break; }
                if (reader.NodeType != XmlNodeType.Element) continue;

                switch (reader.LocalName)
                {
                    case "Code":
                        ReadCode(reader, fault);
                        break;
                    case "Reason":
                        fault.Reason = ReadReason(reader);
                        break;
                    case "Detail":
                        fault.Detail = reader.ReadInnerXml();
                        break;
                }
            }

            return fault;
        }

        /// <summary>Reads Code/Value plus the chain of nested Subcode/Value elements.</summary>
        private static void ReadCode(XmlReader reader, SoapFault fault)
        {
            int codeDepth = reader.Depth;
            if (reader.IsEmptyElement) return;

            bool first = true;
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == codeDepth) return;
                if (reader.NodeType != XmlNodeType.Element) continue;

                if (reader.LocalName == "Value")
                {
                    string value = Localise(reader.ReadElementContentAsString());
                    if (first) { fault.Code = value; first = false; }
                    else fault.Subcodes.Add(value);
                }
            }
        }

        /// <summary>Takes the first Text child of Reason; Onvif devices send a single language.</summary>
        private static string ReadReason(XmlReader reader)
        {
            int reasonDepth = reader.Depth;
            if (reader.IsEmptyElement) return null;

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Depth == reasonDepth) return null;
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "Text")
                    return reader.ReadElementContentAsString();
            }

            return null;
        }

        /// <summary>
        /// Drops the prefix from a fault code QName. The prefix binding is rarely useful and the
        /// local part is what callers match on.
        /// </summary>
        private static string Localise(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            int colon = value.IndexOf(':');
            return colon < 0 ? value : value.Substring(colon + 1);
        }
    }
}
