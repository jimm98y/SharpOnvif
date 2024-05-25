using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Media2
{
    [DisableMustUnderstandValidation]
    public class Media2Base : Media2
    {
        public virtual AddConfigurationResponse AddConfiguration(AddConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Token")]
        public virtual string CreateMask(Mask Mask)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "OSDToken")]
        public virtual string CreateOSD(OSDConfiguration OSD)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Token")]
        public virtual CreateProfileResponse CreateProfile(CreateProfileRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteMask(string Token)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteOSD(string OSDToken)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteProfile(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetAnalyticsConfigurationsResponse GetAnalyticsConfigurations(GetAnalyticsConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual GetAudioDecoderConfigurationOptionsResponse GetAudioDecoderConfigurationOptions(GetAudioDecoderConfigurationOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetAudioDecoderConfigurationsResponse GetAudioDecoderConfigurations(GetAudioDecoderConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual GetAudioEncoderConfigurationOptionsResponse GetAudioEncoderConfigurationOptions(GetAudioEncoderConfigurationOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetAudioEncoderConfigurationsResponse GetAudioEncoderConfigurations(GetAudioEncoderConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual AudioOutputConfigurationOptions GetAudioOutputConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetAudioOutputConfigurationsResponse GetAudioOutputConfigurations(GetAudioOutputConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual AudioSourceConfigurationOptions GetAudioSourceConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetAudioSourceConfigurationsResponse GetAudioSourceConfigurations(GetAudioSourceConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual MaskOptions GetMaskOptions(string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Masks")]
        public virtual GetMasksResponse GetMasks(GetMasksRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual MetadataConfigurationOptions GetMetadataConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetMetadataConfigurationsResponse GetMetadataConfigurations(GetMetadataConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "OSDOptions")]
        public virtual OSDConfigurationOptions GetOSDOptions(string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "OSDs")]
        public virtual GetOSDsResponse GetOSDs(GetOSDsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Profiles")]
        public virtual GetProfilesResponse GetProfiles(GetProfilesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities2 GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Uri")]
        public virtual GetSnapshotUriResponse GetSnapshotUri(GetSnapshotUriRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Uri")]
        public virtual GetStreamUriResponse GetStreamUri(GetStreamUriRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual GetVideoEncoderConfigurationOptionsResponse GetVideoEncoderConfigurationOptions(GetVideoEncoderConfigurationOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetVideoEncoderConfigurationsResponse GetVideoEncoderConfigurations(GetVideoEncoderConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Info")]
        public virtual EncoderInstanceInfo GetVideoEncoderInstances(string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual VideoSourceConfigurationOptions GetVideoSourceConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetVideoSourceConfigurationsResponse GetVideoSourceConfigurations(GetVideoSourceConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "VideoSourceModes")]
        public virtual GetVideoSourceModesResponse GetVideoSourceModes(GetVideoSourceModesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual RemoveConfigurationResponse RemoveConfiguration(RemoveConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAudioDecoderConfiguration(AudioDecoderConfiguration Configuration)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAudioEncoderConfiguration(AudioEncoder2Configuration Configuration)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAudioOutputConfiguration(AudioOutputConfiguration Configuration)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAudioSourceConfiguration(AudioSourceConfiguration Configuration)
        {
            throw new NotImplementedException();
        }

        public virtual void SetMask(Mask Mask)
        {
            throw new NotImplementedException();
        }

        public virtual void SetMetadataConfiguration(MetadataConfiguration Configuration)
        {
            throw new NotImplementedException();
        }

        public virtual void SetOSD(OSDConfiguration OSD)
        {
            throw new NotImplementedException();
        }

        public virtual void SetSynchronizationPoint(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void SetVideoEncoderConfiguration(VideoEncoder2Configuration Configuration)
        {
            throw new NotImplementedException();
        }

        public virtual void SetVideoSourceConfiguration(VideoSourceConfiguration Configuration)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Reboot")]
        public virtual bool SetVideoSourceMode(string VideoSourceToken, string VideoSourceModeToken)
        {
            throw new NotImplementedException();
        }

        public virtual void StartMulticastStreaming(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void StopMulticastStreaming(string ProfileToken)
        {
            throw new NotImplementedException();
        }
    }
}
