using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.AdvancedSecurity
{
    [DisableMustUnderstandValidation]
    public class AdvancedSecurityServiceBase : AdvancedSecurityService
    {
        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }
    }
}
