﻿using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace SharpOnvifClient.Security
{
    public class DigestHeaderInspector : IClientMessageInspector
    {
        private readonly string _username;
        private readonly string _password;
        private readonly TimeSpan _utcNowOffset = TimeSpan.Zero;

        public DigestHeaderInspector(string username, string password, TimeSpan utcNowOffset)
        {
            _username = username;
            _password = password;
            _utcNowOffset = utcNowOffset;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        { }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            request.Headers.Add(new DigestHeader(_username, _password, DateTime.UtcNow.Add(_utcNowOffset)));
            return null;
        }
    }
}
