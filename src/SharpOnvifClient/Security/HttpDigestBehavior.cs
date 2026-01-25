using System;
using System.Collections.Generic;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace SharpOnvifClient.Security
{
    public class HttpDigestBehavior : IEndpointBehavior
    {
        private readonly NetworkCredential _credentials;
        private readonly List<string> _supportedHashAlgorithms;

        public HttpDigestBehavior(NetworkCredential credentials, List<string> supportedHashAlgorithms)
        {
            this._credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            this._supportedHashAlgorithms = supportedHashAlgorithms ?? throw new ArgumentNullException(nameof(supportedHashAlgorithms));
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // do nothing
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new HttpDigestHeaderInspector(_credentials, _supportedHashAlgorithms));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        { }

        public void Validate(ServiceEndpoint endpoint)
        {  }
    }
}
