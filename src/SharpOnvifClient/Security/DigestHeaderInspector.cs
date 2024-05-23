using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace SharpOnvifClient.Security
{
    public class DigestHeaderInspector : IClientMessageInspector
    {
        private readonly string _username;
        private readonly string _password;

        public DigestHeaderInspector(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        { }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            request.Headers.Add(new DigestHeader(_username, _password, DateTime.UtcNow));
            return null;
        }
    }
}
