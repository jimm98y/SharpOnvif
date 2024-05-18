using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace SharpOnvifWCF.Security
{
    public class PasswordDigestHeaderInspector : IClientMessageInspector
    {
        private readonly string _username;
        private readonly string _password;

        public PasswordDigestHeaderInspector(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        { }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            request.Headers.Add(new PasswordDigestHeader(_username, _password, DateTime.UtcNow));
            return null;
        }
    }
}
