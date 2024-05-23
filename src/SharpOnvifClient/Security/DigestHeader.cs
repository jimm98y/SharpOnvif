using System;
using System.ServiceModel.Channels;
using System.Xml;
using System.Xml.Serialization;

namespace SharpOnvifClient.Security
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

    /// <summary>
    /// Security header for the Digest authentication.
    /// </summary>
    /// <remarks>https://stapp.space/using-soap-security-in-dotnet-core/</remarks>
    public class DigestHeader : MessageHeader
    {
        private readonly string _username;
        private readonly string _nonce;
        private readonly DateTime _created;
        private readonly string _password;

        public DigestHeader(string username, string password, DateTime created)
        {
            _username = username;
            _nonce = DigestHelpers.CalculateNonce();
            _created = created;
            _password = password;
        }

        public override string Name { get; } = "Security";

        public override string Namespace { get; } = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            var serializer = new XmlSerializer(typeof(UsernameToken));
            var pass = DigestHelpers.CreateHashedPassword(_nonce, _created, _password);
            serializer.Serialize(writer,
                new UsernameToken
                {
                    Username = _username,
                    Password = new Password
                    {
                        Text = pass,
                        Type = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordDigest"
                    },
                    Nonce = new Nonce
                    {
                        Text = _nonce,
                        EncodingType = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-soap-message-security-1.0#Base64Binary"
                    },
                    Created = DigestHelpers.DateTimeToString(_created)
                });
        }
    }
}
