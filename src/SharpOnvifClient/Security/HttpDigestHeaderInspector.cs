using SharpOnvifCommon.Security;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Authentication;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;

public class HttpDigestHeaderInspector : IClientMessageInspector
{
    private class HttpDigestCorrelation
    {
        public string Authorization { get; set; }
        public string Uri { get; }

        public HttpDigestCorrelation(string authorization, string uri)
        {
            Authorization = authorization;
            Uri = uri;
        }
    }

    private NetworkCredential _credentials;
    private readonly string[] _supportedHashAlgorithms;
    private readonly string[] _supportedQop;
    private static readonly HttpClient _httpClient = new HttpClient();

    public HttpDigestHeaderInspector(NetworkCredential credentials, string[] supportedHashAlgorithms, string[] supportedQop)
    {
        this._credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        this._supportedHashAlgorithms = supportedHashAlgorithms ?? throw new ArgumentNullException(nameof(supportedHashAlgorithms)); 
        this._supportedQop = supportedQop ?? throw new ArgumentNullException(nameof(supportedQop)); 
    }

    public void AfterReceiveReply(ref Message reply, object correlationState)
    {
        HttpDigestCorrelation correlation = (HttpDigestCorrelation)correlationState;
        if(correlation == null)
        {
            // no auth
            return;
        }

        if (reply.Properties.TryGetValue(HttpResponseMessageProperty.Name, out var httpResponseMessageObject))
        {
            HttpResponseMessageProperty httpResponseMessage = httpResponseMessageObject as HttpResponseMessageProperty;
            var authenticationInfoHeaders = httpResponseMessage.Headers.GetValues("Authentication-Info");
            if (authenticationInfoHeaders != null && authenticationInfoHeaders.Length == 1)
            {
                string receivedAuthenticationInfo = authenticationInfoHeaders.Single();

                string nextnonce = DigestAuthentication.GetValueFromHeader(receivedAuthenticationInfo, "nextnonce", true);
                string qop = DigestAuthentication.GetValueFromHeader(receivedAuthenticationInfo, "qop", false);
                string rspauth = DigestAuthentication.GetValueFromHeader(receivedAuthenticationInfo, "rspauth", true);
                string cnonce = DigestAuthentication.GetValueFromHeader(receivedAuthenticationInfo, "cnonce", true);
                string nc = DigestAuthentication.GetValueFromHeader(receivedAuthenticationInfo, "nc", false);

                string algorithm = DigestAuthentication.GetValueFromHeader(correlation.Authorization, "algorithm", false) ?? "";
                string authorizationNonce = DigestAuthentication.GetValueFromHeader(correlation.Authorization, "nonce", true);
                string authorizationCnonce = DigestAuthentication.GetValueFromHeader(correlation.Authorization, "cnonce", true);
                string authorizationNc = DigestAuthentication.GetValueFromHeader(correlation.Authorization, "nc", false);
                string authorizationRealm = DigestAuthentication.GetValueFromHeader(correlation.Authorization, "realm", true);
                string authorizationQop = DigestAuthentication.GetValueFromHeader(correlation.Authorization, "qop", false);

                if(!string.IsNullOrEmpty(nextnonce))
                {
                    Debug.WriteLine($"Nextnonce received: {nextnonce}, but it's currently not supported.");
                }

                if (string.IsNullOrEmpty(qop))
                {
                    throw new AuthenticationException("qop is missing in the Authentication-Info response header!");
                }

                if (string.IsNullOrEmpty(cnonce))
                {
                    throw new AuthenticationException("cnonce is missing in the Authentication-Info response header!");
                }

                if (string.IsNullOrEmpty(nc))
                {
                    throw new AuthenticationException("nc is missing in the Authentication-Info response header!");
                }

                if (string.IsNullOrEmpty(rspauth))
                {
                    throw new AuthenticationException("rspauth is missing in the Authentication-Info response header!");
                }

                if(string.Compare(authorizationCnonce, cnonce) != 0)
                {
                    throw new AuthenticationException("cnonce mismatch!");
                }

                if (string.Compare(authorizationNc, nc) != 0)
                {
                    throw new AuthenticationException("nc mismatch!");
                }

                if(qop.Contains("\""))
                {
                    // some implementations incorrectly put qop in quotes
                    qop = qop.Replace("\"", "");    
                }

                if(string.Compare("auth", qop) != 0 && string.Compare("auth-int", qop) != 0)
                {
                    throw new AuthenticationException("qop invalid!");
                }

                if(string.Compare(qop, authorizationQop) != 0)
                {
                    throw new AuthenticationException("qop mismatch!");
                }

                byte[] body = null;
                if (string.Compare("auth-int", qop, true) == 0)
                {
                    // Workaround: private reflection is unfortunate
                    // TODO: look for better solution
                    object messageData = reply.GetType().GetProperty("MessageData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(reply, null);
                    var buffer = (ArraySegment<byte>)messageData.GetType().GetProperty("Buffer").GetValue(messageData, null);
                    body = buffer.Array.Skip(buffer.Offset).Take(buffer.Count).ToArray();
                }

                string calculatedRspauth = DigestAuthentication.CreateWebDigestRFC7616(
                    algorithm,
                    _credentials.UserName,
                    authorizationRealm,
                    _credentials.Password,
                    false,
                    authorizationNonce,
                    "",
                    correlation.Uri,
                    DigestAuthentication.ConvertNCToInt(authorizationNc),
                    authorizationCnonce,
                    authorizationQop,
                    body);

                if(string.Compare(calculatedRspauth, rspauth) != 0)
                {
                    throw new AuthenticationException("rspauth validation failed!");
                }
            }
        }
    }

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
                string selectedQop = null;
                string[] serverSupportedQop = qop.Split(',');
                foreach (string offeredQop in serverSupportedQop)
                {
                    foreach (string supportedQop in this._supportedQop)
                    {
                        if (string.Compare(offeredQop.Trim(), supportedQop.Trim(), true) == 0)
                        {
                            selectedQop = supportedQop.Trim();
                            break;
                        }
                    }
                }

                int nc = 1;
                byte[] body = null;

                if (string.Compare("auth-int", selectedQop, true) == 0)
                {
                    body = ReadBody(ref request);
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

            // return the content of the authorization header so that we can use it to verify the Authentication-Info response
            return new HttpDigestCorrelation(authorization, channel.RemoteAddress.Uri.PathAndQuery);
        }

        return null;
    }

    private static byte[] ReadBody(ref Message request)
    {
        byte[] body;
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