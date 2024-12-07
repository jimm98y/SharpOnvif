using System;
using System.Net;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace SharpOnvifClient.Security
{
    public sealed class WsUsernameTokenBehavior : IEndpointBehavior, IHasUtcOffset
    {
        private readonly NetworkCredential _credentials;

        public TimeSpan UtcNowOffset { get; set; } = TimeSpan.Zero;

        public WsUsernameTokenBehavior(NetworkCredential credentials)
        {
            this._credentials = credentials;
        }

        public WsUsernameTokenBehavior(NetworkCredential credentials, TimeSpan utcNowOffset) : this(credentials)
        {
            this.UtcNowOffset = utcNowOffset;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // do nothing
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new WsUsernameTokenHeaderInspector(_credentials, UtcNowOffset));
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
