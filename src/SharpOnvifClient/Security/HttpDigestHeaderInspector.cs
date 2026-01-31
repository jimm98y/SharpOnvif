using SharpOnvifClient.Security;
using SharpOnvifCommon.Security;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
    private readonly IHttpMessageState _state;

    public HttpDigestHeaderInspector(NetworkCredential credentials, string[] supportedHashAlgorithms, string[] supportedQop, IHttpMessageState state)
    {
        this._credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
        this._supportedHashAlgorithms = supportedHashAlgorithms ?? throw new ArgumentNullException(nameof(supportedHashAlgorithms)); 
        this._supportedQop = supportedQop ?? throw new ArgumentNullException(nameof(supportedQop));
        this._state = state ?? throw new ArgumentNullException(nameof(state));
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

                string nextnonce = HttpDigestAuthentication.GetValueFromHeader(receivedAuthenticationInfo, "nextnonce", true);
                string qop = HttpDigestAuthentication.GetValueFromHeader(receivedAuthenticationInfo, "qop", false);
                string rspauth = HttpDigestAuthentication.GetValueFromHeader(receivedAuthenticationInfo, "rspauth", true);
                string cnonce = HttpDigestAuthentication.GetValueFromHeader(receivedAuthenticationInfo, "cnonce", true);
                string nc = HttpDigestAuthentication.GetValueFromHeader(receivedAuthenticationInfo, "nc", false);

                string algorithm = HttpDigestAuthentication.GetValueFromHeader(correlation.Authorization, "algorithm", false) ?? "";
                string authorizationNonce = HttpDigestAuthentication.GetValueFromHeader(correlation.Authorization, "nonce", true);
                string authorizationCnonce = HttpDigestAuthentication.GetValueFromHeader(correlation.Authorization, "cnonce", true);
                string authorizationNc = HttpDigestAuthentication.GetValueFromHeader(correlation.Authorization, "nc", false);
                string authorizationRealm = HttpDigestAuthentication.GetValueFromHeader(correlation.Authorization, "realm", true);
                string authorizationQop = HttpDigestAuthentication.GetValueFromHeader(correlation.Authorization, "qop", false);

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

                string calculatedRspauth = HttpDigestAuthentication.CreateWebDigestRFC7616(
                    algorithm,
                    _credentials.UserName,
                    authorizationRealm,
                    _credentials.Password,
                    false,
                    authorizationNonce,
                    "",
                    correlation.Uri,
                    HttpDigestAuthentication.ConvertNCToInt(authorizationNc),
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

        var headers = _state.GetHeaders();

        if (headers != null)
        {
            foreach (var header in headers)
            {
                string receivedWwwAuth = header.ToString();
                nonce = HttpDigestAuthentication.GetValueFromHeader(receivedWwwAuth, "nonce", true);
                realm = HttpDigestAuthentication.GetValueFromHeader(receivedWwwAuth, "realm", true);
                opaque = HttpDigestAuthentication.GetValueFromHeader(receivedWwwAuth, "opaque", true);
                algorithm = HttpDigestAuthentication.GetValueFromHeader(receivedWwwAuth, "algorithm", false) ?? "";
                stale = (HttpDigestAuthentication.GetValueFromHeader(receivedWwwAuth, "stale", false) ?? "").ToUpperInvariant() == "TRUE";
                userhash = (HttpDigestAuthentication.GetValueFromHeader(receivedWwwAuth, "userhash", false) ?? "").ToUpperInvariant() == "TRUE";
                qop = HttpDigestAuthentication.GetValueFromHeader(receivedWwwAuth, "qop", true);

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

        if (isSupported)
        {
            string uri = channel.RemoteAddress.Uri.PathAndQuery;
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
                    uri);

                authorization = DigestAuthentication.CreateAuthorizationRFC2069(
                    _credentials.UserName, 
                    realm, 
                    nonce, 
                    uri, 
                    response, 
                    opaque,
                    algorithm);
                */
            }
            else
            {
                int nextNc = _state.GetAndUpdateNC();
                string cnonce = HttpDigestAuthentication.GenerateClientNonce(BinarySerializationType.Hex);
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

                byte[] body = null;

                if (string.Compare("auth-int", selectedQop, true) == 0)
                {
                    body = ReadRequestBody(ref request);
                }

                response = HttpDigestAuthentication.CreateWebDigestRFC7616(
                    algorithm,
                    _credentials.UserName,
                    realm,
                    _credentials.Password,
                    false,
                    nonce,
                    method,
                    uri,
                    nextNc,
                    cnonce,
                    selectedQop,
                    body
                    );

                string username;
                if(userhash)
                {
                    username = HttpDigestAuthentication.CreateUserNameHashRFC7616(algorithm, _credentials.UserName, realm);
                }
                else
                {
                    username = _credentials.UserName;
                }

                authorization = HttpDigestAuthentication.CreateAuthorizationRFC7616(
                    username,
                    realm,
                    nonce,
                    uri,
                    response,
                    opaque,
                    algorithm,
                    selectedQop,
                    nextNc,
                    cnonce,
                    userhash);
            }            

            httpRequestMessage.Headers["Authorization"] = authorization;
            request.Properties[HttpRequestMessageProperty.Name] = httpRequestMessage;

            // return the content of the authorization header so that we can use it to verify the Authentication-Info response
            return new HttpDigestCorrelation(authorization, uri);
        }

        return null;
    }

    private static byte[] ReadRequestBody(ref Message request)
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