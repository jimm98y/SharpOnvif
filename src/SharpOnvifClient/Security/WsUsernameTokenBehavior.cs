using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace SharpOnvifClient.Security
{
    public class WsUsernameTokenBehavior : IEndpointBehavior, IHasUtcOffset
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public TimeSpan UtcNowOffset { get; set; } = TimeSpan.Zero;

        public WsUsernameTokenBehavior(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }

        public WsUsernameTokenBehavior(string username, string password, TimeSpan utcNowOffset) : this(username, password)
        {
            this.UtcNowOffset = utcNowOffset;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // do nothing
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new WsUsernameTokenHeaderInspector(this.Username, this.Password, UtcNowOffset));
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
