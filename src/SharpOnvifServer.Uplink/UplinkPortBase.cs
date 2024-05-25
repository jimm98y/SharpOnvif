using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Uplink
{
    [DisableMustUnderstandValidation]
    public class UplinkPortBase : UplinkPort
    {
        public virtual DeleteUplinkResponse DeleteUplink(DeleteUplinkRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual GetUplinksResponse GetUplinks(GetUplinksRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetUplink(Configuration Configuration)
        {
            throw new NotImplementedException();
        }
    }
}
