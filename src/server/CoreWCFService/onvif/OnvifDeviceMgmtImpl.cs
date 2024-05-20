


using System;

namespace CoreWCFService.onvif
{
    [DisableMustUnderstandValidationAttribute]
    public class OnvifDeviceMgmtImpl : Device
    {
        public void AddIPAddressFilter(IPAddressFilter IPAddressFilter)
        {
            throw new NotImplementedException();
        }

        public AddScopesResponse AddScopes(AddScopesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "NvtCertificate")]
        public CreateCertificateResponse CreateCertificate(CreateCertificateRequest request)
        {
            throw new NotImplementedException();
        }

        public void CreateDot1XConfiguration(Dot1XConfiguration Dot1XConfiguration)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Token")]
        public string CreateStorageConfiguration(StorageConfigurationData StorageConfiguration)
        {
            throw new NotImplementedException();
        }

        public CreateUsersResponse CreateUsers(CreateUsersRequest request)
        {
            throw new NotImplementedException();
        }

        public DeleteCertificatesResponse DeleteCertificates(DeleteCertificatesRequest request)
        {
            throw new NotImplementedException();
        }

        public DeleteDot1XConfigurationResponse DeleteDot1XConfiguration(DeleteDot1XConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public DeleteGeoLocationResponse DeleteGeoLocation(DeleteGeoLocationRequest request)
        {
            throw new NotImplementedException();
        }

        public void DeleteStorageConfiguration(string Token)
        {
            throw new NotImplementedException();
        }

        public DeleteUsersResponse DeleteUsers(DeleteUsersRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PolicyFile")]
        public BinaryData GetAccessPolicy()
        {
            throw new NotImplementedException();
        }

        public GetAuthFailureWarningConfigurationResponse GetAuthFailureWarningConfiguration(GetAuthFailureWarningConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public GetAuthFailureWarningOptionsResponse GetAuthFailureWarningOptions(GetAuthFailureWarningOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CACertificate")]
        public GetCACertificatesResponse GetCACertificates(GetCACertificatesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public GetCapabilitiesResponse GetCapabilities(GetCapabilitiesRequest request)
        {
            //throw new NotImplementedException();
            return new GetCapabilitiesResponse()
            {
                Capabilities = new Capabilities()
                {
                    Device = new DeviceCapabilities()
                    {
                        XAddr = "http://localhost:5000/device_service",
                        Network = new NetworkCapabilities1()
                        {
                            IPFilter = true,
                            ZeroConfiguration = true,
                            IPVersion6 = true,
                            DynDNS = true,
                        },
                        System = new SystemCapabilities1()
                        {
                            SystemLogging = true,
                            SupportedVersions = new OnvifVersion[]
                            { 
                                new OnvifVersion()
                                {
                                    Major = 16,
                                    Minor = 12
                                }
                            }
                        },
                        IO = new IOCapabilities()
                        {
                            InputConnectors = 0,
                            RelayOutputs = 0
                        },
                        Security = new SecurityCapabilities1()
                        {
                            TLS12 = true,
                        }
                    }
                }
            };
        }

        [return: MessageParameter(Name = "CertificateInformation")]
        public GetCertificateInformationResponse GetCertificateInformation(GetCertificateInformationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "NvtCertificate")]
        public GetCertificatesResponse GetCertificates(GetCertificatesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CertificateStatus")]
        public GetCertificatesStatusResponse GetCertificatesStatus(GetCertificatesStatusRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Enabled")]
        public bool GetClientCertificateMode()
        {
            throw new NotImplementedException();
        }

        public GetDeviceInformationResponse GetDeviceInformation(GetDeviceInformationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DiscoveryMode")]
        public DiscoveryMode GetDiscoveryMode()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DNSInformation")]
        public DNSInformation GetDNS()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public GetDot11CapabilitiesResponse GetDot11Capabilities(GetDot11CapabilitiesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Status")]
        public Dot11Status GetDot11Status(string InterfaceToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Dot1XConfiguration")]
        public Dot1XConfiguration GetDot1XConfiguration(string Dot1XConfigurationToken)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Dot1XConfiguration")]
        public GetDot1XConfigurationsResponse GetDot1XConfigurations(GetDot1XConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DPAddress")]
        public GetDPAddressesResponse GetDPAddresses(GetDPAddressesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DynamicDNSInformation")]
        public DynamicDNSInformation GetDynamicDNS()
        {
            throw new NotImplementedException();
        }

        public GetEndpointReferenceResponse GetEndpointReference(GetEndpointReferenceRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Location")]
        public GetGeoLocationResponse GetGeoLocation(GetGeoLocationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "HostnameInformation")]
        public HostnameInformation GetHostname()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "IPAddressFilter")]
        public IPAddressFilter GetIPAddressFilter()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "NetworkGateway")]
        public NetworkGateway GetNetworkDefaultGateway()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "NetworkInterfaces")]
        public GetNetworkInterfacesResponse GetNetworkInterfaces(GetNetworkInterfacesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "NetworkProtocols")]
        public GetNetworkProtocolsResponse GetNetworkProtocols(GetNetworkProtocolsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "NTPInformation")]
        public NTPInformation GetNTP()
        {
            throw new NotImplementedException();
        }

        public GetPasswordComplexityConfigurationResponse GetPasswordComplexityConfiguration(GetPasswordComplexityConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        public GetPasswordComplexityOptionsResponse GetPasswordComplexityOptions(GetPasswordComplexityOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        public GetPasswordHistoryConfigurationResponse GetPasswordHistoryConfiguration(GetPasswordHistoryConfigurationRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Pkcs10Request")]
        public GetPkcs10RequestResponse GetPkcs10Request(GetPkcs10RequestRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RelayOutputs")]
        public GetRelayOutputsResponse GetRelayOutputs(GetRelayOutputsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RemoteDiscoveryMode")]
        public DiscoveryMode GetRemoteDiscoveryMode()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RemoteUser")]
        public RemoteUser GetRemoteUser()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Scopes")]
        public GetScopesResponse GetScopes(GetScopesRequest request)
        {
            //throw new NotImplementedException();
            return new GetScopesResponse()
            {
                Scopes = new Scope[]
                {
                    new Scope()
                    {
                        ScopeDef = ScopeDefinition.Fixed,
                        ScopeItem = "onvif://www.onvif.org/type/video_encoder"
                    },
                    new Scope()
                    {
                        ScopeDef = ScopeDefinition.Fixed,
                        ScopeItem = "onvif://www.onvif.org/Profile/Streaming"
                    },
                    new Scope()
                    {
                        ScopeDef = ScopeDefinition.Fixed,
                        ScopeItem = "onvif://www.onvif.org/Profile/G"
                    }
                }
            };
        }

        [return: MessageParameter(Name = "Capabilities")]
        public DeviceServiceCapabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Service")]
        public GetServicesResponse GetServices(GetServicesRequest request)
        {
            return new GetServicesResponse()
            {
                Service = new Service[]
                {
                    new Service()
                    {
                        Namespace = "http://www.onvif.org/ver10/device/wsdl",
                        XAddr = "http://192.168.1.15/onvif/device_service",
                        Version = new OnvifVersion()
                        {
                            Major = 17,
                            Minor = 12
                        },
                    }
                }
            };
        }

        [return: MessageParameter(Name = "StorageConfiguration")]
        public StorageConfiguration GetStorageConfiguration(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "StorageConfigurations")]
        public GetStorageConfigurationsResponse GetStorageConfigurations(GetStorageConfigurationsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "BackupFiles")]
        public GetSystemBackupResponse GetSystemBackup(GetSystemBackupRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SystemDateAndTime")]
        public SystemDateTime GetSystemDateAndTime()
        {
            var now = System.DateTime.UtcNow;
            return new SystemDateTime()
            {
                UTCDateTime = new DateTime()
                {
                    Date = new Date()
                    {
                        Day = now.Day,
                        Month = now.Month,
                        Year = now.Year,
                    },
                    Time = new Time()
                    {
                        Hour = now.Hour,
                        Minute = now.Minute,
                        Second = now.Second
                    }
                }
            };
            //throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SystemLog")]
        public SystemLog GetSystemLog(SystemLogType LogType)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SupportInformation")]
        public SupportInformation GetSystemSupportInformation()
        {
            throw new NotImplementedException();
        }

        public GetSystemUrisResponse GetSystemUris(GetSystemUrisRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "User")]
        public GetUsersResponse GetUsers(GetUsersRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "WsdlUrl")]
        public GetWsdlUrlResponse GetWsdlUrl(GetWsdlUrlRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ZeroConfiguration")]
        public NetworkZeroConfiguration GetZeroConfiguration()
        {
            throw new NotImplementedException();
        }

        public LoadCACertificatesResponse LoadCACertificates(LoadCACertificatesRequest request)
        {
            throw new NotImplementedException();
        }

        public LoadCertificatesResponse LoadCertificates(LoadCertificatesRequest request)
        {
            throw new NotImplementedException();
        }

        public LoadCertificateWithPrivateKeyResponse LoadCertificateWithPrivateKey(LoadCertificateWithPrivateKeyRequest request)
        {
            throw new NotImplementedException();
        }

        public void RemoveIPAddressFilter(IPAddressFilter IPAddressFilter)
        {
            throw new NotImplementedException();
        }

        public RemoveScopesResponse RemoveScopes(RemoveScopesRequest request)
        {
            throw new NotImplementedException();
        }

        public RestoreSystemResponse RestoreSystem(RestoreSystemRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Networks")]
        public ScanAvailableDot11NetworksResponse ScanAvailableDot11Networks(ScanAvailableDot11NetworksRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "AuxiliaryCommandResponse")]
        public string SendAuxiliaryCommand(string AuxiliaryCommand)
        {
            throw new NotImplementedException();
        }

        public void SetAccessPolicy(BinaryData PolicyFile)
        {
            throw new NotImplementedException();
        }

        public void SetAuthFailureWarningConfiguration(bool Enabled, int MonitorPeriod, int MaxAuthFailures)
        {
            throw new NotImplementedException();
        }

        public SetCertificatesStatusResponse SetCertificatesStatus(SetCertificatesStatusRequest request)
        {
            throw new NotImplementedException();
        }

        public void SetClientCertificateMode(bool Enabled)
        {
            throw new NotImplementedException();
        }

        public void SetDiscoveryMode(DiscoveryMode DiscoveryMode)
        {
            throw new NotImplementedException();
        }

        public SetDNSResponse SetDNS(SetDNSRequest request)
        {
            throw new NotImplementedException();
        }

        public void SetDot1XConfiguration(Dot1XConfiguration Dot1XConfiguration)
        {
            throw new NotImplementedException();
        }

        public SetDPAddressesResponse SetDPAddresses(SetDPAddressesRequest request)
        {
            throw new NotImplementedException();
        }

        public SetDynamicDNSResponse SetDynamicDNS(SetDynamicDNSRequest request)
        {
            throw new NotImplementedException();
        }

        public SetGeoLocationResponse SetGeoLocation(SetGeoLocationRequest request)
        {
            throw new NotImplementedException();
        }

        public void SetHashingAlgorithm(string Algorithm)
        {
            throw new NotImplementedException();
        }

        public SetHostnameResponse SetHostname(SetHostnameRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RebootNeeded")]
        public bool SetHostnameFromDHCP(bool FromDHCP)
        {
            throw new NotImplementedException();
        }

        public void SetIPAddressFilter(IPAddressFilter IPAddressFilter)
        {
            throw new NotImplementedException();
        }

        public SetNetworkDefaultGatewayResponse SetNetworkDefaultGateway(SetNetworkDefaultGatewayRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RebootNeeded")]
        public bool SetNetworkInterfaces(string InterfaceToken, NetworkInterfaceSetConfiguration NetworkInterface)
        {
            throw new NotImplementedException();
        }

        public SetNetworkProtocolsResponse SetNetworkProtocols(SetNetworkProtocolsRequest request)
        {
            throw new NotImplementedException();
        }

        public SetNTPResponse SetNTP(SetNTPRequest request)
        {
            throw new NotImplementedException();
        }

        public void SetPasswordComplexityConfiguration(int MinLen, int Uppercase, int Number, int SpecialChars, bool BlockUsernameOccurrence, bool PolicyConfigurationLocked)
        {
            throw new NotImplementedException();
        }

        public void SetPasswordHistoryConfiguration(bool Enabled, int Length)
        {
            throw new NotImplementedException();
        }

        public void SetRelayOutputSettings(string RelayOutputToken, RelayOutputSettings Properties)
        {
            throw new NotImplementedException();
        }

        public void SetRelayOutputState(string RelayOutputToken, RelayLogicalState LogicalState)
        {
            throw new NotImplementedException();
        }

        public void SetRemoteDiscoveryMode(DiscoveryMode RemoteDiscoveryMode)
        {
            throw new NotImplementedException();
        }

        public void SetRemoteUser(RemoteUser RemoteUser)
        {
            throw new NotImplementedException();
        }

        public SetScopesResponse SetScopes(SetScopesRequest request)
        {
            throw new NotImplementedException();
        }

        public void SetStorageConfiguration(StorageConfiguration StorageConfiguration)
        {
            throw new NotImplementedException();
        }

        public void SetSystemDateAndTime(SetDateTimeType DateTimeType, bool DaylightSavings, TimeZone TimeZone, DateTime UTCDateTime)
        {
            throw new NotImplementedException();
        }

        public void SetSystemFactoryDefault(FactoryDefaultType FactoryDefault)
        {
            throw new NotImplementedException();
        }

        public SetUserResponse SetUser(SetUserRequest request)
        {
            throw new NotImplementedException();
        }

        public void SetZeroConfiguration(string InterfaceToken, bool Enabled)
        {
            throw new NotImplementedException();
        }

        public StartFirmwareUpgradeResponse StartFirmwareUpgrade(StartFirmwareUpgradeRequest request)
        {
            throw new NotImplementedException();
        }

        public StartSystemRestoreResponse StartSystemRestore(StartSystemRestoreRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Message")]
        public string SystemReboot()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Message")]
        public string UpgradeSystemFirmware(AttachmentData Firmware)
        {
            throw new NotImplementedException();
        }
    }
}
