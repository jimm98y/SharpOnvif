using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Display
{
    [DisableMustUnderstandValidation]
    public class DisplayPortBase : DisplayPort
    {
        public virtual CreatePaneConfigurationResponse CreatePaneConfiguration(CreatePaneConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeletePaneConfigurationResponse DeletePaneConfiguration(DeletePaneConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetDisplayOptionsResponse GetDisplayOptions(GetDisplayOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetLayoutResponse GetLayout(GetLayoutRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetPaneConfigurationResponse GetPaneConfiguration(GetPaneConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PaneConfiguration")]
        public virtual GetPaneConfigurationsResponse GetPaneConfigurations(GetPaneConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        public virtual SetLayoutResponse SetLayout(SetLayoutRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual SetPaneConfigurationResponse SetPaneConfiguration(SetPaneConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Any")]
        public virtual SetPaneConfigurationsResponse SetPaneConfigurations(SetPaneConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
