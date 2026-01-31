using System;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace SharpOnvifClient.Security
{
    public class HttpDigestBehavior : IEndpointBehavior
    {
        private readonly NetworkCredential _credentials;
        private readonly string[] _supportedHashAlgorithms;
        private readonly string[] _supportedQop;

        public HttpDigestBehavior(NetworkCredential credentials, string[] supportedHashAlgorithms, string[] supportedQop)
        {
            this._credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            this._supportedHashAlgorithms = supportedHashAlgorithms ?? throw new ArgumentNullException(nameof(supportedHashAlgorithms));
            this._supportedQop = supportedQop ?? throw new ArgumentNullException(nameof(supportedQop));
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // do nothing
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new HttpDigestHeaderInspector(_credentials, _supportedHashAlgorithms, _supportedQop));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        { }

        public void Validate(ServiceEndpoint endpoint)
        {  }
    }
}
