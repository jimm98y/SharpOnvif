using CoreWCF;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using SharpOnvifCommon;
using SharpOnvifCommon.PTZ;
using SharpOnvifServer.PTZ;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OnvifService.Onvif
{
    public class PTZImpl : PTZBase
    {
        public const string PTZ_CONFIGURATION_TOKEN = "PTZ_1";
        public const string PTZ_NODE_TOKEN = "PTZ_Node_1";

        private IServer server;
        private readonly ILogger<PTZImpl> _logger;

        public float Pan { get; set; } = 0.5f;
        public float Tilt { get; set; } = 0.5f;
        public float Zoom { get; set; } = 0.5f;

        public MoveStatus PanTiltStatus { get; set; } = MoveStatus.IDLE;
        public MoveStatus ZoomStatus { get; set; } = MoveStatus.IDLE;

        private Dictionary<string, PTZPreset> Presets { get; } = new Dictionary<string, PTZPreset>();

        public PTZImpl(IServer server, ILogger<PTZImpl> logger)
        {
            this.server = server;
            this._logger = logger;
        }

        [return: MessageParameter(Name = "Capabilities")]
        public override Capabilities GetServiceCapabilities()
        {
            return new Capabilities()
            {
                EFlip = true,
                EFlipSpecified = true,
                Reverse = true,
                ReverseSpecified = true,
                GetCompatibleConfigurations = true,
                GetCompatibleConfigurationsSpecified = true,
                MoveStatus = true,
                MoveStatusSpecified = true,
                StatusPosition = true,
                StatusPositionSpecified = true,
            };
        }

        [return: MessageParameter(Name = "PTZStatus")]
        public override PTZStatus GetStatus(string ProfileToken)
        {
            return new PTZStatus()
            {
                MoveStatus = new PTZMoveStatus()
                {
                    PanTilt = PanTiltStatus,
                    Zoom = ZoomStatus
                },
                Position = new PTZVector()
                {
                    PanTilt = new Vector2D()
                    {
                        x = Pan,
                        y = Tilt
                    },
                    Zoom = new Vector1D()
                    {
                        x = Zoom
                    }
                },
                UtcTime = DateTime.UtcNow
            };
        }

        public override void AbsoluteMove(string ProfileToken, PTZVector Position, PTZSpeed Speed)
        {
            if(Position != null)
            {
                if (Position.PanTilt != null)
                {
                    this.Pan = Position.PanTilt.x;
                    this.Tilt = Position.PanTilt.y;
                    _logger.LogInformation($"PTZ: AbsoluteMove: pan: {Position.PanTilt.x}, tilt: {Position.PanTilt.y}");
                }

                if (Position.Zoom != null)
                {
                    this.Zoom = Position.Zoom.x;
                    _logger.LogInformation($"PTZ: AbsoluteMove: zoom: {Position.Zoom.x}");
                }
            }
        }

        public override void RelativeMove(string ProfileToken, PTZVector Translation, PTZSpeed Speed)
        {
            if (Translation != null)
            {
                if (Translation.PanTilt != null)
                {
                    this.Pan += Translation.PanTilt.x;
                    this.Tilt += Translation.PanTilt.y;
                    _logger.LogInformation($"PTZ: RelativeMove: panDelta: {Translation.PanTilt.x}, tiltDelta: {Translation.PanTilt.y}");
                }

                if (Translation.Zoom != null)
                {
                    this.Zoom += Translation.Zoom.x;
                    _logger.LogInformation($"PTZ: RelativeMove: zoomDelta: {Translation.Zoom.x}");
                }
            }
        }

        public override void Stop(string ProfileToken, bool PanTilt, bool Zoom)
        {
            _logger.LogInformation($"PTZ: Stop");
        }

        public override ContinuousMoveResponse ContinuousMove(ContinuousMoveRequest request)
        {
            var panTiltStatus = request.Velocity.PanTilt != null ? MoveStatus.MOVING : MoveStatus.IDLE;
            var zoomStatus = request.Velocity.Zoom != null ? MoveStatus.MOVING : MoveStatus.IDLE;
            this.PanTiltStatus = panTiltStatus;
            this.ZoomStatus = zoomStatus;
            _logger.LogInformation($"PTZ: ContinuousMove: panTilt: {panTiltStatus}, zoom {zoomStatus}");
            return new ContinuousMoveResponse();
        }

        public override SetPresetResponse SetPreset(SetPresetRequest request)
        {
            string token = Guid.NewGuid().ToString();
            this.Presets.Add(token, new PTZPreset()
            {
                Name = request.PresetName,
                token = token,
                PTZPosition = new PTZVector()
                {
                    PanTilt = new Vector2D() { x = Pan, y = Tilt },
                    Zoom = new Vector1D() { x = Zoom }
                }
            });
            _logger.LogInformation($"PTZ: SetPreset: {request.ProfileToken}/{token}");
            return new SetPresetResponse(token);
        }

        [return: MessageParameter(Name = "Preset")]
        public override GetPresetsResponse GetPresets(GetPresetsRequest request)
        {
            return new GetPresetsResponse()
            {
                Preset = Presets.Values.ToArray()
            };
        }

        public override void GotoPreset(string ProfileToken, string PresetToken, PTZSpeed Speed)
        {
            if(Presets.TryGetValue(PresetToken, out var preset))
            {
                this.Pan = preset.PTZPosition.PanTilt.x;
                this.Tilt = preset.PTZPosition.PanTilt.y;
                this.Zoom = preset.PTZPosition.Zoom.x;

                _logger.LogInformation($"PTZ: GoToPreset: {ProfileToken}/{PresetToken} pan: {preset.PTZPosition.PanTilt.x}, tilt: {preset.PTZPosition.PanTilt.y}, zoom: {preset.PTZPosition.Zoom.x}");
            }
            else
            {
                _logger.LogInformation($"PTZ: GoToPreset: unknown");
            }
        }

        [return: MessageParameter(Name = "PTZNode")]
        public override GetNodesResponse GetNodes(GetNodesRequest request)
        {
            return new GetNodesResponse()
            {
                PTZNode = new PTZNode[]
                {
                    GetMyNode()
                }
            };
        }

        [return: MessageParameter(Name = "PTZNode")]
        public override PTZNode GetNode(string NodeToken)
        {
            return GetMyNode();
        }

        public override void GotoHomePosition(string ProfileToken, PTZSpeed Speed)
        {
            _logger.LogInformation("PTZ: GoToHomePosition");
        }

        private static PTZNode GetMyNode()
        {
            return new PTZNode()
            {
                token = PTZ_NODE_TOKEN,
                Name = PTZ_NODE_TOKEN,
                SupportedPTZSpaces = new PTZSpaces()
                {
                    AbsolutePanTiltPositionSpace = new Space2DDescription[]
                    {
                        new Space2DDescription()
                        {
                            XRange = new FloatRange() { Min = -1, Max = 1 },
                            YRange =  new FloatRange() { Min = -1, Max = 1 },
                            URI = SpacesPanTilt.POSITION_GENERIC_SPACE
                        },
                    },
                    AbsoluteZoomPositionSpace = new Space1DDescription[]
                    {
                        new Space1DDescription()
                        {
                            XRange = new FloatRange() { Min = 0, Max = 1 },
                            URI = SpacesZoom.POSITION_GENERIC_SPACE
                        }
                    },
                    RelativePanTiltTranslationSpace = new Space2DDescription[]
                    {
                        new Space2DDescription()
                        {
                            XRange = new FloatRange() { Min = -1, Max = 1 },
                            YRange =  new FloatRange() { Min = -1, Max = 1 },
                            URI = SpacesPanTilt.TRANSLATION_GENERIC_SPACE
                        },
                    },
                    RelativeZoomTranslationSpace = new Space1DDescription[]
                    {
                        new Space1DDescription()
                        {
                            XRange = new FloatRange() { Min = -1, Max = 1 },
                            URI = SpacesZoom.TRANSLATION_GENERIC_SPACE
                        }
                    },
                    PanTiltSpeedSpace = new Space1DDescription[]
                    {
                        new Space1DDescription()
                        {
                            XRange = new FloatRange { Min = -1, Max = 1 },
                            URI = SpacesPanTilt.SPEED_GENERIC_SPACE
                        }
                    },
                    ZoomSpeedSpace = new Space1DDescription[] {
                        new Space1DDescription()
                        {
                            XRange = new FloatRange { Min = -1, Max = 1 },
                            URI = SpacesZoom.SPEED_GENERIC_SPACE
                        } 
                    },
                    ContinuousPanTiltVelocitySpace = new Space2DDescription[]
                    {
                        new Space2DDescription()
                        {
                            XRange = new FloatRange() { Min = -1, Max = 1 },
                            YRange =  new FloatRange() { Min = -1, Max = 1 },
                            URI = SpacesPanTilt.VELOCITY_GENERIC_SPACE
                        },
                    },
                    ContinuousZoomVelocitySpace = new Space1DDescription[]
                    {
                        new Space1DDescription()
                        {
                            XRange = new FloatRange { Min = -1, Max = 1 },
                            URI = SpacesZoom.VELOCITY_GENERIC_SPACE
                        }
                    },
                },
                HomeSupported = true,
            };
        }

        [return: MessageParameter(Name = "PTZConfiguration")]
        public override GetConfigurationsResponse GetConfigurations(GetConfigurationsRequest request)
        {
            return new GetConfigurationsResponse()
            {
                PTZConfiguration = GetMyPTZConfiguration()
            };
        }

        [return: MessageParameter(Name = "PTZConfiguration")]
        public override GetCompatibleConfigurationsResponse GetCompatibleConfigurations(GetCompatibleConfigurationsRequest request)
        {
            return new GetCompatibleConfigurationsResponse()
            {
                 PTZConfiguration = GetMyPTZConfiguration()
            };
        }

        private PTZConfiguration[] GetMyPTZConfiguration()
        {
            return new PTZConfiguration[]
            {
                new PTZConfiguration()
                {
                    token = PTZ_CONFIGURATION_TOKEN,
                    Name = PTZ_CONFIGURATION_TOKEN,
                    UseCount = 2,
                    NodeToken = PTZ_NODE_TOKEN,
                    DefaultAbsolutePantTiltPositionSpace = SpacesPanTilt.POSITION_GENERIC_SPACE,
                    DefaultAbsoluteZoomPositionSpace = SpacesZoom.POSITION_GENERIC_SPACE,
                    DefaultRelativePanTiltTranslationSpace = SpacesPanTilt.TRANSLATION_GENERIC_SPACE,
                    DefaultRelativeZoomTranslationSpace = SpacesZoom.TRANSLATION_GENERIC_SPACE,
                    DefaultContinuousPanTiltVelocitySpace = SpacesPanTilt.VELOCITY_GENERIC_SPACE,
                    DefaultContinuousZoomVelocitySpace = SpacesZoom.VELOCITY_GENERIC_SPACE,
                    DefaultPTZSpeed = new PTZSpeed()
                    {
                        PanTilt = new Vector2D()
                        {
                            x = 0.5f,
                            y = 0.5f,
                            space = SpacesPanTilt.SPEED_GENERIC_SPACE
                        },
                        Zoom = new Vector1D()
                        {
                            x = 0.5f,
                            space = SpacesZoom.SPEED_GENERIC_SPACE
                        }
                    },
                    DefaultPTZTimeout = OnvifHelpers.GetTimeoutInSeconds(10),
                    PanTiltLimits = new PanTiltLimits()
                    {
                        Range = new Space2DDescription()
                        {
                            XRange = new FloatRange() { Min = -1, Max = 1 },
                            YRange = new FloatRange() { Min = -1, Max = 1 }
                        }
                    },
                    ZoomLimits = new ZoomLimits()
                    {
                        Range = new Space1DDescription()
                        {
                            XRange = new FloatRange() { Min = 0, Max = 1 }
                        }
                    }
                }
            };
        }

    }
}
