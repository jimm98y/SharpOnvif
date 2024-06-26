﻿using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace SharpOnvifClient.Security
{
    public class DigestBehavior : IEndpointBehavior
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public DigestBehavior(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // do nothing
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new DigestHeaderInspector(this.Username, this.Password));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            throw new NotImplementedException();
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            // do nothing
        }
    }
}
