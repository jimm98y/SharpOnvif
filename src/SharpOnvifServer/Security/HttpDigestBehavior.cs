using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Description;
using CoreWCF.Dispatcher;
using Microsoft.AspNetCore.Http;
using SharpOnvifServer.Security;
using System.Collections.ObjectModel;

namespace SharpOnvifServer
{
    public class HttpDigestBehavior : IServiceBehavior
    {
        private IHttpContextAccessor HttpContextAccessor { get; }
        private IUserRepository UserRepository { get; }

        public HttpDigestBehavior(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
        {
            HttpContextAccessor = httpContextAccessor;
            UserRepository = userRepository;
        }

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        { }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcherBase cdb in serviceHostBase.ChannelDispatchers)
            {
                var dispatcher = cdb as ChannelDispatcher;
                foreach (EndpointDispatcher endpointDispatcher in dispatcher.Endpoints)
                {
                    if (!endpointDispatcher.IsSystemEndpoint)
                    {
                        endpointDispatcher.DispatchRuntime.MessageInspectors.Add(new HttpDigestHeaderInspector(HttpContextAccessor, UserRepository));
                    }
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        { }
    }
}
