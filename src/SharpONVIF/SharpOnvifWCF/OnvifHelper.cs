using System;
using System.Net;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SharpOnvifWCF
{
    /// <summary>
    /// Helper class for the WCF auto-generated stubs.
    /// </summary>
    public static class OnvifHelper
    {
        public static void ConfigureAuthentication(ServiceEndpoint endpoint, string userName, string password)
        {
            var passwordDigestBehavior = new PasswordDigestBehavior(userName, password);
            endpoint.EndpointBehaviors.Add(passwordDigestBehavior);
        }

        public static Binding CreateBinding()
        {
            var httpTransportBinding = new HttpTransportBindingElement { AuthenticationScheme = AuthenticationSchemes.Digest };
            var textMessageEncodingBinding = new TextMessageEncodingBindingElement { MessageVersion = MessageVersion.CreateVersion(EnvelopeVersion.Soap12, AddressingVersion.None) };
            var customBinding = new CustomBinding(textMessageEncodingBinding, httpTransportBinding);
            return customBinding;
        }

        public static EndpointAddress CreateEndpointAddress(string endPointAddress)
        {
            return new EndpointAddress(endPointAddress);
        }
    }

    /// <summary>
    /// Security header for the Digest authentication.
    /// </summary>
    /// <remarks>https://stapp.space/using-soap-security-in-dotnet-core/</remarks>
    public class PasswordDigestHeader : MessageHeader
    {
        private readonly string _username;
        private readonly string _nonce;
        private readonly string _created;
        private readonly string _password;

        public PasswordDigestHeader(string username, string password, DateTime created)
        {
            _username = username;
            _nonce = CalculateNonce();
            _created = created.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            _password = password;
        }


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

        public override string Name { get; } = "Security";

        public override string Namespace { get; } = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            var serializer = new XmlSerializer(typeof(UsernameToken));
            var pass = CreateHashedPassword(_nonce, _created, _password);
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

        private static string CalculateNonce()
        {
            var byteArray = new byte[32];
            using (var rnd = RandomNumberGenerator.Create())
            {
                rnd.GetBytes(byteArray);
            }
            return Convert.ToBase64String(byteArray);
        }

        private static string CreateHashedPassword(string nonceStr, string created, string password)
        {
            var nonce = Convert.FromBase64String(nonceStr);
            var createdBytes = Encoding.UTF8.GetBytes(created);
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var combined = new byte[createdBytes.Length + nonce.Length + passwordBytes.Length];

            Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
            Buffer.BlockCopy(createdBytes, 0, combined, nonce.Length, createdBytes.Length);
            Buffer.BlockCopy(passwordBytes, 0, combined, nonce.Length + createdBytes.Length, passwordBytes.Length);

            return Convert.ToBase64String(SHA1.Create().ComputeHash(combined));
        }
    }

    public class PasswordDigestHeaderInspector : IClientMessageInspector
    {
        private readonly string _username;
        private readonly string _password;

        public PasswordDigestHeaderInspector(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        { }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            request.Headers.Add(new PasswordDigestHeader(_username, _password, DateTime.UtcNow));
            return null;
        }
    }

    public class PasswordDigestBehavior : IEndpointBehavior
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public PasswordDigestBehavior(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // do nothing
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new PasswordDigestHeaderInspector(this.Username, this.Password));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            throw new NotImplementedException();
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            // do nothing
        }
    }
}
