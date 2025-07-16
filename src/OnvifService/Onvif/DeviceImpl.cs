using CoreWCF;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpOnvifCommon;
using SharpOnvifServer;
using SharpOnvifServer.DeviceMgmt;
using System;
using System.Linq;

namespace OnvifService.Onvif
{
    public class DeviceImpl : DeviceBase
    {
        private const string CERTIFICATE_ID = "Cert_1";

        private readonly IServer _server;
        private readonly ILogger<DeviceImpl> _logger;
        private readonly IConfiguration _configuration;

        public string PrimaryNICName { get; private set; }
        public string PrimaryIPv4Address { get; private set; }
        public string PrimaryIPv4DNS { get; private set; }
        public string PrimaryMACAddress { get; private set; }
        public string PrimaryNTPAddress { get; private set; }
        public string PrimaryIPv4Gateway { get; private set; }

        public DeviceImpl(IServer server, ILogger<DeviceImpl> logger, IConfiguration configuration)
        {
            _server = server;
            _logger = logger;
            _configuration = configuration;

            PrimaryNICName = _configuration.GetValue("DeviceImpl:PrimaryNICName", "");
            if (string.IsNullOrEmpty(PrimaryNICName)) PrimaryNICName = GetPrimaryNICName();
            PrimaryIPv4Address = _configuration.GetValue("DeviceImpl:PrimaryIPv4Address", "");
            if (string.IsNullOrEmpty(PrimaryIPv4Address)) PrimaryIPv4Address = GetPrimaryIPv4Address();
            PrimaryIPv4DNS = _configuration.GetValue("DeviceImpl:PrimaryIPv4DNS", "");
            if (string.IsNullOrEmpty(PrimaryIPv4DNS)) PrimaryIPv4DNS = GetPrimaryIPv4DNS();
            PrimaryMACAddress = _configuration.GetValue("DeviceImpl:PrimaryMACAddress", "");
            if (string.IsNullOrEmpty(PrimaryMACAddress)) PrimaryMACAddress = GetPrimaryMACAddress();
            PrimaryNTPAddress = _configuration.GetValue("DeviceImpl:PrimaryNTPAddress", "");
            if (string.IsNullOrEmpty(PrimaryNTPAddress)) PrimaryNTPAddress = GetPrimaryNTPAddress();
            PrimaryIPv4Gateway = _configuration.GetValue("DeviceImpl:PrimaryIPv4Gateway", "");
            if (string.IsNullOrEmpty(PrimaryIPv4Gateway)) PrimaryIPv4Gateway = GetPrimaryIPv4Gateway();
        }

        public override GetCapabilitiesResponse GetCapabilities(GetCapabilitiesRequest request)
        {
            return new GetCapabilitiesResponse()
            {
                Capabilities = new Capabilities()
                {
                    Device = new DeviceCapabilities()
                    {
                        XAddr = $"{_server.GetHttpEndpoint()}/onvif/device_service",
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
                            },
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
                    },
                    Media = new MediaCapabilities()
                    {
                        XAddr = $"{_server.GetHttpEndpoint()}/onvif/media_service",
                        StreamingCapabilities = new RealTimeStreamingCapabilities()
                        {
                            RTP_RTSP_TCP = true,
                            RTP_RTSP_TCPSpecified = true,
                        }
                    },
                    Events = new EventCapabilities()
                    {
                        WSPullPointSupport = true,
                        XAddr = $"{_server.GetHttpEndpoint()}/onvif/events_service"
                    },
                    PTZ = new PTZCapabilities()
                    {
                        XAddr = $"{_server.GetHttpEndpoint()}/onvif/ptz_service",
                    }
                }
            };
        }

        public override GetDeviceInformationResponse GetDeviceInformation(GetDeviceInformationRequest request)
        {
            return new GetDeviceInformationResponse()
            {
                FirmwareVersion = "1.0",
                HardwareId = "1.0",
                Manufacturer = "Lukas Volf",
                Model = "1",
                SerialNumber = "1",
            };
        }

        public override DNSInformation GetDNS()
        {
            return new DNSInformation()
            {
                DNSManual = new IPAddress[]
                {
                    new IPAddress()
                    {
                        IPv4Address = PrimaryIPv4DNS
                    }
                }
            };
        }

        public override GetNetworkInterfacesResponse GetNetworkInterfaces(GetNetworkInterfacesRequest request)
        {
            return new GetNetworkInterfacesResponse()
            {
                NetworkInterfaces = new NetworkInterface[]
                {
                    new NetworkInterface()
                    {
                        Enabled = true,
                        Info = new NetworkInterfaceInfo()
                        {
                            Name = PrimaryNICName,
                            HwAddress = PrimaryMACAddress,
                        },
                        Link = new NetworkInterfaceLink()
                        {
                             AdminSettings = new NetworkInterfaceConnectionSetting(),
                             OperSettings = new NetworkInterfaceConnectionSetting(), 
                        },
                        IPv4 = new IPv4NetworkInterface()
                        {
                            Config = new IPv4Configuration()
                            {
                                Manual = new PrefixedIPv4Address[] 
                                {
                                    new PrefixedIPv4Address()
                                    {
                                        Address = PrimaryIPv4Address,
                                        PrefixLength = 24
                                    }
                                },
                            },
                            Enabled = true,
                        }
                    },
                }
            };
        }

