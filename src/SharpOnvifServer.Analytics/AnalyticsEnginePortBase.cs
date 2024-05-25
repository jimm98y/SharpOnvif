using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Analytics
{
    [DisableMustUnderstandValidation]
    public class AnalyticsEnginePortBase : AnalyticsEnginePort
    {
        public virtual CreateAnalyticsModulesResponse CreateAnalyticsModules(CreateAnalyticsModulesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteAnalyticsModulesResponse DeleteAnalyticsModules(DeleteAnalyticsModulesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual GetAnalyticsModuleOptionsResponse GetAnalyticsModuleOptions(GetAnalyticsModuleOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "AnalyticsModule")]
        public virtual GetAnalyticsModulesResponse GetAnalyticsModules(GetAnalyticsModulesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SupportedAnalyticsModules")]
        public virtual SupportedAnalyticsModules GetSupportedAnalyticsModules(string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "AnalyticsModule")]
        public virtual GetSupportedMetadataResponse GetSupportedMetadata(GetSupportedMetadataRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual ModifyAnalyticsModulesResponse ModifyAnalyticsModules(ModifyAnalyticsModulesRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
