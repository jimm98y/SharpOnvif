using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.AdvancedSecurity
{
    [DisableMustUnderstandValidation]
    public class JWTBase : JWT
    {
        [return: MessageParameter(Name = "Configuration")]
        public virtual JWTConfiguration GetJWTConfiguration()
        {
            throw new NotImplementedException();
        }

        public virtual void SetJWTConfiguration(JWTConfiguration Configuration)
        {
            throw new NotImplementedException();
        }
    }
}