        [return: MessageParameter(Name = "NTPInformation")]
        public override NTPInformation GetNTP()
        {
            return new NTPInformation()
            {
                NTPManual = new NetworkHost[]
                {
                    new NetworkHost() { IPv4Address = PrimaryNTPAddress }
                }
            };
        }

        [return: MessageParameter(Name = "HostnameInformation")]
        public override HostnameInformation GetHostname()
        {
            return new HostnameInformation()
            {
                Name = new Uri(_server.GetHttpEndpoint()).Host
            };
        }

        [return: MessageParameter(Name = "NetworkProtocols")]
        public override GetNetworkProtocolsResponse GetNetworkProtocols(GetNetworkProtocolsRequest request)
        {
            return new GetNetworkProtocolsResponse()
            {
                NetworkProtocols = new NetworkProtocol[]
                {
                    new NetworkProtocol()
                    {
                         Enabled = true,
                         Name = NetworkProtocolType.RTSP,
                         Port = new int[] { 8554 }, // TODO: change this when modifying the RTSP URI
                    }
                }
            };
        }

        [return: MessageParameter(Name = "NetworkGateway")]
        public override NetworkGateway GetNetworkDefaultGateway()
        {
            return new NetworkGateway()
            {
                IPv4Address = new string[] { PrimaryIPv4Gateway }
            };
        }

        [return: MessageParameter(Name = "DiscoveryMode")]
        public override DiscoveryMode GetDiscoveryMode()
        {
            return DiscoveryMode.Discoverable;
        }

