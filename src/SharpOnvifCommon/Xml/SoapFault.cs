using System;
using System.Collections.Generic;
using System.Xml;

namespace SharpOnvifCommon.Xml
{
    /// <summary>A SOAP 1.2 fault, as Onvif devices report errors.</summary>
    public sealed class SoapFault
    {
        /// <summary>Top-level code, normally "Sender" or "Receiver".</summary>
        public string Code { get; set; }

        /// <summary>
        /// Nested subcodes, outermost first. Onvif puts its error taxonomy here, for example
        /// ter:NotAuthorized or ter:InvalidArgVal / ter:NoProfile.
        /// </summary>
        public IList<string> Subcodes { get; private set; }

        /// <summary>Human-readable reason text.</summary>
        public string Reason { get; set; }

        /// <summary>Contents of the Detail element, if any, as raw XML.</summary>
        public string Detail { get; set; }

        public SoapFault()
        {
            Subcodes = new List<string>();
        }

        /// <summary>The most specific subcode, which is the useful one for handling an error.</summary>
        public string Subcode
        {
            get { return Subcodes.Count == 0 ? null : Subcodes[Subcodes.Count - 1]; }
        }

        public OnvifFaultException ToException()
        {
            return new OnvifFaultException(this);
        }

        public override string ToString()
        {
            string subcode = Subcode;
            if (!string.IsNullOrEmpty(subcode) && !string.IsNullOrEmpty(Reason))
                return subcode + ": " + Reason;
            if (!string.IsNullOrEmpty(subcode)) return subcode;
            if (!string.IsNullOrEmpty(Reason)) return Reason;
            return Code ?? "SOAP fault";
        }

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

    /// <summary>Thrown when an Onvif device answers with a SOAP fault.</summary>
    public class OnvifFaultException : Exception
    {
        public OnvifFaultException(string message) : base(message)
        {
        }

        public OnvifFaultException(SoapFault fault) : base(fault == null ? "SOAP fault" : fault.ToString())
        {
            Fault = fault;
        }

        /// <summary>The parsed fault, or null when the response was not a well-formed fault.</summary>
        public SoapFault Fault { get; private set; }

        /// <summary>True when the device rejected the request for lack of valid credentials.</summary>
        public bool IsNotAuthorized
        {
            get
            {
                if (Fault == null) return false;
                foreach (string subcode in Fault.Subcodes)
                {
                    if (string.Equals(subcode, "NotAuthorized", StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }
        }
    }
}
