namespace __RUNTIME__.Xml
{
    /// <summary>XML namespaces the serialization layer refers to by name.</summary>
    /// <remarks>
    /// Partial: a generated part adds the namespaces of the schemas this runtime was generated
    /// for, under the names the generator was given for them.
    /// </remarks>
    public static partial class OnvifXmlNamespaces
    {
        public const string Xmlns = "http://www.w3.org/2000/xmlns/";
        public const string XmlSchema = "http://www.w3.org/2001/XMLSchema";
        public const string XmlSchemaInstance = "http://www.w3.org/2001/XMLSchema-instance";
        public const string Xml = "http://www.w3.org/XML/1998/namespace";

        /// <summary>SOAP 1.2, which every Onvif binding uses.</summary>
        public const string SoapEnvelope = "http://www.w3.org/2003/05/soap-envelope";

        public const string Addressing = "http://www.w3.org/2005/08/addressing";
    }

    /// <summary>A prefix bound to a namespace.</summary>
    public sealed class XmlNamespaceDeclaration
    {
        public XmlNamespaceDeclaration(string prefix, string @namespace)
        {
            Prefix = prefix;
            Namespace = @namespace;
        }

        /// <summary>The prefix, without the xmlns colon.</summary>
        public string Prefix { get; private set; }

        /// <summary>The namespace it stands for.</summary>
        public string Namespace { get; private set; }
    }
}
