using CoreWCF;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpOnvifCommon;
using SharpOnvifCommon.PTZ;
using SharpOnvifServer;
using SharpOnvifServer.Media;
using System.Collections.Generic;
using System.Linq;

namespace OnvifService.Onvif
{
    public class MediaImpl : MediaBase
    {
        public class MediaProfile
        {
            public string ProfileToken { get; set; } = "Profile_1";
            public string VideoSourceToken { get; set; } = "VideoSource_1";
            public string AudioSourceToken { get; set; } = "AudioSource_1";
            public string VideoEncoderToken { get; set; } = "VideoEncoder_1";
            public string AudioEncoderToken { get; set; } = "AudioEncoder_1";
            public string AudioOutputToken { get; set; } = "AudioOutput_1";
            public string AudioDecoderToken { get; set; } = "AudioDecoder_1";
            public int VideoWidth { get; set; } = 640;
            public int VideoHeight { get; set; } = 360;
            public int VideoFps { get; set; } = 25;
            public string VideoRtspUri { get; set; } = "rtsp://localhost:8554/";
            public string VideoSnapshotUri { get; set; } = null;
            public int AudioChannels { get; set; } = 1;
            public int AudioSampleBitrate { get; set; } = 44100;
            public string PtzToken { get; set; } = PTZImpl.PTZ_CONFIGURATION_TOKEN;
            public string PtzNodeToken { get; set; } = PTZImpl.PTZ_NODE_TOKEN;
        }

        public Dictionary<string, MediaProfile> Profiles { get; set; } = new Dictionary<string, MediaProfile>();

        // You can use https://github.com/jimm98y/SharpRealTimeStreaming to stream RTSP       
        private readonly IServer _server;
        private readonly ILogger<MediaImpl> _logger;
        private readonly IConfiguration _configuration;

        public MediaImpl(IServer server, ILogger<MediaImpl> logger, IConfiguration configuration)
        {
            _server = server;
            _logger = logger;
            _configuration = configuration;
            Profiles = _configuration.GetSection("MediaImpl:Profiles").Get<MediaProfile[]>()?.ToDictionary(x => x.ProfileToken, x => x);
        }

        public override GetAudioSourcesResponse GetAudioSources(GetAudioSourcesRequest request)
        {
            return new GetAudioSourcesResponse()
            {
                AudioSources = Profiles?.Values.Select(GetMyAudioSource).ToArray()
            };
        }

        public override GetProfilesResponse GetProfiles(GetProfilesRequest request)
        {
            return new GetProfilesResponse()
            {
                Profiles = Profiles?.Values.Select(CreateMyProfile).ToArray()
            };
        }

        [return: MessageParameter(Name = "Profile")]
        public override Profile GetProfile(string ProfileToken)
        {
            if (Profiles == null || !Profiles.ContainsKey(ProfileToken))
                OnvifErrors.ReturnSenderInvalidArg();

            return CreateMyProfile(Profiles[ProfileToken]);
        }

        public override MediaUri GetSnapshotUri(string ProfileToken)
        {
            if (Profiles == null || !Profiles.ContainsKey(ProfileToken))
                OnvifErrors.ReturnSenderInvalidArg();

            return new MediaUri()
            {
                Uri = string.IsNullOrEmpty(Profiles[ProfileToken].VideoSnapshotUri) ? OnvifHelpers.ChangeUriPath(OperationContext.Current.IncomingMessageProperties.Via, "/preview").ToString() : Profiles[ProfileToken].VideoSnapshotUri
            };
        }

        public override MediaUri GetStreamUri(StreamSetup StreamSetup, string ProfileToken)
        {
            if (Profiles == null || !Profiles.ContainsKey(ProfileToken))
                OnvifErrors.ReturnSenderInvalidArg();

            return new MediaUri()
            {
                Uri = Profiles[ProfileToken].VideoRtspUri,
            };
        }

        public override GetVideoSourcesResponse GetVideoSources(GetVideoSourcesRequest request)
        {
            return new GetVideoSourcesResponse()
            {
                VideoSources = Profiles?.Values.Select(CreateMyVideoSource).ToArray()
            };
        }

