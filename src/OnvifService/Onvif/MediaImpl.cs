using CoreWCF;
using Microsoft.AspNetCore.Hosting.Server;
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

        // TODO: Update to match your video source
        // You can use https://github.com/jimm98y/SharpRealTimeStreaming to stream RTSP
        private const int VIDEO_WIDTH = 640;
        private const int VIDEO_HEIGHT = 360;
        private const int VIDEO_FPS = 25;
        private const string VIDEO_RTSP_URI = "rtsp://localhost:8554/";

        private const int AUDIO_CHANNELS = 1;
        private const int AUDIO_SAMPLE_RATE = 44100;

        private readonly IServer _server;

        public MediaImpl(IServer server)
        {
            _server = server;
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
                        Channels = AUDIO_CHANNELS,
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
                Uri = $"{_server.GetHttpEndpoint()}/preview"
            };
        }

        public override MediaUri GetStreamUri(StreamSetup StreamSetup, string ProfileToken)
        {
            return new MediaUri()
            {
                Uri = VIDEO_RTSP_URI,
            };
        }

        public override GetVideoSourcesResponse GetVideoSources(GetVideoSourcesRequest request)
        {
            return new GetVideoSourcesResponse()
            {
                VideoSources = new VideoSource[]
                {
                    new VideoSource()
                    {
                        token = VIDEO_SOURCE_TOKEN,
                        Resolution = new VideoResolution()
                        {
                            Width = VIDEO_WIDTH,
                            Height = VIDEO_HEIGHT
                        },
                        Framerate = VIDEO_FPS,
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
                            Width = VIDEO_WIDTH,
                            Height = VIDEO_HEIGHT
                        }
                    }
                },
                QualityRange = new IntRange() { Min = 1, Max = 100 },
            };
        }

        // TODO: Update to match your video source
        private static Profile GetMyProfile()
        {
            return new Profile()
            {
                Name = "mainStream",
                token = PROFILE_TOKEN,
                VideoSourceConfiguration = GetMyVideoSourceConfiguration(),
                VideoEncoderConfiguration = GetMyVideoEncoderConfiguration(),
                AudioSourceConfiguration = GetMyAudioSourceConfiguration(),
                AudioEncoderConfiguration = GetMyAudioEncoderConfiguration(),
                PTZConfiguration = GetMyPtzConfiguration(),
            };
        }

        // TODO: Update to match your video source
        private static PTZConfiguration GetMyPtzConfiguration()
        {
            return new PTZConfiguration()
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
                DefaultPTZTimeout = "PT5S",
                Name = "PTZConfig",
                UseCount = 1,
            };
        }

        // TODO: Update to match your video source
        private static VideoSourceConfiguration GetMyVideoSourceConfiguration()
        {
            return new VideoSourceConfiguration()
            {
                SourceToken = VIDEO_SOURCE_TOKEN,
                Name = "VideoSourceConfig",
                Bounds = new IntRectangle() { x = 0, y = 0, width = VIDEO_WIDTH, height = VIDEO_HEIGHT },
                UseCount = 1
            };
        }

        // TODO: Update to match your video source
        private static VideoEncoderConfiguration GetMyVideoEncoderConfiguration()
        {
            return new VideoEncoderConfiguration()
            {
                Name = "VideoEncoder_1",
                UseCount = 1,
                Encoding = VideoEncoding.H264,
                Resolution = new VideoResolution() { Width = VIDEO_WIDTH, Height = VIDEO_HEIGHT },
                Quality = 5.0f,
                H264 = new H264Configuration()
                {
                    GovLength = 15,
                    H264Profile = H264Profile.Main
                }
            };
        }

        private static AudioEncoderConfiguration GetMyAudioEncoderConfiguration()
        {
            return new AudioEncoderConfiguration()
            {
                Encoding = AudioEncoding.AAC,
                SampleRate = AUDIO_SAMPLE_RATE,
                Name = "AudioSource_1",
                UseCount = 1
            };
        }

        private static AudioSourceConfiguration GetMyAudioSourceConfiguration()
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
