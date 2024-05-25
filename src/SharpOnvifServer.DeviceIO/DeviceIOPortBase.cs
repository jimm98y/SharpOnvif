using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.DeviceIO
{
    [DisableMustUnderstandValidation]
    public class DeviceIOPortBase : DeviceIOPort
    {
        public virtual GetAudioOutputConfigurationResponse GetAudioOutputConfiguration(GetAudioOutputConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetAudioOutputConfigurationOptionsResponse GetAudioOutputConfigurationOptions(GetAudioOutputConfigurationOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Token")]
        public virtual GetAudioOutputsResponse GetAudioOutputs(GetAudioOutputsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetAudioSourceConfigurationResponse GetAudioSourceConfiguration(GetAudioSourceConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetAudioSourceConfigurationOptionsResponse GetAudioSourceConfigurationOptions(GetAudioSourceConfigurationOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Token")]
        public virtual GetAudioSourcesResponse GetAudioSources(GetAudioSourcesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DigitalInputOptions")]
        public virtual DigitalInputConfigurationOptions GetDigitalInputConfigurationOptions(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DigitalInputs")]
        public virtual GetDigitalInputsResponse GetDigitalInputs(GetDigitalInputsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RelayOutputOptions")]
        public virtual GetRelayOutputOptionsResponse GetRelayOutputOptions(GetRelayOutputOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetRelayOutputsResponse GetRelayOutputs(GetRelayOutputsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SerialPortConfiguration")]
        public virtual SerialPortConfiguration GetSerialPortConfiguration(string SerialPortToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SerialPortOptions")]
        public virtual SerialPortConfigurationOptions GetSerialPortConfigurationOptions(string SerialPortToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SerialPort")]
        public virtual GetSerialPortsResponse GetSerialPorts(GetSerialPortsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        public virtual GetVideoOutputConfigurationResponse GetVideoOutputConfiguration(GetVideoOutputConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetVideoOutputConfigurationOptionsResponse GetVideoOutputConfigurationOptions(GetVideoOutputConfigurationOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "VideoOutputs")]
        public virtual GetVideoOutputsResponse GetVideoOutputs(GetVideoOutputsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetVideoSourceConfigurationResponse GetVideoSourceConfiguration(GetVideoSourceConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetVideoSourceConfigurationOptionsResponse GetVideoSourceConfigurationOptions(GetVideoSourceConfigurationOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Token")]
        public virtual GetVideoSourcesResponse GetVideoSources(GetVideoSourcesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual SendReceiveSerialCommandResponse SendReceiveSerialCommand(SendReceiveSerialCommandRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual SetAudioOutputConfigurationResponse SetAudioOutputConfiguration(SetAudioOutputConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual SetAudioSourceConfigurationResponse SetAudioSourceConfiguration(SetAudioSourceConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual SetDigitalInputConfigurationsResponse SetDigitalInputConfigurations(SetDigitalInputConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetRelayOutputSettings(RelayOutput RelayOutput)
        {
            throw new NotImplementedException();
        }

        public virtual SetRelayOutputStateResponse SetRelayOutputState(SetRelayOutputStateRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetSerialPortConfiguration(SerialPortConfiguration SerialPortConfiguration, bool ForcePersistance)
        {
            throw new NotImplementedException();
        }

        public virtual SetVideoOutputConfigurationResponse SetVideoOutputConfiguration(SetVideoOutputConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual SetVideoSourceConfigurationResponse SetVideoSourceConfiguration(SetVideoSourceConfigurationRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
