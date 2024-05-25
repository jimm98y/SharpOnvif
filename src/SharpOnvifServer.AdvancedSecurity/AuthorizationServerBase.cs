using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.AdvancedSecurity
{
    [DisableMustUnderstandValidation]
    public class AuthorizationServerBase : AuthorizationServer
    {
        [return: MessageParameter(Name = "Token")]
        public virtual string CreateAuthorizationServerConfiguration(AuthorizationServerConfigurationData Configuration)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteAuthorizationServerConfiguration(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual GetAuthorizationServerConfigurationsResponse GetAuthorizationServerConfigurations(GetAuthorizationServerConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAuthorizationServerConfiguration(AuthorizationServerConfiguration Configuration)
        {
            throw new NotImplementedException();
        }
    }
}
