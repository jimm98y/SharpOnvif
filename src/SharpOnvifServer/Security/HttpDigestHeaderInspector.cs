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

using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Dispatcher;
using Microsoft.AspNetCore.Http;
using SharpOnvifCommon.Security;
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace SharpOnvifServer.Security
{
    public class HttpDigestHeaderInspector : IDispatchMessageInspector
    {
        private IHttpContextAccessor HttpContextAccessor { get; }
        private IUserRepository UserRepository { get; }

        public HttpDigestHeaderInspector(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
        {
            HttpContextAccessor = httpContextAccessor;
            UserRepository = userRepository;
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        { 
            return null; 
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            var webToken = HttpContextAccessor.HttpContext.Request.GetSecurityHeaderFromHeaders();
            if (webToken != null)
            {
                byte[] body = null;
                if (string.Compare("auth-int", webToken.Qop, true) == 0)
                {
                    body = ReadResponseBody(ref reply);
                }

                UserInfo user = UserRepository.GetUser(webToken);

                string noncePrime = webToken.Nonce;
                string cnoncePrime = webToken.CNonce;
                if (!string.IsNullOrEmpty(webToken.Opaque))
                {
                    var prime = HttpDigestAuthentication.GetNoncePrime(webToken.Opaque);
                    if (prime != null)
                    {
                        noncePrime = prime.Value.nonce;
                        cnoncePrime = prime.Value.cnonce;
                    }
                }

                string authenticationInfo =
                    HttpDigestAuthentication.CreateAuthenticationInfoRFC7616(
                        webToken.Algorithm,
                        user.UserName,
                        webToken.Realm,
                        user.Password,
                        user.IsPasswordAlreadyHashed,
                        webToken.Nonce,
                        webToken.Uri,
                        HttpDigestAuthentication.ConvertNCToInt(webToken.Nc),
                        webToken.CNonce,
                        webToken.Qop,
                        body,
                        null,
                        noncePrime,
                        cnoncePrime);
                HttpContextAccessor.HttpContext.Response.Headers.Append("Authentication-Info", authenticationInfo);
            }
        }

        private static byte[] ReadResponseBody(ref Message reply)
        {
            byte[] body;
            // we have to create a copy of the message because it can only be read once
            using (MessageBuffer mb = reply.CreateBufferedCopy(int.MaxValue))
            {
                using (MemoryStream s = new MemoryStream())
                {
                    using (XmlWriter xw = XmlWriter.Create(s, new XmlWriterSettings() { OmitXmlDeclaration = true }))
                    {
                        // make sure the original message is set to a value which has not been copied
                        reply = mb.CreateMessage();

                        using (var msg = mb.CreateMessage())
                        {
                            msg.WriteMessage(xw);
                            xw.Flush();
                        }

                        s.Position = 0;

                        byte[] bXML = new byte[s.Length];
                        s.Read(bXML, 0, (int)s.Length);

                        string content = "";

                        if (bXML[0] != (byte)'<')
                        {
                            content = Encoding.UTF8.GetString(bXML, 3, bXML.Length - 3);
                        }
                        else
                        {
                            content = Encoding.UTF8.GetString(bXML, 0, bXML.Length);
                        }

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.PreserveWhitespace = true;
                        xmlDoc.LoadXml(content);
                        // Workaround: For HTTP Digest authentication, the Headers section includes an Action element which
                        //   is removed before the message is sent. Therefore we have to remove it to calculate the hash.
                        //   see https://web.archive.org/web/20130110115740/http://www.oreillynet.com/xml/blog/2002/11/unraveling_the_mystery_of_soap.html
                        XmlNode xNode = xmlDoc.SelectSingleNode("//*[local-name()='Action']");
                        if (xNode != null)
                        {
                            var parent = xNode.ParentNode;
                            parent.RemoveChild(xNode);

                            // remove also the Header section if empty
                            if (parent.ChildNodes.Count == 0)
                            {
                                parent.ParentNode.RemoveChild(parent);
                            }
                        }
                        using (var modifiedOutput = new MemoryStream())
                        {
                            xmlDoc.Save(modifiedOutput);
                            content = Encoding.UTF8.GetString(modifiedOutput.ToArray());
                        }
                        // Workaround: The XmlWriter always adds a space before the ending tag, remove it
                        content = content.Replace("\" />", "\"/>");

                        // Here we are making an assumption that whatever we just created is exactly 
                        //  what will be sent on the wire. This seems to be true for most requests 
                        //  I have tested so far, but it seems very fragile.
                        body = Encoding.UTF8.GetBytes(content);
                    }
                }
            }

            return body;
        }
    }
}
