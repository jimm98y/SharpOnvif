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
    public class DigestMessageInspector : IDispatchMessageInspector
    {
        private IHttpContextAccessor HttpContextAccessor { get; }

        public DigestMessageInspector(IHttpContextAccessor httpContextAccessor)
        {
            HttpContextAccessor = httpContextAccessor;
        }

        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        { 
            return null; 
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            var webToken = DigestAuthenticationHandler.GetSecurityHeaderFromHeaders(HttpContextAccessor.HttpContext.Request);
            if (webToken != null)
            {
                byte[] body = null;
                if (string.Compare("auth-int", webToken.Qop, true) == 0)
                {
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
                                body = Encoding.UTF8.GetBytes(content);
                            }
                        }
                    }
                }

                string authenticationInfo =
                    DigestAuthentication.CreateAuthenticationInfoRFC7616(
                        webToken.Algorithm,
                        webToken.Uri,
                        webToken.Qop,
                        webToken.CNonce,
                        DigestAuthentication.ConvertNCToInt(webToken.Nc),
                        null,
                        body);
                HttpContextAccessor.HttpContext.Response.Headers.Append("Authentication-Info", authenticationInfo);
            }
        }
    }
}
