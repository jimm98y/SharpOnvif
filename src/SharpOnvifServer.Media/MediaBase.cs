using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Media
{
    [DisableMustUnderstandValidation]
    public class MediaBase : Media
    {
        public virtual void AddAudioDecoderConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        public virtual void AddAudioEncoderConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        public virtual void AddAudioOutputConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        public virtual void AddAudioSourceConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        public virtual void AddMetadataConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        public virtual void AddPTZConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        public virtual void AddVideoAnalyticsConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        public virtual void AddVideoEncoderConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        public virtual void AddVideoSourceConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        public virtual CreateOSDResponse CreateOSD(CreateOSDRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Profile")]
        public virtual Profile CreateProfile(string Name, string Token)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteOSDResponse DeleteOSD(DeleteOSDRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteProfile(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual AudioDecoderConfiguration GetAudioDecoderConfiguration(string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual AudioDecoderConfigurationOptions GetAudioDecoderConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetAudioDecoderConfigurationsResponse GetAudioDecoderConfigurations(GetAudioDecoderConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual AudioEncoderConfiguration GetAudioEncoderConfiguration(string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual AudioEncoderConfigurationOptions GetAudioEncoderConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetAudioEncoderConfigurationsResponse GetAudioEncoderConfigurations(GetAudioEncoderConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual AudioOutputConfiguration GetAudioOutputConfiguration(string ConfigurationToken)
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

        [return: MessageParameter(Name = "AudioOutputs")]
        public virtual GetAudioOutputsResponse GetAudioOutputs(GetAudioOutputsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual AudioSourceConfiguration GetAudioSourceConfiguration(string ConfigurationToken)
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

        [return: MessageParameter(Name = "AudioSources")]
        public virtual GetAudioSourcesResponse GetAudioSources(GetAudioSourcesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleAudioDecoderConfigurationsResponse GetCompatibleAudioDecoderConfigurations(GetCompatibleAudioDecoderConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleAudioEncoderConfigurationsResponse GetCompatibleAudioEncoderConfigurations(GetCompatibleAudioEncoderConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleAudioOutputConfigurationsResponse GetCompatibleAudioOutputConfigurations(GetCompatibleAudioOutputConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleAudioSourceConfigurationsResponse GetCompatibleAudioSourceConfigurations(GetCompatibleAudioSourceConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleMetadataConfigurationsResponse GetCompatibleMetadataConfigurations(GetCompatibleMetadataConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleVideoAnalyticsConfigurationsResponse GetCompatibleVideoAnalyticsConfigurations(GetCompatibleVideoAnalyticsConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleVideoEncoderConfigurationsResponse GetCompatibleVideoEncoderConfigurations(GetCompatibleVideoEncoderConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleVideoSourceConfigurationsResponse GetCompatibleVideoSourceConfigurations(GetCompatibleVideoSourceConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetGuaranteedNumberOfVideoEncoderInstancesResponse GetGuaranteedNumberOfVideoEncoderInstances(GetGuaranteedNumberOfVideoEncoderInstancesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual MetadataConfiguration GetMetadataConfiguration(string ConfigurationToken)
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

        public virtual GetOSDResponse GetOSD(GetOSDRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetOSDOptionsResponse GetOSDOptions(GetOSDOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "OSDs")]
        public virtual GetOSDsResponse GetOSDs(GetOSDsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Profile")]
        public virtual Profile GetProfile(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Profiles")]
        public virtual GetProfilesResponse GetProfiles(GetProfilesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "MediaUri")]
        public virtual MediaUri GetSnapshotUri(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "MediaUri")]
        public virtual MediaUri GetStreamUri(StreamSetup StreamSetup, string ProfileToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual VideoAnalyticsConfiguration GetVideoAnalyticsConfiguration(string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetVideoAnalyticsConfigurationsResponse GetVideoAnalyticsConfigurations(GetVideoAnalyticsConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual VideoEncoderConfiguration GetVideoEncoderConfiguration(string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual VideoEncoderConfigurationOptions GetVideoEncoderConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetVideoEncoderConfigurationsResponse GetVideoEncoderConfigurations(GetVideoEncoderConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual VideoSourceConfiguration GetVideoSourceConfiguration(string ConfigurationToken)
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

        [return: MessageParameter(Name = "VideoSources")]
        public virtual GetVideoSourcesResponse GetVideoSources(GetVideoSourcesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void RemoveAudioDecoderConfiguration(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void RemoveAudioEncoderConfiguration(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void RemoveAudioOutputConfiguration(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void RemoveAudioSourceConfiguration(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void RemoveMetadataConfiguration(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void RemovePTZConfiguration(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void RemoveVideoAnalyticsConfiguration(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void RemoveVideoEncoderConfiguration(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void RemoveVideoSourceConfiguration(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAudioDecoderConfiguration(AudioDecoderConfiguration Configuration, bool ForcePersistence)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAudioEncoderConfiguration(AudioEncoderConfiguration Configuration, bool ForcePersistence)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAudioOutputConfiguration(AudioOutputConfiguration Configuration, bool ForcePersistence)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAudioSourceConfiguration(AudioSourceConfiguration Configuration, bool ForcePersistence)
        {
            throw new NotImplementedException();
        }

        public virtual void SetMetadataConfiguration(MetadataConfiguration Configuration, bool ForcePersistence)
        {
            throw new NotImplementedException();
        }

        public virtual SetOSDResponse SetOSD(SetOSDRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetSynchronizationPoint(string ProfileToken)
        {
            throw new NotImplementedException();
        }

        public virtual void SetVideoAnalyticsConfiguration(VideoAnalyticsConfiguration Configuration, bool ForcePersistence)
        {
            throw new NotImplementedException();
        }

        public virtual void SetVideoEncoderConfiguration(VideoEncoderConfiguration Configuration, bool ForcePersistence)
        {
            throw new NotImplementedException();
        }

        public virtual void SetVideoSourceConfiguration(VideoSourceConfiguration Configuration, bool ForcePersistence)
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
