using System.Xml.Serialization;

namespace SharpOnvifServer.Security
{
    [XmlRoot(ElementName = "Password", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
    public class Password
    {
        [XmlAttribute(AttributeName = "Type")]
        public string Type { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "Nonce", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
    public class Nonce
    {
        [XmlAttribute(AttributeName = "EncodingType")]
        public string EncodingType { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "UsernameToken", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
    public class UsernameToken
    {
        [XmlElement(ElementName = "Username", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
        public string Username { get; set; }

        [XmlElement(ElementName = "Password", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
        public Password Password { get; set; }

        [XmlElement(ElementName = "Nonce", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd")]
        public Nonce Nonce { get; set; }

        [XmlElement(ElementName = "Created", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
        public string Created { get; set; }

        [XmlAttribute(AttributeName = "Id", Namespace = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd")]
        public string Id { get; set; }
    }
}
