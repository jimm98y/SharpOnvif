using SharpOnvifCommon.Security;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;

public class HttpDigestHeaderInspector : IClientMessageInspector
{
    private NetworkCredential _credentials;
    private readonly string[] _supportedHashAlgorithms;
    private static readonly HttpClient _httpClient = new HttpClient();

    public HttpDigestHeaderInspector(NetworkCredential credentials, string[] supportedHashAlgorithms)
    {
        this._credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        this._supportedHashAlgorithms = supportedHashAlgorithms ?? throw new ArgumentNullException(nameof(supportedHashAlgorithms)); 
    }

    public void AfterReceiveReply(ref Message reply, object correlationState)
    {  }

    public object BeforeSendRequest(ref Message request, IClientChannel channel)
    {
        HttpRequestMessageProperty httpRequestMessage;

        if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out var httpRequestMessageObject))
        {
            httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;
        }
        else
        {
            httpRequestMessage = new HttpRequestMessageProperty();
            httpRequestMessage.Method = "POST";
        }

        string nonce = "";
        string realm = "";
        string opaque = "";
        string algorithm = "";
        string qop = "";
        bool stale = false;
        bool userhash = false;
        bool isSupported = false;

        // pre-flight request to get the www-auth header from the server
        using (var req = new HttpRequestMessage(HttpMethod.Head, channel.RemoteAddress.Uri))
        {
            HttpResponseMessage resp = null;

            try
            {
#if NET5_0_OR_GREATER
                resp = _httpClient.Send(req, HttpCompletionOption.ResponseHeadersRead);
#else
                resp = _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).Result;
#endif
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }

            using (resp)
            {
                if (resp.StatusCode == HttpStatusCode.Unauthorized && resp.Headers.WwwAuthenticate != null)
                {
                    foreach (var header in resp.Headers.WwwAuthenticate)
                    {
                        var receivedWwwAuth = header.ToString();
                        nonce = DigestAuthentication.GetValueFromHeader(receivedWwwAuth, "nonce", true);
                        realm = DigestAuthentication.GetValueFromHeader(receivedWwwAuth, "realm", true);
                        opaque = DigestAuthentication.GetValueFromHeader(receivedWwwAuth, "opaque", true);
                        algorithm = DigestAuthentication.GetValueFromHeader(receivedWwwAuth, "algorithm", false) ?? "";
                        stale = (DigestAuthentication.GetValueFromHeader(receivedWwwAuth, "stale", false) ?? "").ToUpperInvariant() == "TRUE";
                        userhash = (DigestAuthentication.GetValueFromHeader(receivedWwwAuth, "userhash", false) ?? "").ToUpperInvariant() == "TRUE";
                        qop = DigestAuthentication.GetValueFromHeader(receivedWwwAuth, "qop", true);

                        if(string.IsNullOrEmpty(qop))
                        {
                            // RFC 2069 is not allowed by the Onvif Core specification
                            continue;
                        }

                        if (!string.IsNullOrEmpty(nonce))
                        {
                            if(string.IsNullOrEmpty(algorithm))
                            {
                                if(_supportedHashAlgorithms.Contains("") || _supportedHashAlgorithms.Contains("MD5"))
                                {
                                    isSupported = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (_supportedHashAlgorithms.Contains(algorithm))
                                {
                                    // select the first supported header
                                    isSupported = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        string cnonce = null;
        if (isSupported)
        {
            string method = string.IsNullOrEmpty(httpRequestMessage.Method) ? "POST" : httpRequestMessage.Method;
            string response;
            string authorization;

            // RFC 2069 is not allowed by the Onvif Core specification
            if (string.IsNullOrEmpty(qop))
            {
                throw new NotSupportedException("RFC 2069 is not allowed by the Onvif Core specification");
                /*
                response = DigestAuthentication.CreateWebDigestRFC2069(
                    algorithm,
                    _credentials.UserName,
                    realm,
                    _credentials.Password,
                    nonce,
                    method,
                    channel.RemoteAddress.Uri.PathAndQuery);

                authorization = DigestAuthentication.CreateAuthorizationRFC2069(
                    _credentials.UserName, 
                    realm, 
                    nonce, 
                    channel.RemoteAddress.Uri.PathAndQuery, 
                    response, 
                    opaque,
                    algorithm);
                */
            }
            else
            {
                cnonce = DigestAuthentication.GenerateClientNonce(BinarySerializationType.Hex);
                string selectedQop; 
                if(qop.Contains("auth-int"))
                {
                    selectedQop = "auth-int";
                }
                else if(qop.Contains("auth"))
                {
                    selectedQop = "auth";
                }
                else
                {
                    throw new NotSupportedException($"Invalid qop={qop}");
                }

                int nc = 1;
                byte[] body = null;

                if (string.Compare("auth-int", selectedQop, true) == 0)
                {
                    // we have to create a copy of the message because it can only be read once
                    using (MessageBuffer mb = request.CreateBufferedCopy(int.MaxValue))
                    {
                        using (MemoryStream s = new MemoryStream())
                        {
                            using (XmlWriter xw = XmlWriter.Create(s, new XmlWriterSettings() { OmitXmlDeclaration = true }))
                            {
                                // make sure the original message is set to a value which has not been copied
                                request = mb.CreateMessage();

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

                response = DigestAuthentication.CreateWebDigestRFC7616(
                    algorithm,
                    _credentials.UserName,
                    realm,
                    _credentials.Password,
                    false,
                    nonce,
                    method,
                    channel.RemoteAddress.Uri.PathAndQuery,
                    nc,
                    cnonce,
                    selectedQop,
                    body
                    );

                string username;
                if(userhash)
                {
                    username = DigestAuthentication.CreateUserNameHashRFC7616(algorithm, _credentials.UserName, realm);
                }
                else
                {
                    username = _credentials.UserName;
                }

                authorization = DigestAuthentication.CreateAuthorizationRFC7616(
                    username,
                    realm,
                    nonce,
                    channel.RemoteAddress.Uri.PathAndQuery,
                    response,
                    opaque,
                    algorithm,
                    selectedQop,
                    nc,
                    cnonce,
                    userhash);
            }            

            httpRequestMessage.Headers["Authorization"] = authorization;
            request.Properties[HttpRequestMessageProperty.Name] = httpRequestMessage;

            return cnonce;
        }

        return null;
    }
}