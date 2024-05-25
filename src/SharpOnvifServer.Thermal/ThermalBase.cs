using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Thermal
{
    [DisableMustUnderstandValidation]
    public class ThermalBase : ThermalPort
    {
        [return: MessageParameter(Name = "Configuration")]
        public virtual Configuration GetConfiguration(string VideoSourceToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ConfigurationOptions")]
        public virtual ConfigurationOptions GetConfigurationOptions(string VideoSourceToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetConfigurationsResponse GetConfigurations(GetConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual RadiometryConfiguration GetRadiometryConfiguration(string VideoSourceToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ConfigurationOptions")]
        public virtual RadiometryConfigurationOptions GetRadiometryConfigurationOptions(string VideoSourceToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        public virtual void SetConfiguration(string VideoSourceToken, Configuration Configuration)
        {
            throw new NotImplementedException();
        }

        public virtual void SetRadiometryConfiguration(string VideoSourceToken, RadiometryConfiguration Configuration)
        {
            throw new NotImplementedException();
        }
    }
}
