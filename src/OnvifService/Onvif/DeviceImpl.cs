using CoreWCF;
using Microsoft.AspNetCore.Hosting.Server;
using SharpOnvifCommon;
using SharpOnvifServer;
using SharpOnvifServer.DeviceMgmt;
using System;

namespace OnvifService.Onvif
{
    public class DeviceImpl : DeviceBase
    {
        private const string CERTIFICATE_ID = "Cert_1";

        private readonly IServer _server;

        public DeviceImpl(IServer server)
        {
            _server = server;
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
                DNSManual = new SharpOnvifServer.DeviceMgmt.IPAddress[]
                {
                    new SharpOnvifServer.DeviceMgmt.IPAddress()
                    {
                        IPv4Address = NetworkHelpers.GetIPv4NetworkInterfaceDns()
                    }
                }
            };
        }

        public override GetNetworkInterfacesResponse GetNetworkInterfaces(GetNetworkInterfacesRequest request)
        {
            return new GetNetworkInterfacesResponse()
            {
                NetworkInterfaces = new SharpOnvifServer.DeviceMgmt.NetworkInterface[]
                {
                    new SharpOnvifServer.DeviceMgmt.NetworkInterface()
                    {
                        Enabled = true,
                        Info = new NetworkInterfaceInfo()
                        {
                            Name = "eth0",
                            HwAddress = NetworkHelpers.GetPrimaryNetworkInterfaceMAC(),
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
                                        Address = NetworkHelpers.GetIPv4NetworkInterface(),
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
                    new NetworkHost() { IPv4Address = NetworkHelpers.GetIPv4NTPAddress() }
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
                IPv4Address = new string[] { NetworkHelpers.GetIPv4NetworkInterfaceGateway() }
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
            LogAction("Device: SetSystemFactoryDefault");
        }

        [return: MessageParameter(Name = "Message")]
        public override string SystemReboot()
        {
            LogAction("Device: SystemReboot");
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
            LogAction("Device: SetSystemDateAndTime");
        }

        private static void LogAction(string log)
        {
            Console.WriteLine(log);
        }
    }
}
