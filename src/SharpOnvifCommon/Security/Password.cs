using System.Xml.Serialization;

namespace SharpOnvifCommon.Security
{
    [XmlRoot(ElementName = "Password", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
    public class Password
    {
        [XmlAttribute(AttributeName = "Type")]
        public string Type { get; set; }

        [XmlText]
        public string Text { get; set; }
    }
}
