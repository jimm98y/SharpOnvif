using CoreWCF;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpOnvifCommon;
using SharpOnvifCommon.PTZ;
using SharpOnvifServer;
using SharpOnvifServer.Media;

namespace OnvifService.Onvif
{
    public class MediaImpl : MediaBase
    {
        public const string VIDEO_SOURCE_TOKEN = "VideoSource_1";
        public const string AUDIO_SOURCE_TOKEN = "AudioSource_1";
        public const string PROFILE_TOKEN = "Profile_1";

        // You can use https://github.com/jimm98y/SharpRealTimeStreaming to stream RTSP
        private int VideoWidth { get; set; } = 640;
        private int VideoHeight { get; set; } = 360;
        private int VideoFps { get; set; } = 25;
        private string VideoRtspUri { get; set; } = "rtsp://localhost:8554/";
        private string VideoSnapshotUri { get; set; } = null;
        private int AudioChannels { get; set; } = 1;
        private int AudioSampleBitrate { get; set; } = 44100;

        private readonly IServer _server;
        private readonly ILogger<MediaImpl> _logger;
        private readonly IConfiguration _configuration;

        public MediaImpl(IServer server, ILogger<MediaImpl> logger, IConfiguration configuration)
        {
            _server = server;
            _logger = logger;
            _configuration = configuration;

            VideoWidth = _configuration.GetValue<int>("MediaImpl:VideoWidth");
            VideoHeight = _configuration.GetValue<int>("MediaImpl:VideoHeight");
            VideoFps = _configuration.GetValue<int>("MediaImpl:VideoFps");
            VideoRtspUri = _configuration.GetValue<string>("MediaImpl:VideoRtspUri");
            VideoSnapshotUri = _configuration.GetValue<string>("MediaImpl:VideoSnapshotUri");

            AudioChannels = _configuration.GetValue<int>("MediaImpl:AudioChannels");
            AudioSampleBitrate = _configuration.GetValue<int>("MediaImpl:AudioSampleBitrate");
        }

        public override GetAudioSourcesResponse GetAudioSources(GetAudioSourcesRequest request)
        {
            return new GetAudioSourcesResponse()
            {
                AudioSources = new AudioSource[]
                { 
                    new AudioSource()
                    {
                        token = AUDIO_SOURCE_TOKEN,
                        Channels = AudioChannels,
                    }
                }
            };
        }

        public override GetProfilesResponse GetProfiles(GetProfilesRequest request)
        {
            return new GetProfilesResponse()
            {
                Profiles = new Profile[]
                {
                    GetMyProfile()
                }
            };
        }

        [return: MessageParameter(Name = "Profile")]
        public override Profile GetProfile(string ProfileToken)
        {
            return GetMyProfile();
        }

        public override MediaUri GetSnapshotUri(string ProfileToken)
        {
            return new MediaUri()
            {
                Uri = string.IsNullOrEmpty(VideoSnapshotUri) ? $"{_server.GetHttpEndpoint()}/preview" : VideoSnapshotUri
            };
        }

        public override MediaUri GetStreamUri(StreamSetup StreamSetup, string ProfileToken)
        {
            return new MediaUri()
            {
                Uri = VideoRtspUri,
            };
        }

        public override GetVideoSourcesResponse GetVideoSources(GetVideoSourcesRequest request)
        {
            return new GetVideoSourcesResponse()
            {
                // TODO: Update to match your video source
                VideoSources = new VideoSource[]
                {
                    new VideoSource()
                    {
                        token = VIDEO_SOURCE_TOKEN,
                        Resolution = new VideoResolution()
                        {
                            Width = VideoWidth,
                            Height = VideoHeight
                        },
                        Framerate = VideoFps,
                        Imaging = new ImagingSettings()
                        {
                            Brightness = 100
                        }
                    }
                }
            };
        }

        [return: MessageParameter(Name = "Configuration")]
        public override VideoSourceConfiguration GetVideoSourceConfiguration(string ConfigurationToken)
        {
            return GetMyVideoSourceConfiguration();
        }

        [return: MessageParameter(Name = "Configuration")]
        public override VideoEncoderConfiguration GetVideoEncoderConfiguration(string ConfigurationToken)
        {
            return GetMyVideoEncoderConfiguration();
        }

