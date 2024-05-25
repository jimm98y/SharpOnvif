using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.DeviceIO
{
    [DisableMustUnderstandValidation]
    public class DeviceBase : Device
    {
        public virtual void AddIPAddressFilter(IPAddressFilter IPAddressFilter)
        {
            throw new NotImplementedException();
        }

        public virtual AddScopesResponse AddScopes(AddScopesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "NvtCertificate")]
        public virtual CreateCertificateResponse CreateCertificate(CreateCertificateRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void CreateDot1XConfiguration(Dot1XConfiguration Dot1XConfiguration)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Token")]
        public virtual string CreateStorageConfiguration(StorageConfigurationData StorageConfiguration)
        {
            throw new NotImplementedException();
        }

        public virtual CreateUsersResponse CreateUsers(CreateUsersRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteCertificatesResponse DeleteCertificates(DeleteCertificatesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteDot1XConfigurationResponse DeleteDot1XConfiguration(DeleteDot1XConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteGeoLocationResponse DeleteGeoLocation(DeleteGeoLocationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteStorageConfiguration(string Token)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteUsersResponse DeleteUsers(DeleteUsersRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PolicyFile")]
        public virtual BinaryData GetAccessPolicy()
        {
            throw new NotImplementedException();
        }

        public virtual GetAuthFailureWarningConfigurationResponse GetAuthFailureWarningConfiguration(GetAuthFailureWarningConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetAuthFailureWarningOptionsResponse GetAuthFailureWarningOptions(GetAuthFailureWarningOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CACertificate")]
        public virtual GetCACertificatesResponse GetCACertificates(GetCACertificatesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual GetCapabilitiesResponse GetCapabilities(GetCapabilitiesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CertificateInformation")]
        public virtual GetCertificateInformationResponse GetCertificateInformation(GetCertificateInformationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "NvtCertificate")]
        public virtual GetCertificatesResponse GetCertificates(GetCertificatesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CertificateStatus")]
        public virtual GetCertificatesStatusResponse GetCertificatesStatus(GetCertificatesStatusRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Enabled")]
        public virtual bool GetClientCertificateMode()
        {
            throw new NotImplementedException();
        }

        public virtual GetDeviceInformationResponse GetDeviceInformation(GetDeviceInformationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DiscoveryMode")]
        public virtual DiscoveryMode GetDiscoveryMode()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DNSInformation")]
        public virtual DNSInformation GetDNS()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual GetDot11CapabilitiesResponse GetDot11Capabilities(GetDot11CapabilitiesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Status")]
        public virtual Dot11Status GetDot11Status(string InterfaceToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Dot1XConfiguration")]
        public virtual Dot1XConfiguration GetDot1XConfiguration(string Dot1XConfigurationToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Dot1XConfiguration")]
        public virtual GetDot1XConfigurationsResponse GetDot1XConfigurations(GetDot1XConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DPAddress")]
        public virtual GetDPAddressesResponse GetDPAddresses(GetDPAddressesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DynamicDNSInformation")]
        public virtual DynamicDNSInformation GetDynamicDNS()
        {
            throw new NotImplementedException();
        }

        public virtual GetEndpointReferenceResponse GetEndpointReference(GetEndpointReferenceRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Location")]
        public virtual GetGeoLocationResponse GetGeoLocation(GetGeoLocationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "HostnameInformation")]
        public virtual HostnameInformation GetHostname()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "IPAddressFilter")]
        public virtual IPAddressFilter GetIPAddressFilter()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "NetworkGateway")]
        public virtual NetworkGateway GetNetworkDefaultGateway()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "NetworkInterfaces")]
        public virtual GetNetworkInterfacesResponse GetNetworkInterfaces(GetNetworkInterfacesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "NetworkProtocols")]
        public virtual GetNetworkProtocolsResponse GetNetworkProtocols(GetNetworkProtocolsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "NTPInformation")]
        public virtual NTPInformation GetNTP()
        {
            throw new NotImplementedException();
        }

        public virtual GetPasswordComplexityConfigurationResponse GetPasswordComplexityConfiguration(GetPasswordComplexityConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetPasswordComplexityOptionsResponse GetPasswordComplexityOptions(GetPasswordComplexityOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetPasswordHistoryConfigurationResponse GetPasswordHistoryConfiguration(GetPasswordHistoryConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Pkcs10Request")]
        public virtual GetPkcs10RequestResponse GetPkcs10Request(GetPkcs10RequestRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RelayOutputs")]
        public virtual GetRelayOutputsResponse1 GetRelayOutputs(GetRelayOutputsRequest1 request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RemoteDiscoveryMode")]
        public virtual DiscoveryMode GetRemoteDiscoveryMode()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RemoteUser")]
        public virtual RemoteUser GetRemoteUser()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Scopes")]
        public virtual GetScopesResponse GetScopes(GetScopesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual DeviceServiceCapabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Service")]
        public virtual GetServicesResponse GetServices(GetServicesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "StorageConfiguration")]
        public virtual StorageConfiguration GetStorageConfiguration(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "StorageConfigurations")]
        public virtual GetStorageConfigurationsResponse GetStorageConfigurations(GetStorageConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "BackupFiles")]
        public virtual GetSystemBackupResponse GetSystemBackup(GetSystemBackupRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SystemDateAndTime")]
        public virtual SystemDateTime GetSystemDateAndTime()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SystemLog")]
        public virtual SystemLog GetSystemLog(SystemLogType LogType)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SupportInformation")]
        public virtual SupportInformation GetSystemSupportInformation()
        {
            throw new NotImplementedException();
        }

        public virtual GetSystemUrisResponse GetSystemUris(GetSystemUrisRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "User")]
        public virtual GetUsersResponse GetUsers(GetUsersRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "WsdlUrl")]
        public virtual GetWsdlUrlResponse GetWsdlUrl(GetWsdlUrlRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ZeroConfiguration")]
        public virtual NetworkZeroConfiguration GetZeroConfiguration()
        {
            throw new NotImplementedException();
        }

        public virtual LoadCACertificatesResponse LoadCACertificates(LoadCACertificatesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual LoadCertificatesResponse LoadCertificates(LoadCertificatesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual LoadCertificateWithPrivateKeyResponse LoadCertificateWithPrivateKey(LoadCertificateWithPrivateKeyRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void RemoveIPAddressFilter(IPAddressFilter IPAddressFilter)
        {
            throw new NotImplementedException();
        }

        public virtual RemoveScopesResponse RemoveScopes(RemoveScopesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual RestoreSystemResponse RestoreSystem(RestoreSystemRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Networks")]
        public virtual ScanAvailableDot11NetworksResponse ScanAvailableDot11Networks(ScanAvailableDot11NetworksRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "AuxiliaryCommandResponse")]
        public virtual string SendAuxiliaryCommand(string AuxiliaryCommand)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAccessPolicy(BinaryData PolicyFile)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAuthFailureWarningConfiguration(bool Enabled, int MonitorPeriod, int MaxAuthFailures)
        {
            throw new NotImplementedException();
        }

        public virtual SetCertificatesStatusResponse SetCertificatesStatus(SetCertificatesStatusRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetClientCertificateMode(bool Enabled)
        {
            throw new NotImplementedException();
        }

        public virtual void SetDiscoveryMode(DiscoveryMode DiscoveryMode)
        {
            throw new NotImplementedException();
        }

        public virtual SetDNSResponse SetDNS(SetDNSRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetDot1XConfiguration(Dot1XConfiguration Dot1XConfiguration)
        {
            throw new NotImplementedException();
        }

        public virtual SetDPAddressesResponse SetDPAddresses(SetDPAddressesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual SetDynamicDNSResponse SetDynamicDNS(SetDynamicDNSRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual SetGeoLocationResponse SetGeoLocation(SetGeoLocationRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetHashingAlgorithm(string Algorithm)
        {
            throw new NotImplementedException();
        }

        public virtual SetHostnameResponse SetHostname(SetHostnameRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RebootNeeded")]
        public virtual bool SetHostnameFromDHCP(bool FromDHCP)
        {
            throw new NotImplementedException();
        }

        public virtual void SetIPAddressFilter(IPAddressFilter IPAddressFilter)
        {
            throw new NotImplementedException();
        }

        public virtual SetNetworkDefaultGatewayResponse SetNetworkDefaultGateway(SetNetworkDefaultGatewayRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RebootNeeded")]
        public virtual bool SetNetworkInterfaces(string InterfaceToken, NetworkInterfaceSetConfiguration NetworkInterface)
        {
            throw new NotImplementedException();
        }

        public virtual SetNetworkProtocolsResponse SetNetworkProtocols(SetNetworkProtocolsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual SetNTPResponse SetNTP(SetNTPRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetPasswordComplexityConfiguration(int MinLen, int Uppercase, int Number, int SpecialChars, bool BlockUsernameOccurrence, bool PolicyConfigurationLocked)
        {
            throw new NotImplementedException();
        }

        public virtual void SetPasswordHistoryConfiguration(bool Enabled, int Length)
        {
            throw new NotImplementedException();
        }

        public virtual void SetRelayOutputSettings(string RelayOutputToken, RelayOutputSettings Properties)
        {
            throw new NotImplementedException();
        }

        public virtual void SetRelayOutputState(string RelayOutputToken, RelayLogicalState LogicalState)
        {
            throw new NotImplementedException();
        }

        public virtual void SetRemoteDiscoveryMode(DiscoveryMode RemoteDiscoveryMode)
        {
            throw new NotImplementedException();
        }

        public virtual void SetRemoteUser(RemoteUser RemoteUser)
        {
            throw new NotImplementedException();
        }

        public virtual SetScopesResponse SetScopes(SetScopesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetStorageConfiguration(StorageConfiguration StorageConfiguration)
        {
            throw new NotImplementedException();
        }

        public virtual void SetSystemDateAndTime(SetDateTimeType DateTimeType, bool DaylightSavings, TimeZone TimeZone, DateTime UTCDateTime)
        {
            throw new NotImplementedException();
        }

        public virtual void SetSystemFactoryDefault(FactoryDefaultType FactoryDefault)
        {
            throw new NotImplementedException();
        }

        public virtual SetUserResponse SetUser(SetUserRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetZeroConfiguration(string InterfaceToken, bool Enabled)
        {
            throw new NotImplementedException();
        }

        public virtual StartFirmwareUpgradeResponse StartFirmwareUpgrade(StartFirmwareUpgradeRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual StartSystemRestoreResponse StartSystemRestore(StartSystemRestoreRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Message")]
        public virtual string SystemReboot()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Message")]
        public virtual string UpgradeSystemFirmware(AttachmentData Firmware)
        {
            throw new NotImplementedException();
        }
    }
}
