using CoreWCF;
using SharpOnvifServer.DeviceIO;

namespace OnvifService.Onvif
{
    public class DeviceIOImpl : DeviceIOPortBase
    {
        [return: MessageParameter(Name = "DigitalInputs")]
        public override GetDigitalInputsResponse GetDigitalInputs(GetDigitalInputsRequest request)
        {
            return new GetDigitalInputsResponse()
            {
                DigitalInputs = new SharpOnvifServer.DeviceMgmt.DigitalInput[]
                {
                    new SharpOnvifServer.DeviceMgmt.DigitalInput()
                    {
                        token = "DigitalInput_1",
                        IdleState = SharpOnvifServer.DeviceMgmt.DigitalIdleState.open,
                        IdleStateSpecified = true
                    }
                }
            };
        }

        [return: MessageParameter(Name = "DigitalInputOptions")]
        public override DigitalInputConfigurationOptions GetDigitalInputConfigurationOptions(string Token)
        {
            if (Token != "DigitalInput_1")
                return new DigitalInputConfigurationOptions();

            return new DigitalInputConfigurationOptions()
            {
                 IdleState = new SharpOnvifServer.DeviceMgmt.DigitalIdleState[]
                 {
                     SharpOnvifServer.DeviceMgmt.DigitalIdleState.open
                 }
            };
        }

        public override SetDigitalInputConfigurationsResponse SetDigitalInputConfigurations(SetDigitalInputConfigurationsRequest request)
        {
            foreach(var input in request.DigitalInputs)
            {
                // TODO
            }

            return new SetDigitalInputConfigurationsResponse();
        }
    }
}
