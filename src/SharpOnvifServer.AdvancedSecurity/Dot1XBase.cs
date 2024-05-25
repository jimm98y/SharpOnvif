using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.AdvancedSecurity
{
    [DisableMustUnderstandValidation]
    public class Dot1XBase : Dot1X
    {
        [return: MessageParameter(Name = "Dot1XID")]
        public virtual AddDot1XConfigurationResponse AddDot1XConfiguration(AddDot1XConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteDot1XConfigurationResponse DeleteDot1XConfiguration(DeleteDot1XConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RebootNeeded")]
        public virtual bool DeleteNetworkInterfaceDot1XConfiguration(string token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual GetAllDot1XConfigurationsResponse GetAllDot1XConfigurations(GetAllDot1XConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Dot1XConfiguration")]
        public virtual GetDot1XConfigurationResponse GetDot1XConfiguration(GetDot1XConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Dot1XID")]
        public virtual GetNetworkInterfaceDot1XConfigurationResponse GetNetworkInterfaceDot1XConfiguration(GetNetworkInterfaceDot1XConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RebootNeeded")]
        public virtual SetNetworkInterfaceDot1XConfigurationResponse SetNetworkInterfaceDot1XConfiguration(SetNetworkInterfaceDot1XConfigurationRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