        [return: MessageParameter(Name = "Options")]
        public override VideoEncoderConfigurationOptions GetVideoEncoderConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
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
                            Width = VideoWidth,
                            Height = VideoHeight
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
                Configurations = new AudioEncoderConfiguration[]
                {
                    GetMyAudioEncoderConfiguration()
                }
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetAudioSourceConfigurationsResponse GetAudioSourceConfigurations(GetAudioSourceConfigurationsRequest request)
        {
            return new GetAudioSourceConfigurationsResponse()
            {
                Configurations = new AudioSourceConfiguration[]
                {
                    GetMyAudioSourceConfiguration()
                }
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetVideoEncoderConfigurationsResponse GetVideoEncoderConfigurations(GetVideoEncoderConfigurationsRequest request)
        {
            return new GetVideoEncoderConfigurationsResponse()
            {
                Configurations = new VideoEncoderConfiguration[]
                {
                    GetMyVideoEncoderConfiguration()
                }
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetVideoSourceConfigurationsResponse GetVideoSourceConfigurations(GetVideoSourceConfigurationsRequest request)
        {
            return new GetVideoSourceConfigurationsResponse()
            {
                Configurations = new VideoSourceConfiguration[]
                {
                    GetMyVideoSourceConfiguration()
                }
            };
        }

        [return: MessageParameter(Name = "Options")]
        public override VideoSourceConfigurationOptions GetVideoSourceConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            return new VideoSourceConfigurationOptions()
            {
                VideoSourceTokensAvailable = new string[] { VIDEO_SOURCE_TOKEN },
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
                    XRange = new IntRange() { Min = 0, Max = VideoWidth },
                    YRange = new IntRange() { Min = 0, Max = VideoHeight },
                    WidthRange = new IntRange() { Min = 0, Max = VideoWidth },
                    HeightRange = new IntRange() { Min = 0, Max = VideoHeight }
                },
                MaximumNumberOfProfiles = 10,
                MaximumNumberOfProfilesSpecified = true,
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetCompatibleVideoEncoderConfigurationsResponse GetCompatibleVideoEncoderConfigurations(GetCompatibleVideoEncoderConfigurationsRequest request)
        {
            return new GetCompatibleVideoEncoderConfigurationsResponse()
            {
                Configurations = new VideoEncoderConfiguration[]
                {
                    GetMyVideoEncoderConfiguration()
                }
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetCompatibleAudioDecoderConfigurationsResponse GetCompatibleAudioDecoderConfigurations(GetCompatibleAudioDecoderConfigurationsRequest request)
        {
            return new GetCompatibleAudioDecoderConfigurationsResponse()
            {
                Configurations = new AudioDecoderConfiguration[]
                {
                    new AudioDecoderConfiguration()
                    {
                        Name = "AudioDecoder_1",
                        token = "AudioDecoder_1"
                    }
                }
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public override GetAudioOutputConfigurationsResponse GetAudioOutputConfigurations(GetAudioOutputConfigurationsRequest request)
        {
            return new GetAudioOutputConfigurationsResponse()
            {
                Configurations = new AudioOutputConfiguration[]
                 {
                     new AudioOutputConfiguration()
                     {
                          token = "AudioOutput_1",
                          Name = "AudioOutput_1",
                          OutputToken = "AudioOutput_1"
                     }
                 }
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

        private Profile GetMyProfile()
        {
            return new Profile()
            {
                Name = "mainStream",
                token = PROFILE_TOKEN,
                VideoSourceConfiguration = GetMyVideoSourceConfiguration(),
                VideoEncoderConfiguration = GetMyVideoEncoderConfiguration(),
                AudioSourceConfiguration = GetMyAudioSourceConfiguration(),
                AudioEncoderConfiguration = GetMyAudioEncoderConfiguration(),
                PTZConfiguration = new PTZConfiguration()
                {
                    NodeToken = PTZImpl.PTZ_NODE_TOKEN,
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
                    Name = "PTZConfig",
                    UseCount = 1,
                }
            };
        }

        private VideoSourceConfiguration GetMyVideoSourceConfiguration()
        {
            return new VideoSourceConfiguration()
            {
                SourceToken = VIDEO_SOURCE_TOKEN,
                Name = "VideoSourceConfig",
                Bounds = new IntRectangle() 
                { 
                    x = 0,
                    y = 0,
                    width = VideoWidth, 
                    height = VideoHeight 
                },
                UseCount = 1
            };
        }

        // TODO: Update to match your video source
        private VideoEncoderConfiguration GetMyVideoEncoderConfiguration()
        {
            return new VideoEncoderConfiguration()
            {
                Name = "VideoEncoder_1",
                UseCount = 1,
                Encoding = VideoEncoding.H264,
                Resolution = new VideoResolution()
                { 
                    Width = VideoWidth,
                    Height = VideoHeight
                },
                Quality = 5.0f,
                H264 = new H264Configuration()
                {
                    GovLength = 15,
                    H264Profile = H264Profile.Main
                }
            };
        }

        private AudioEncoderConfiguration GetMyAudioEncoderConfiguration()
        {
            return new AudioEncoderConfiguration()
            {
                Encoding = AudioEncoding.AAC,
                SampleRate = AudioSampleBitrate,
                Name = "AudioSource_1",
                UseCount = 1
            };
        }

        private AudioSourceConfiguration GetMyAudioSourceConfiguration()
        {
            return new AudioSourceConfiguration()
            {
                SourceToken = AUDIO_SOURCE_TOKEN,
                Name = "AudioSourceConfig",
                UseCount = 1
            };
        }
    }
}