        [return: MessageParameter(Name = "Configuration")]
        public override VideoSourceConfiguration GetVideoSourceConfiguration(string ConfigurationToken)
        {
            if (ConfigurationToken == null || Profiles == null || Profiles.Values.FirstOrDefault(x => x.VideoSourceToken == ConfigurationToken) == null)
                OnvifErrors.ReturnSenderInvalidArg();

            return GetMyVideoSourceConfiguration(Profiles.Values.First(x => x.VideoSourceToken == ConfigurationToken));
        }

        [return: MessageParameter(Name = "Configuration")]
        public override VideoEncoderConfiguration GetVideoEncoderConfiguration(string ConfigurationToken)
        {
            if (ConfigurationToken == null || Profiles == null || Profiles.Values.FirstOrDefault(x => x.VideoEncoderToken == ConfigurationToken) == null)
                OnvifErrors.ReturnSenderInvalidArg();

            return GetMyVideoEncoderConfiguration(Profiles.Values.First(x => x.VideoEncoderToken == ConfigurationToken));
        }

        [return: MessageParameter(Name = "Options")]
        public override VideoEncoderConfigurationOptions GetVideoEncoderConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            if (Profiles == null || !Profiles.ContainsKey(ProfileToken) || Profiles.Values.FirstOrDefault(x => x.VideoEncoderToken == ConfigurationToken) == null)
                OnvifErrors.ReturnSenderInvalidArg();

