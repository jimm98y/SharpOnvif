using SharpOnvifCommon;
using SharpOnvifCommon.Security;
using System;
using System.ServiceModel.Channels;
using System.Xml;
using System.Xml.Serialization;

namespace SharpOnvifClient.Security
{
    /// <summary>
    /// Security header for the WsUsernameToken authentication.
    /// </summary>
    /// <remarks>https://stapp.space/using-soap-security-in-dotnet-core/</remarks>
    public class WsUsernameTokenHeader : MessageHeader
    {
        private readonly string _username;
        private readonly string _nonce;
        private readonly string _created;
        private readonly string _password;

        public WsUsernameTokenHeader(string username, string password, DateTime created)
        {
            _username = username;
            _nonce = DigestHelpers.CalculateNonce();
            _created = OnvifHelpers.DateTimeToString(created);
            _password = password;
        }

        public override string Name { get; } = "Security";

        public override string Namespace { get; } = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            var serializer = new XmlSerializer(typeof(UsernameToken));
            var pass = DigestHelpers.CreateSoapDigest(_nonce, _created, _password);
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
                    Created = _created
                });
        }
    }
}
