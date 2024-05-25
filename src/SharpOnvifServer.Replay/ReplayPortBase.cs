using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Replay
{
    [DisableMustUnderstandValidation]
    public class ReplayPortBase : ReplayPort
    {
        [return: MessageParameter(Name = "Configuration")]
        public virtual ReplayConfiguration GetReplayConfiguration()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Uri")]
        public virtual GetReplayUriResponse GetReplayUri(GetReplayUriRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        public virtual void SetReplayConfiguration(ReplayConfiguration Configuration)
        {
            throw new NotImplementedException();
        }
    }
}
