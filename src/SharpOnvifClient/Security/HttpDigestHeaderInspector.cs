using SharpOnvifCommon.Security;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace SharpOnvifClient.Security
{
    public class HttpDigestHeaderInspector : IClientMessageInspector
    {
        private NetworkCredential _credentials;

        public HttpDigestHeaderInspector(NetworkCredential credentials)
        {
            this._credentials = credentials;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        { }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            HttpRequestMessageProperty httpRequestMessage;
            object httpRequestMessageObject;
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out httpRequestMessageObject))
            {
                httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;
                if (string.IsNullOrEmpty(httpRequestMessage.Headers["WWW-Authenticate"]))
                {
                    // Digest username="admin", realm="IP Camera", nonce="U/MVAKMI/Ue5j+rjWvTD3flLEYqZHtxBl/TSWaZgzg8=", uri="/onvif/device_service", response="b55aa61c96f1a169a43138189a268037"
                    string nonce = DigestHelpers.CalculateNonce();
                    string digest = DigestHelpers.CreateWebDigestRFC2069(_credentials.UserName, _credentials.Domain, _credentials.Password, nonce, request.Headers.Action, request.Properties.Via?.ToString());
                    httpRequestMessage.Headers["WWW-Authenticate"] = $"Digest username=\"{_credentials.UserName}\", realm=\"{_credentials.Domain}\", nonce=\"{nonce}\", uri=\"/onvif/device_service\", response=\"{digest}\"";
                }
            }
            else
            {
                httpRequestMessage = new HttpRequestMessageProperty();

                string nonce = DigestHelpers.CalculateNonce();
                string digest = DigestHelpers.CreateWebDigestRFC2069(_credentials.UserName, _credentials.Domain, _credentials.Password, nonce, request.Headers.Action, request.Properties.Via?.ToString());

                httpRequestMessage.Headers.Add("WWW-Authenticate", $"Digest username=\"{_credentials.UserName}\", realm=\"{_credentials.Domain}\", nonce=\"{nonce}\", uri=\"/onvif/device_service\", response=\"{digest}\"");
                request.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessage);
            }
            return null;
        }
    }
}