        public override GetScopesResponse GetScopes(GetScopesRequest request)
        {
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

        public override GetServicesResponse GetServices(GetServicesRequest request)
        {
            return new GetServicesResponse()
            {
                Service = new Service[]
                {
                    new Service()
                    {
                        Namespace = OnvifServices.DEVICE_MGMT, 
                        XAddr = $"{_server.GetHttpEndpoint()}/onvif/device_service",
                        Version = new OnvifVersion()
                        {
                            Major = 17,
                            Minor = 12
                        },
                    },
                    new Service()
                    {
                        Namespace = OnvifServices.MEDIA,
                        XAddr = $"{_server.GetHttpEndpoint()}/onvif/media_service",
                        Version = new OnvifVersion()
                        {
                            Major = 17,
                            Minor = 12
                        },
                    },
                    new Service()
                    {
                        Namespace = OnvifServices.EVENTS,
                        XAddr = $"{_server.GetHttpEndpoint()}/onvif/events_service",
                        Version = new OnvifVersion()
                        {
                            Major = 17,
                            Minor = 12
                        },
                    },
                    new Service()
                    {
                        Namespace = OnvifServices.PTZ,
                        XAddr = $"{_server.GetHttpEndpoint()}/onvif/ptz_service",
                        Version = new OnvifVersion()
                        {
                            Major = 17,
                            Minor = 12
                        },
                    },
                }
            };
        }

        public override SystemDateTime GetSystemDateAndTime()
        {
            var now = System.DateTime.UtcNow;
            return new SystemDateTime()
            {
                UTCDateTime = new SharpOnvifServer.DeviceMgmt.DateTime()
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
        }

        [return: MessageParameter(Name = "Capabilities")]
        public override DeviceServiceCapabilities GetServiceCapabilities()
        {
            return new DeviceServiceCapabilities();
        }

        public override void SetSystemFactoryDefault(FactoryDefaultType FactoryDefault)
        {
            _logger.LogInformation("Device: SetSystemFactoryDefault");
        }

        [return: MessageParameter(Name = "Message")]
        public override string SystemReboot()
        {
            _logger.LogInformation("Device: SystemReboot");
            return "";
        }

        [return: MessageParameter(Name = "User")]
        public override GetUsersResponse GetUsers(GetUsersRequest request)
        {
            return new GetUsersResponse()
            {
                User = new User[]
                {
                    new User()
                    {
                        Username = "admin",
                        UserLevel = UserLevel.Administrator
                    }
                }
            };
        }

        [return: MessageParameter(Name = "SystemLog")]
        public override SystemLog GetSystemLog(SystemLogType LogType)
        {
            return new SystemLog()
            {
                String = "log"
            };
        }

        [return: MessageParameter(Name = "NvtCertificate")]
        public override GetCertificatesResponse GetCertificates(GetCertificatesRequest request)
        {
            return new GetCertificatesResponse()
            {
                NvtCertificate = new Certificate[] 
                {
                    new Certificate()
                    {
                        Certificate1 = new BinaryData()
                        {
                            contentType = "",
                            Data = new byte[] { } // TODO
                        },
                        CertificateID = CERTIFICATE_ID
                    }
                }
            };
        }

        [return: MessageParameter(Name = "CertificateStatus")]
        public override GetCertificatesStatusResponse GetCertificatesStatus(GetCertificatesStatusRequest request)
        {
            return new GetCertificatesStatusResponse()
            {
                CertificateStatus = new CertificateStatus[]
                {
                    new CertificateStatus()
                    {
                        CertificateID = CERTIFICATE_ID,
                        Status = true
                    }
                }
            };
        }

        public override void SetSystemDateAndTime(SetDateTimeType DateTimeType, bool DaylightSavings, SharpOnvifServer.DeviceMgmt.TimeZone TimeZone, SharpOnvifServer.DeviceMgmt.DateTime UTCDateTime)
        {
            _logger.LogInformation("Device: SetSystemDateAndTime");
        }

        public override void SetRelayOutputState(string RelayOutputToken, RelayLogicalState LogicalState)
        {
            _logger.LogInformation("Device: SetRelayOutputState");
        }

        public override void SetRelayOutputSettings(string RelayOutputToken, RelayOutputSettings Properties)
        {
            _logger.LogInformation("Device: SetRelayOutputSettings");
        }

        [return: MessageParameter(Name = "RelayOutputs")]
        public override GetRelayOutputsResponse GetRelayOutputs(GetRelayOutputsRequest request)
        {
            return new GetRelayOutputsResponse()
            {
                RelayOutputs = new RelayOutput[]
                {
                     new RelayOutput()
                     {
                          Properties = new RelayOutputSettings()
                          {
                               DelayTime = OnvifHelpers.GetTimeoutInSeconds(5),
                               IdleState = RelayIdleState.closed,
                               Mode = RelayMode.Monostable
                          },
                          token = "RelayOutput_1"
                     }
                 }
            };
        }

        #region Network Helpers

        private static System.Net.NetworkInformation.NetworkInterface GetPrimaryNetworkInterface()
        {
            return System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(
                i => i.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback &&
                i.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Tunnel &&
                i.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
            );
        }

        public static string GetPrimaryIPv4Address()
        {
            string ret = "0.0.0.0";
            var nic = GetPrimaryNetworkInterface();
            if (nic != null)
            {
                var nicProperties = nic.GetIPProperties();
                if (nicProperties != null)
                {
                    var unicastAddresses = nicProperties.UnicastAddresses;
                    if (unicastAddresses != null)
                    {
                        var unicastAddress = unicastAddresses.FirstOrDefault(x => x.Address != null && x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                        if (unicastAddress != null)
                        {
                            if (unicastAddress.Address != null)
                            {
                                ret = unicastAddress.Address.ToString();
                            }
                        }
                    }
                }
            }
            return ret;
        }

        public static string GetPrimaryIPv4DNS()
        {
            string ret = "0.0.0.0";
            var nic = GetPrimaryNetworkInterface();
            if (nic != null)
            {
                var nicProperties = nic.GetIPProperties();
                if (nicProperties != null)
                {
                    var dnsAddresses = nicProperties.DnsAddresses;
                    if (dnsAddresses != null)
                    {
                        var dnsAddress = dnsAddresses.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                        if (dnsAddress != null)
                        {
                            ret = dnsAddress.ToString();
                        }
                    }
                }
            }
            return ret;
        }

        public static string GetPrimaryNTPAddress(string ntp = "time.windows.com")
        {
            string ret = "0.0.0.0";
            var dnsHostEntry = System.Net.Dns.GetHostEntry(ntp);
            if (dnsHostEntry != null)
            {
                var addressList = dnsHostEntry.AddressList;
                if(addressList != null)
                {
                    var ntpAddress = addressList.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                    if(ntpAddress != null)
                    {
                        ret = ntpAddress.ToString();
                    }
                }
            }
            return ret;
        }

        public static string GetPrimaryIPv4Gateway()
        {
            string ret = "0.0.0.0";
            var nic = GetPrimaryNetworkInterface();
            if (nic != null)
            {
                var nicProperties = nic.GetIPProperties();
                if (nicProperties != null)
                {
                    var gatewayAddresses = nicProperties.GatewayAddresses;
                    if (gatewayAddresses != null)
                    {
                        var gatewayAddress = gatewayAddresses.FirstOrDefault(x => x.Address != null && x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                        if (gatewayAddress != null)
                        {
                            if (gatewayAddress.Address != null)
                            {
                                ret = gatewayAddress.Address.ToString();
                            }
                        }
                    }
                }
            }
            return ret;
        }

        public static string GetPrimaryMACAddress()
        {
            string ret = "00:00:00:00:00:00";
            var nic = GetPrimaryNetworkInterface();
            if (nic != null)
            {
                var nicPhysicalAddress = nic.GetPhysicalAddress();
                if (nicPhysicalAddress != null)
                {
                    ret = BitConverter.ToString(nicPhysicalAddress.GetAddressBytes()).Replace('-', ':');
                }
            }
            return ret;
        }
        private string GetPrimaryNICName()
        {
            string ret = "eth0";
            var nic = GetPrimaryNetworkInterface();
            if (nic != null)
            {
                ret = nic.Name;
            }
            return ret;
        }

        #endregion // Network Helpers
    }
}
