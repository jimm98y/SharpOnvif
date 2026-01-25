using SharpOnvifCommon.Security;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

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
                        qop = DigestAuthentication.GetValueFromHeader(receivedWwwAuth, "qop", true);

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
                string selectedQop = "auth";

                int nc = 1;

                response = DigestAuthentication.CreateWebDigestRFC7616(
                    algorithm,
                    _credentials.UserName,
                    realm,
                    _credentials.Password,
                    nonce,
                    method,
                    channel.RemoteAddress.Uri.PathAndQuery,
                    nc,
                    cnonce,
                    selectedQop
                    );

                authorization = DigestAuthentication.CreateAuthorizationRFC7616(
                    _credentials.UserName,
                    realm,
                    nonce,
                    channel.RemoteAddress.Uri.PathAndQuery,
                    response,
                    opaque,
                    algorithm,
                    selectedQop,
                    nc,
                    cnonce,
                    false);
            }            

            httpRequestMessage.Headers["Authorization"] = authorization;
            request.Properties[HttpRequestMessageProperty.Name] = httpRequestMessage;

            return cnonce;
        }

        return null;
    }
}