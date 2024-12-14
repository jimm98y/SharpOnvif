using CoreWCF;
using Microsoft.AspNetCore.Hosting.Server;
using SharpOnvifCommon.PTZ;
using SharpOnvifServer.PTZ;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OnvifService.Onvif
{
    public class PTZImpl : PTZBase
    {
        public const string PTZ_NODE_TOKEN = "PTZ_Node_1";

        private IServer server;

        public float Pan { get; set; } = 0.5f;
        public float Tilt { get; set; } = 0.5f;
        public float Zoom { get; set; } = 0.5f;

        public MoveStatus PanTiltStatus { get; set; } = MoveStatus.IDLE;
        public MoveStatus ZoomStatus { get; set; } = MoveStatus.IDLE;

        private Dictionary<string, PTZPreset> Presets { get; } = new Dictionary<string, PTZPreset>();

        public PTZImpl(IServer server)
        {
            this.server = server;
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
                    LogAction($"PTZ: AbsoluteMove: pan: {Position.PanTilt.x}, tilt: {Position.PanTilt.y}");
                }

                if (Position.Zoom != null)
                {
                    this.Zoom = Position.Zoom.x;
                    LogAction($"PTZ: AbsoluteMove: zoom: {Position.Zoom.x}");
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
                    LogAction($"PTZ: RelativeMove: panDelta: {Translation.PanTilt.x}, tiltDelta: {Translation.PanTilt.y}");
                }

                if (Translation.Zoom != null)
                {
                    this.Zoom += Translation.Zoom.x;
                    LogAction($"PTZ: RelativeMove: zoomDelta: {Translation.Zoom.x}");
                }
            }
        }

        public override void Stop(string ProfileToken, bool PanTilt, bool Zoom)
        {
            LogAction($"PTZ: Stop");
        }

        public override ContinuousMoveResponse ContinuousMove(ContinuousMoveRequest request)
        {
            var panTiltStatus = request.Velocity.PanTilt != null ? MoveStatus.MOVING : MoveStatus.IDLE;
            var zoomStatus = request.Velocity.Zoom != null ? MoveStatus.MOVING : MoveStatus.IDLE;
            this.PanTiltStatus = panTiltStatus;
            this.ZoomStatus = zoomStatus;
            LogAction($"PTZ: ContinuousMove: panTilt: {panTiltStatus}, zoom {zoomStatus}");
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
            LogAction($"PTZ: SetPreset: {request.ProfileToken}/{token}");
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

                LogAction($"PTZ: GoToPreset: {ProfileToken}/{PresetToken} pan: {preset.PTZPosition.PanTilt.x}, tilt: {preset.PTZPosition.PanTilt.y}, zoom: {preset.PTZPosition.Zoom.x}");
            }
            else
            {
                LogAction($"PTZ: GoToPreset: unknown");
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
            LogAction("PTZ: GoToHomePosition");
        }

        private static PTZNode GetMyNode()
        {
            return new PTZNode()
            {
                token = PTZ_NODE_TOKEN,
                Name = "Default",
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

        private static void LogAction(string log)
        {
            Console.WriteLine(log);
        }
    }
}
