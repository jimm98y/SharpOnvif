// SharpOnvif
// Copyright (C) 2026 Lukas Volf
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.

using SharpOnvifCommon;
using SharpOnvifCommon.Security;
using System;
using System.Net;
using System.ServiceModel.Channels;
using System.Xml;
using System.Xml.Serialization;

namespace SharpOnvifClient.Security
{
    /// <summary>
    /// Security header for the WsUsernameToken authentication.
    /// </summary>
    /// <remarks>https://stapp.space/using-soap-security-in-dotnet-core/</remarks>
    public sealed class WsUsernameTokenHeader : MessageHeader
    {
        private readonly string _nonce;
        private readonly string _created;
        private NetworkCredential _credentials;

        public WsUsernameTokenHeader(NetworkCredential credentials, DateTime created)
        {
            this._credentials = credentials;
            _nonce = WsDigestAuthentication.CalculateNonce();
            _created = OnvifHelpers.DateTimeToString(created);
        }

        public override string Name { get; } = "Security";

        public override string Namespace { get; } = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            var serializer = new XmlSerializer(typeof(UsernameToken));
            var pass = WsDigestAuthentication.CreateSoapDigest(_nonce, _created, _credentials.Password);
            serializer.Serialize(writer,
                new UsernameToken
                {
                    Username = _credentials.UserName,
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
