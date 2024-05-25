using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Provisioning
{
    [DisableMustUnderstandValidation]
    public class ProvisioningBase : ProvisioningService
    {
        public virtual FocusMoveResponse FocusMove(FocusMoveRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Usage")]
        public virtual Usage GetUsage(string VideoSource)
        {
            throw new NotImplementedException();
        }

        public virtual PanMoveResponse PanMove(PanMoveRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual RollMoveResponse RollMove(RollMoveRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void Stop(string VideoSource)
        {
            throw new NotImplementedException();
        }

        public virtual TiltMoveResponse TiltMove(TiltMoveRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual ZoomMoveResponse ZoomMove(ZoomMoveRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
