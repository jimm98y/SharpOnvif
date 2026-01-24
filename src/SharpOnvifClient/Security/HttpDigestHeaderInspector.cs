using SharpOnvifCommon.Security;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text.RegularExpressions;

public class HttpDigestHeaderInspector : IClientMessageInspector
{
    private NetworkCredential _credentials;
    private static readonly HttpClient _httpClient = new HttpClient();

    public HttpDigestHeaderInspector(NetworkCredential credentials)
    {
        this._credentials = credentials;
    }

    public void AfterReceiveReply(ref Message reply, object correlationState)
    { }

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
        //string qop;

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
                    var receivedWwwAuth = resp.Headers.WwwAuthenticate.ToString();
                    var receivedNonce = Regex.Match(receivedWwwAuth, "nonce=\"(?<n>[^\"]+)\"", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    var receivedRealm = Regex.Match(receivedWwwAuth, "realm=\"(?<r>[^\"]+)\"", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    var receivedOpaque = Regex.Match(receivedWwwAuth, "opaque=\"(?<o>[^\"]+)\"", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
                    //var receivedQop = Regex.Match(receivedWwwAuth, "qop=\"(?<q>[^\"]+)\"", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

                    if (receivedNonce.Success)
                    {
                        nonce = receivedNonce.Groups["n"].Value;
                        realm = receivedRealm.Success ? receivedRealm.Groups["r"].Value : null;
                        opaque = receivedOpaque.Success ? receivedOpaque.Groups["o"].Value : null;
                        //qop = receivedQop.Success ? receivedQop.Groups["q"].Value : null;
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(nonce))
        {
            const string algorithm = "MD5";

            string method = string.IsNullOrEmpty(httpRequestMessage.Method) ? "POST" : httpRequestMessage.Method;
            string digest = DigestAuthentication.CreateWebDigestRFC2069(
                algorithm,
                _credentials.UserName,
                realm,
                _credentials.Password,
                nonce,
                method,
                channel.RemoteAddress.Uri.PathAndQuery);

            string authorization = DigestAuthentication.CreateAuthorizationRFC2069(_credentials.UserName, realm, nonce, channel.RemoteAddress.Uri.PathAndQuery, digest, opaque, algorithm);
            httpRequestMessage.Headers["Authorization"] = authorization;
        }

        request.Properties[HttpRequestMessageProperty.Name] = httpRequestMessage;

        return null;
    }
}