            return new VideoEncoderConfigurationOptions()
            {
                // TODO: Update to match your video source
                H264 = new H264Options()
                {
                    GovLengthRange = new IntRange() { Min = 15, Max = 15 },
                    H264ProfilesSupported = new H264Profile[] { H264Profile.Main },
                    EncodingIntervalRange = new IntRange() { Min = 1, Max = 100 },
                    FrameRateRange = new IntRange() {  Min = 1, Max = 100 },
                    ResolutionsAvailable = new VideoResolution[] 
                    {
                        new VideoResolution() 
                        { 
                            Width = Profiles[ProfileToken].VideoWidth,
                            Height = Profiles[ProfileToken].VideoHeight
                        }
                    }
                },
                QualityRange = new IntRange() { Min = 1, Max = 100 },
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetAudioEncoderConfigurationsResponse GetAudioEncoderConfigurations(GetAudioEncoderConfigurationsRequest request)
        {
            return new GetAudioEncoderConfigurationsResponse()
            {
                Configurations = Profiles?.Values.Select(GetMyAudioEncoderConfiguration).ToArray() 
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetAudioSourceConfigurationsResponse GetAudioSourceConfigurations(GetAudioSourceConfigurationsRequest request)
        {
            return new GetAudioSourceConfigurationsResponse()
            {
                Configurations = Profiles?.Values.Select(GetMyAudioSourceConfiguration).ToArray()
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetVideoEncoderConfigurationsResponse GetVideoEncoderConfigurations(GetVideoEncoderConfigurationsRequest request)
        {
            return new GetVideoEncoderConfigurationsResponse()
            {
                Configurations = Profiles?.Values.Select(GetMyVideoEncoderConfiguration).ToArray()
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetVideoSourceConfigurationsResponse GetVideoSourceConfigurations(GetVideoSourceConfigurationsRequest request)
        {
            return new GetVideoSourceConfigurationsResponse()
            {
                Configurations = Profiles?.Values.Select(GetMyVideoSourceConfiguration).ToArray()
            };
        }

        [return: MessageParameter(Name = "Options")]
        public override VideoSourceConfigurationOptions GetVideoSourceConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            if (Profiles == null || !Profiles.ContainsKey(ProfileToken) || Profiles.Values.FirstOrDefault(x => x.VideoSourceToken == ConfigurationToken) == null)
                OnvifErrors.ReturnSenderInvalidArg();

            return new VideoSourceConfigurationOptions()
            {
                VideoSourceTokensAvailable = new string[] { Profiles[ProfileToken].VideoSourceToken },
                Extension = new VideoSourceConfigurationOptionsExtension()
                {
                    Rotate = new RotateOptions()
                    {
                        Reboot = true,
                        Mode = new RotateMode[] { RotateMode.ON, RotateMode.OFF, RotateMode.AUTO },
                        DegreeList = new int[] { 0, 90, 180, 270 },
                        RebootSpecified = true,
                    }
                },
                BoundsRange = new IntRectangleRange()
                {
                    XRange = new IntRange() { Min = 0, Max = Profiles[ProfileToken].VideoWidth },
                    YRange = new IntRange() { Min = 0, Max = Profiles[ProfileToken].VideoHeight },
                    WidthRange = new IntRange() { Min = 0, Max = Profiles[ProfileToken].VideoWidth },
                    HeightRange = new IntRange() { Min = 0, Max = Profiles[ProfileToken].VideoHeight }
                },
                MaximumNumberOfProfiles = 10,
                MaximumNumberOfProfilesSpecified = true,
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetCompatibleVideoEncoderConfigurationsResponse GetCompatibleVideoEncoderConfigurations(GetCompatibleVideoEncoderConfigurationsRequest request)
        {
            if (Profiles == null)
                OnvifErrors.ReturnSenderInvalidArg();

            return new GetCompatibleVideoEncoderConfigurationsResponse()
            {
                Configurations = Profiles.Values.Select(GetMyVideoEncoderConfiguration).ToArray()
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetCompatibleAudioDecoderConfigurationsResponse GetCompatibleAudioDecoderConfigurations(GetCompatibleAudioDecoderConfigurationsRequest request)
        {
            return new GetCompatibleAudioDecoderConfigurationsResponse()
            {
                Configurations = Profiles?.Values.Select(CreateMyAudioDecoderConfiguration).ToArray()
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetAudioOutputConfigurationsResponse GetAudioOutputConfigurations(GetAudioOutputConfigurationsRequest request)
        {
            return new GetAudioOutputConfigurationsResponse()
            {
                Configurations = Profiles.Values.Select(CreateMyAudioOutputConfiguration).ToArray()
            };
        }

        [return: MessageParameter(Name = "Options")]
        public override AudioEncoderConfigurationOptions GetAudioEncoderConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            return new AudioEncoderConfigurationOptions()
            {
                 Options = new AudioEncoderConfigurationOption[]
                 {
                     new AudioEncoderConfigurationOption()
                     {
                         BitrateList = new int[] { 32, 64, 128 },
                         Encoding = AudioEncoding.AAC,
                         SampleRateList = new int[] { 8000, 16000, 32000, 44100 },
                     }
                 }
            };
        }

        public override void SetVideoEncoderConfiguration(VideoEncoderConfiguration Configuration, bool ForcePersistence)
        {
            _logger.LogInformation("MediaImpl: SetVideoEncoderConfiguration");
        }

        public override void AddPTZConfiguration(string ProfileToken, string ConfigurationToken)
        {
            _logger.LogInformation("MediaImpl: AddPTZConfiguration");
        }

        public override void AddAudioOutputConfiguration(string ProfileToken, string ConfigurationToken)
        {
            _logger.LogInformation("MediaImpl: AddAudioOutputConfiguration");
        }

        public override void AddAudioDecoderConfiguration(string ProfileToken, string ConfigurationToken)
        {
            _logger.LogInformation("MediaImpl: AddAudioDecoderConfiguration");
        }

        private AudioDecoderConfiguration CreateMyAudioDecoderConfiguration(MediaProfile profile)
        {
            return new AudioDecoderConfiguration()
            {
                token = profile.AudioDecoderToken,
                Name = profile.AudioDecoderToken,
            };
        }

        private static Profile CreateMyProfile(MediaProfile profile)
        {
            return new Profile()
            {
                token = profile.ProfileToken,
                Name = profile.ProfileToken,
                VideoSourceConfiguration = GetMyVideoSourceConfiguration(profile),
                VideoEncoderConfiguration = GetMyVideoEncoderConfiguration(profile),
                AudioSourceConfiguration = GetMyAudioSourceConfiguration(profile),
                AudioEncoderConfiguration = GetMyAudioEncoderConfiguration(profile),
                PTZConfiguration = new PTZConfiguration()
                {
                    token = profile.PtzToken,
                    Name = profile.PtzToken,
                    NodeToken = profile.PtzNodeToken,
                    PanTiltLimits = new PanTiltLimits()
                    {
                        Range = new Space2DDescription()
                        {
                            XRange = new FloatRange() { Min = -1, Max = 1 },
                            YRange = new FloatRange() { Min = -1, Max = 1 },
                            URI = SpacesPanTilt.POSITION_GENERIC_SPACE
                        },
                    },
                    ZoomLimits = new ZoomLimits()
                    {
                        Range = new Space1DDescription()
                        {
                            XRange = new FloatRange() { Min = 0, Max = 1 },
                            URI = SpacesZoom.POSITION_GENERIC_SPACE
                        }
                    },
                    DefaultAbsolutePantTiltPositionSpace = SpacesPanTilt.POSITION_GENERIC_SPACE,
                    DefaultAbsoluteZoomPositionSpace = SpacesZoom.POSITION_GENERIC_SPACE,
                    DefaultRelativePanTiltTranslationSpace = SpacesPanTilt.TRANSLATION_GENERIC_SPACE,
                    DefaultRelativeZoomTranslationSpace = SpacesZoom.TRANSLATION_GENERIC_SPACE,
                    DefaultContinuousPanTiltVelocitySpace = SpacesPanTilt.VELOCITY_GENERIC_SPACE,
                    DefaultContinuousZoomVelocitySpace = SpacesZoom.VELOCITY_GENERIC_SPACE,
                    DefaultPTZTimeout = OnvifHelpers.GetTimeoutInSeconds(5),
                    UseCount = 1,
                }
            };
        }

        private static VideoSource CreateMyVideoSource(MediaProfile profile)
        {
            return new VideoSource()
            {
                token = profile.VideoSourceToken,
                Resolution = new VideoResolution()
                {
                    Width = profile.VideoWidth,
                    Height = profile.VideoHeight
                },
                Framerate = profile.VideoFps,
                Imaging = new ImagingSettings()
                {
                    Brightness = 100
                }
            };
        }

        private static VideoSourceConfiguration GetMyVideoSourceConfiguration(MediaProfile profile)
        {
            return new VideoSourceConfiguration()
            {
                token = profile.VideoSourceToken,
                Name = profile.VideoSourceToken,
                SourceToken = profile.VideoSourceToken,
                Bounds = new IntRectangle() 
                { 
                    x = 0,
                    y = 0,
                    width = profile.VideoWidth, 
                    height = profile.VideoHeight
                },
                UseCount = 1,
            };
        }

        // TODO: Update to match your video source
        private static VideoEncoderConfiguration GetMyVideoEncoderConfiguration(MediaProfile profile)
        {
            return new VideoEncoderConfiguration()
            {
                token = profile.VideoEncoderToken,
                Name = profile.VideoEncoderToken,
                UseCount = 1,
                Encoding = VideoEncoding.H264,
                Resolution = new VideoResolution()
                { 
                    Width = profile.VideoWidth,
                    Height = profile.VideoHeight
                },
                Quality = 5.0f,
                H264 = new H264Configuration()
                {
                    GovLength = 15,
                    H264Profile = H264Profile.Main
                }
            };
        }

        private static AudioEncoderConfiguration GetMyAudioEncoderConfiguration(MediaProfile profile)
        {
            return new AudioEncoderConfiguration()
            {
                token = profile.AudioEncoderToken,
                Name = profile.AudioEncoderToken,
                Encoding = AudioEncoding.AAC,
                SampleRate = profile.AudioSampleBitrate,
                UseCount = 1
            };
        }

        private static AudioSourceConfiguration GetMyAudioSourceConfiguration(MediaProfile profile)
        {
            return new AudioSourceConfiguration()
            {
                token = profile.AudioSourceToken,
                Name = profile.AudioSourceToken,
                SourceToken = profile.AudioSourceToken,
                UseCount = 1
            };
        }

        private static AudioSource GetMyAudioSource(MediaProfile profile)
        {
            return new AudioSource()
            {
                token = profile.AudioSourceToken,
                Channels = profile.AudioChannels,
            };
        }

        private static AudioOutputConfiguration CreateMyAudioOutputConfiguration(MediaProfile profile)
        {
            return new AudioOutputConfiguration()
            {
                token = profile.AudioOutputToken,
                Name = profile.AudioOutputToken,
                OutputToken = profile.AudioOutputToken,
            };
        }
    }
}
