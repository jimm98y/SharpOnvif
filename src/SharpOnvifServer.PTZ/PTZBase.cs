using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.PTZ
{
    [DisableMustUnderstandValidation]
    public class PTZBase : PTZ
    {
        public virtual void AbsoluteMove(string ProfileToken, PTZVector Position, PTZSpeed Speed)
        {
            throw new NotImplementedException();
        }

        public virtual ContinuousMoveResponse ContinuousMove(ContinuousMoveRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PresetTourToken")]
        public virtual string CreatePresetTour(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void GeoMove(string ProfileToken, GeoLocation Target, PTZSpeed Speed, float AreaHeight, float AreaWidth)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PTZConfiguration")]
        public virtual GetCompatibleConfigurationsResponse GetCompatibleConfigurations(GetCompatibleConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PTZConfiguration")]
        public virtual PTZConfiguration GetConfiguration(string PTZConfigurationToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PTZConfigurationOptions")]
        public virtual PTZConfigurationOptions GetConfigurationOptions(string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PTZConfiguration")]
        public virtual GetConfigurationsResponse GetConfigurations(GetConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PTZNode")]
        public virtual PTZNode GetNode(string NodeToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PTZNode")]
        public virtual GetNodesResponse GetNodes(GetNodesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Preset")]
        public virtual GetPresetsResponse GetPresets(GetPresetsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PresetTour")]
        public virtual PresetTour GetPresetTour(string ProfileToken, string PresetTourToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual PTZPresetTourOptions GetPresetTourOptions(string ProfileToken, string PresetTourToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PresetTour")]
        public virtual GetPresetToursResponse GetPresetTours(GetPresetToursRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PTZStatus")]
        public virtual PTZStatus GetStatus(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void GotoHomePosition(string ProfileToken, PTZSpeed Speed)
        {
            throw new NotImplementedException();
        }

        public virtual void GotoPreset(string ProfileToken, string PresetToken, PTZSpeed Speed)
        {
            throw new NotImplementedException();
        }

        public virtual void ModifyPresetTour(string ProfileToken, PresetTour PresetTour)
        {
            throw new NotImplementedException();
        }

        public virtual MoveAndStartTrackingResponse MoveAndStartTracking(MoveAndStartTrackingRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void OperatePresetTour(string ProfileToken, string PresetTourToken, PTZPresetTourOperation Operation)
        {
            throw new NotImplementedException();
        }

        public virtual void RelativeMove(string ProfileToken, PTZVector Translation, PTZSpeed Speed)
        {
            throw new NotImplementedException();
        }

        public virtual void RemovePreset(string ProfileToken, string PresetToken)
        {
            throw new NotImplementedException();
        }

        public virtual void RemovePresetTour(string ProfileToken, string PresetTourToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "AuxiliaryResponse")]
        public virtual string SendAuxiliaryCommand(string ProfileToken, string AuxiliaryData)
        {
            throw new NotImplementedException();
        }

        public virtual void SetConfiguration(PTZConfiguration PTZConfiguration, bool ForcePersistence)
        {
            throw new NotImplementedException();
        }

        public virtual void SetHomePosition(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual SetPresetResponse SetPreset(SetPresetRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void Stop(string ProfileToken, bool PanTilt, bool Zoom)
        {
            throw new NotImplementedException();
        }
    }
}
