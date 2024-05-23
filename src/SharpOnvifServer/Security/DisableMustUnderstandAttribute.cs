using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Description;
using CoreWCF.Dispatcher;
using System;
using System.Collections.ObjectModel;

namespace SharpOnvifServer.Security
{
    /// <summary>
    /// Workaround: Required because the Security header is not understood by WCF and it would fail the request because there are unparsed headers at the end of the pipeline.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DisableMustUnderstandValidationAttribute : Attribute, IServiceBehavior
    {
        void IServiceBehavior.Validate(ServiceDescription description, ServiceHostBase serviceHostBase) { }
        void IServiceBehavior.AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) { }
        void IServiceBehavior.ApplyDispatchBehavior(ServiceDescription description, ServiceHostBase serviceHostBase)
        {
            for (int i = 0; i < serviceHostBase.ChannelDispatchers.Count; i++)
            {
                if (serviceHostBase.ChannelDispatchers[i] is ChannelDispatcher channelDispatcher)
                {
                    foreach (EndpointDispatcher endpointDispatcher in channelDispatcher.Endpoints)
                    {
                        if (endpointDispatcher.IsSystemEndpoint) continue;
                        DispatchRuntime runtime = endpointDispatcher.DispatchRuntime;
                        runtime.ValidateMustUnderstand = false;
                    }
                }
            }
        }
    }
}
