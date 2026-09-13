using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SharpOnvifCommon.Xml;

namespace SharpOnvifCommon.Soap
{
    /// <summary>
    /// SOAP 1.2 as Onvif speaks it: the envelope a client's messages travel in.
    /// </summary>
    /// <remarks>
    /// The generated client knows only <see cref="IMessageCodec"/>. This is what meets it, and it
    /// is little more than a front for <see cref="SoapEnvelope"/>, which the server writes its own
    /// replies with.
    /// </remarks>
    public sealed class SoapMessageCodec : IMessageCodec
    {
        /// <summary>The one every client shares, having nothing of its own to remember.</summary>
        public static readonly SoapMessageCodec Instance = new SoapMessageCodec();

        public string ContentType { get { return SoapEnvelope.ContentType; } }

        public string WriteEnvelope(
            IEnumerable<XmlNamespaceDeclaration> prologue,
            Action<IXmlWriter> writeHeaders,
            Action<IXmlWriter> writeBody)
        {
            return SoapEnvelope.Write(prologue, writeHeaders, writeBody);
        }

        public bool ReadEnvelopeBody(
            Stream stream, XmlContract into, Func<string, string, XmlContract> resolveXmlType)
        {
            using (XmlReader xml = SoapEnvelope.CreateReader(stream))
            {
                // A fault is raised from in here, including for the 500 status code devices use to
                // carry one.
                if (!SoapEnvelope.MoveToBody(xml)) return false;
                if (into == null) return true;

                new OnvifXmlReader(xml, resolveXmlType).ReadInto(into);
                return true;
            }
        }
    }
}
