using CoreWCF;
using Microsoft.AspNetCore.Hosting.Server;
using SharpOnvifServer.PTZ;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OnvifService.Onvif
{
    public class PTZPresetImpl
    {
        public string Name { get; set; }
        public float Pan { get; set; }
        public float Tilt { get; set; }
        public float Zoom { get; set; }
        public PTZPresetImpl(string name, float pan, float tilt, float zoom)
        {
            this.Name = name;
            this.Pan = pan;
            this.Tilt = tilt;
            this.Zoom = zoom;
        }
    }

    public class PTZImpl : PTZBase
    {
        private IServer server;

        public float Pan { get; set; } = 0.5f;
        public float Tilt { get; set; } = 0.5f;
        public float Zoom { get; set; } = 0.5f;

        public MoveStatus PanTiltStatus { get; set; } = MoveStatus.IDLE;
        public MoveStatus ZoomStatus { get; set; } = MoveStatus.IDLE;

        private Dictionary<string, PTZPresetImpl> Presets { get; } = new Dictionary<string, PTZPresetImpl> ();

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
                this.Pan = Position.PanTilt.x;
                this.Tilt = Position.PanTilt.y;
                this.Zoom = Position.Zoom.x;
            }
        }

        public override void RelativeMove(string ProfileToken, PTZVector Translation, PTZSpeed Speed)
        {
            if (Translation != null)
            {
                this.Pan += Translation.PanTilt.x;
                this.Tilt += Translation.PanTilt.y;
                this.Zoom += Translation.Zoom.x;
            }
        }

        public override ContinuousMoveResponse ContinuousMove(ContinuousMoveRequest request)
        {
            this.PanTiltStatus = request.Velocity.PanTilt != null ? MoveStatus.MOVING : MoveStatus.IDLE;
            this.ZoomStatus = request.Velocity.Zoom != null ? MoveStatus.MOVING : MoveStatus.IDLE;
            return new ContinuousMoveResponse();
        }

        public override SetPresetResponse SetPreset(SetPresetRequest request)
        {
            string token = Guid.NewGuid().ToString();
            this.Presets.Add(token, new PTZPresetImpl(request.PresetName, Pan, Tilt, Zoom));
            return new SetPresetResponse(token);
        }

        [return: MessageParameter(Name = "Preset")]
        public override GetPresetsResponse GetPresets(GetPresetsRequest request)
        {
            return new GetPresetsResponse()
            {
                Preset = Presets.Select(x => new PTZPreset()
                {
                    Name = x.Value.Name,
                    token = x.Key,
                    PTZPosition = new PTZVector()
                    {
                        PanTilt = new Vector2D() { x = x.Value.Pan, y = x.Value.Tilt },
                        Zoom = new Vector1D() { x = x.Value.Zoom }
                    }
                }).ToArray()
            };
        }

        public override void GotoPreset(string ProfileToken, string PresetToken, PTZSpeed Speed)
        {
            if(Presets.TryGetValue(PresetToken, out var preset))
            {
                this.Pan = preset.Pan;
                this.Tilt = preset.Tilt;
                this.Zoom = preset.Zoom;
            }
        }
    }
}
