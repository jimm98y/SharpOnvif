using SharpOnvifCommon;
using SharpOnvifServer.DeviceMgmt;

namespace OnvifService.Onvif
{
    public class DeviceImpl : DeviceBase
    {
        public override GetCapabilitiesResponse GetCapabilities(GetCapabilitiesRequest request)
        {
            return new GetCapabilitiesResponse()
            {
                Capabilities = new Capabilities()
                {
                    Device = new DeviceCapabilities()
                    {
                        XAddr = $"http://{NetworkHelpers.GetIPv4NetworkInterface()}:5000/onvif/device_service",
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
                    },
                    Media = new MediaCapabilities()
                    {
                        XAddr = $"http://{NetworkHelpers.GetIPv4NetworkInterface()}:5000/onvif/media_service"
                    },
                    Events = new EventCapabilities()
                    {
                        WSPullPointSupport = true,
                        XAddr = $"http://{NetworkHelpers.GetIPv4NetworkInterface()}:5000/onvif/events_service"
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
                SerialNumber = "1"
            };
        }

        public override DNSInformation GetDNS()
        {
            return new DNSInformation()
            {
                FromDHCP = false,
                DNSManual = new IPAddress[]
                {
                    new IPAddress()
                    {
                        IPv4Address = "8.8.8.8"
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
                            Name = "eth0"
                        }
                    },
                }
            };
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
                        XAddr = $"http://{NetworkHelpers.GetIPv4NetworkInterface()}:5000/onvif/device_service",
                        Version = new OnvifVersion()
                        {
                            Major = 17,
                            Minor = 12
                        },
                    },
                    new Service()
                    {
                        Namespace = OnvifServices.MEDIA,
                        XAddr = $"http://{NetworkHelpers.GetIPv4NetworkInterface()}:5000/onvif/media_service",
                        Version = new OnvifVersion()
                        {
                            Major = 17,
                            Minor = 12
                        },
                    },
                    new Service()
                    {
                        Namespace = OnvifServices.EVENTS,
                        XAddr = $"http://{NetworkHelpers.GetIPv4NetworkInterface()}:5000/onvif/events_service",
                        Version = new OnvifVersion()
                        {
                            Major = 17,
                            Minor = 12
                        },
                    }
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
    }
}
