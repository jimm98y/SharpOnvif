using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Imaging
{
    [DisableMustUnderstandValidation]
    public class ImagingPortBase : ImagingPort
    {
        [return: MessageParameter(Name = "Preset")]
        public virtual ImagingPreset GetCurrentPreset(string VideoSourceToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ImagingSettings")]
        public virtual ImagingSettings20 GetImagingSettings(string VideoSourceToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "MoveOptions")]
        public virtual MoveOptions20 GetMoveOptions(string VideoSourceToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ImagingOptions")]
        public virtual ImagingOptions20 GetOptions(string VideoSourceToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Preset")]
        public virtual GetPresetsResponse GetPresets(GetPresetsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Status")]
        public virtual ImagingStatus20 GetStatus(string VideoSourceToken)
        {
            throw new NotImplementedException();
        }

        public virtual void Move(string VideoSourceToken, FocusMove Focus)
        {
            throw new NotImplementedException();
        }

        public virtual void SetCurrentPreset(string VideoSourceToken, string PresetToken)
        {
            throw new NotImplementedException();
        }

        public virtual void SetImagingSettings(string VideoSourceToken, ImagingSettings20 ImagingSettings, bool ForcePersistence)
        {
            throw new NotImplementedException();
        }

        public virtual void Stop(string VideoSourceToken)
        {
            throw new NotImplementedException();
        }
    }
}
