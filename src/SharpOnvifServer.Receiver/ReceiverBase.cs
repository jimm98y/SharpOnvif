using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Receiver
{
    [DisableMustUnderstandValidation]
    public class ReceiverBase : ReceiverPort
    {
        public virtual void ConfigureReceiver(string ReceiverToken, ReceiverConfiguration Configuration)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Receiver")]
        public virtual Receiver CreateReceiver(ReceiverConfiguration Configuration)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteReceiver(string ReceiverToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Receiver")]
        public virtual Receiver GetReceiver(string ReceiverToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Receivers")]
        public virtual GetReceiversResponse GetReceivers(GetReceiversRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ReceiverState")]
        public virtual ReceiverStateInformation GetReceiverState(string ReceiverToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        public virtual void SetReceiverMode(string ReceiverToken, ReceiverMode Mode)
        {
            throw new NotImplementedException();
        }
    }
}
