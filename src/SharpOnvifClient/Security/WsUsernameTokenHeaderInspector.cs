using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace SharpOnvifClient.Security
{
    public sealed class WsUsernameTokenHeaderInspector : IClientMessageInspector
    {
        private readonly TimeSpan _utcNowOffset = TimeSpan.Zero;
        private readonly NetworkCredential _credentials;

        public WsUsernameTokenHeaderInspector(NetworkCredential credentials, TimeSpan utcNowOffset)
        {
            this._credentials = credentials;
            this._utcNowOffset = utcNowOffset;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        { }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            request.Headers.Add(new WsUsernameTokenHeader(_credentials, DateTime.UtcNow.Add(_utcNowOffset)));
            return null;
        }
    }
}
