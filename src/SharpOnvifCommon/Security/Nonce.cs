using System.Xml.Serialization;

namespace SharpOnvifCommon.Security
{
    [XmlRoot(ElementName = "Nonce", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
    public class Nonce
    {
        [XmlAttribute(AttributeName = "EncodingType")]
        public string EncodingType { get; set; }

        [XmlText]
        public string Text { get; set; }
    }
}
