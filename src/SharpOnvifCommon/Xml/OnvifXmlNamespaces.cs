namespace SharpOnvifCommon.Xml
{
    /// <summary>XML namespaces the serialization layer refers to by name.</summary>
    public static class OnvifXmlNamespaces
    {
        public const string Xmlns = "http://www.w3.org/2000/xmlns/";
        public const string XmlSchema = "http://www.w3.org/2001/XMLSchema";
        public const string XmlSchemaInstance = "http://www.w3.org/2001/XMLSchema-instance";
        public const string Xml = "http://www.w3.org/XML/1998/namespace";

        /// <summary>SOAP 1.2, which every Onvif binding uses.</summary>
        public const string SoapEnvelope = "http://www.w3.org/2003/05/soap-envelope";

        public const string Addressing = "http://www.w3.org/2005/08/addressing";

        /// <summary>The shared Onvif schema, conventionally bound to the "tt" prefix.</summary>
        public const string OnvifSchema = "http://www.onvif.org/ver10/schema";

        /// <summary>The Onvif topic namespace, conventionally bound to the "tns1" prefix.</summary>
        public const string OnvifTopics = "http://www.onvif.org/ver10/topics";
    }
}
