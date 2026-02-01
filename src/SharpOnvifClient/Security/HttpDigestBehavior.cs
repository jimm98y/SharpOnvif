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
        private readonly DigestAuthenticationSchemeOptions _authentication;
        private readonly IHttpMessageState _state;

        public HttpDigestBehavior(NetworkCredential credentials, DigestAuthenticationSchemeOptions authentication, IHttpMessageState state)
        {
            this._credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            this._authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
            this._state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        { }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new HttpDigestHeaderInspector(_credentials, _authentication, _state));
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        { }

        public void Validate(ServiceEndpoint endpoint)
        { }
    }
}
