// SharpOnvif
// Copyright (C) 2026 Lukas Volf
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.

using SharpOnvifServer.Dispatch;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpOnvifCommon;
using SharpOnvifServer.DeviceMgmt;
using System;
using System.Linq;
using SharpOnvifCommon.Onvif;

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
        public string PrimaryIPv4Gateway { get; private set; }

        public string PrimaryIPv6Address { get; private set; }
        public string PrimaryIPv6DNS { get; private set; }
        public string PrimaryIPv6Gateway { get; private set; }

        public string PrimaryMACAddress { get; private set; }
        public string PrimaryNTPAddress { get; private set; }

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
            PrimaryIPv4Gateway = _configuration.GetValue("DeviceImpl:PrimaryIPv4Gateway", "");
            if (string.IsNullOrEmpty(PrimaryIPv4Gateway)) PrimaryIPv4Gateway = GetPrimaryIPv4Gateway();

            PrimaryIPv6Address = _configuration.GetValue("DeviceImpl:PrimaryIPv6Address", "");
            if (string.IsNullOrEmpty(PrimaryIPv6Address)) PrimaryIPv6Address = GetPrimaryIPv6Address();
            PrimaryIPv6DNS = _configuration.GetValue("DeviceImpl:PrimaryIPv6DNS", "");
            if (string.IsNullOrEmpty(PrimaryIPv6DNS)) PrimaryIPv6DNS = GetPrimaryIPv6DNS();
            PrimaryIPv6Gateway = _configuration.GetValue("DeviceImpl:PrimaryIPv6Gateway", "");
            if (string.IsNullOrEmpty(PrimaryIPv6Gateway)) PrimaryIPv6Gateway = GetPrimaryIPv6Gateway();

            PrimaryMACAddress = _configuration.GetValue("DeviceImpl:PrimaryMACAddress", "");
            if (string.IsNullOrEmpty(PrimaryMACAddress)) PrimaryMACAddress = GetPrimaryMACAddress();
            PrimaryNTPAddress = _configuration.GetValue("DeviceImpl:PrimaryNTPAddress", "");
            if (string.IsNullOrEmpty(PrimaryNTPAddress)) PrimaryNTPAddress = GetPrimaryNTPAddress();
        }

        public override GetCapabilitiesResponse GetCapabilities(GetCapabilitiesRequest request)
        {
            Uri endpointUri = OnvifOperationContext.RequestUri;

            return new GetCapabilitiesResponse()
            {
                Capabilities = new Capabilities()
                {
                    Device = new DeviceCapabilities()
                    {
                        XAddr = OnvifHelpers.ChangeUriPath(endpointUri, "/onvif/device_service").ToString(),
                        Network = new SharpOnvifCommon.Onvif.NetworkCapabilities()
                        {
                            IPFilter = true,
                            ZeroConfiguration = true,
                            IPVersion6 = true,
                            DynDNS = true,
                        },
                        System = new SharpOnvifCommon.Onvif.SystemCapabilities()
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
                            InputConnectors = 1,
                            RelayOutputs = 1,
                            RelayOutputsSpecified = true,
                            InputConnectorsSpecified = true
                        },
                        Security = new SharpOnvifCommon.Onvif.SecurityCapabilities()
                        {
                            TLS12 = true,
                        }
                    },
                    Media = new MediaCapabilities()
                    {
                        XAddr = OnvifHelpers.ChangeUriPath(endpointUri, "/onvif/device_service").ToString(),
                        StreamingCapabilities = new RealTimeStreamingCapabilities()
                        {
                            RTP_RTSP_TCP = true,
                            RTP_RTSP_TCPSpecified = true,
                        }
                    },
                    Events = new EventCapabilities()
                    {
                        WSPullPointSupport = true,
                        XAddr = OnvifHelpers.ChangeUriPath(endpointUri, "/onvif/events_service").ToString()
                    },
                    PTZ = new PTZCapabilities()
                    {
                        XAddr = OnvifHelpers.ChangeUriPath(endpointUri, "/onvif/device_service").ToString(),
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

        public override GetDNSResponse GetDNS()
        {
            return new GetDNSResponse(new DNSInformation()
            {
                DNSManual = new OnvifIPAddress[]
                {
                    new OnvifIPAddress()
                    {
                        IPv4Address = PrimaryIPv4DNS
                    }
                }
            });
        }

        public override GetNetworkInterfacesResponse GetNetworkInterfaces(GetNetworkInterfacesRequest request)
        {
            return new GetNetworkInterfacesResponse()
            {
                NetworkInterfaces = new OnvifNetworkInterface[]
                {
                    new OnvifNetworkInterface()
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
                        },
                        IPv6 = new IPv6NetworkInterface()
                        {
                            Config = new IPv6Configuration()
                            {
                                Manual = new PrefixedIPv6Address[]
                                {
                                    new PrefixedIPv6Address()
                                    {
                                        Address = PrimaryIPv6Address
                                    }
                                },
                            },
                            Enabled = true,
                        }
                    },
                }
            };
        }

        public override GetNTPResponse GetNTP()
        {
            return new GetNTPResponse(new NTPInformation()
            {
                NTPManual = new NetworkHost[]
                {
                    new NetworkHost() { IPv4Address = PrimaryNTPAddress }
                }
            });
        }

        public override GetHostnameResponse GetHostname()
        {
            Uri endpointUri = OnvifOperationContext.RequestUri;

            return new GetHostnameResponse(new HostnameInformation()
            {
                Name = endpointUri.Host
            });
        }

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

        public override GetNetworkDefaultGatewayResponse GetNetworkDefaultGateway()
        {
            return new GetNetworkDefaultGatewayResponse(new NetworkGateway()
            {
                IPv4Address = new string[] { PrimaryIPv4Gateway },
                IPv6Address = new string[] { PrimaryIPv6Gateway }
            });
        }

        public override GetDiscoveryModeResponse GetDiscoveryMode()
        {
            return new GetDiscoveryModeResponse(DiscoveryMode.Discoverable);
        }

        public override GetScopesResponse GetScopes(GetScopesRequest request)
        {
            return new GetScopesResponse()
            {
                Scopes = new OnvifScope[]
                {
                    new OnvifScope()
                    {
                        ScopeDef = ScopeDefinition.Fixed,
                        ScopeItem = "onvif://www.onvif.org/type/video_encoder"
                    },
                    new OnvifScope()
                    {
                        ScopeDef = ScopeDefinition.Fixed,
                        ScopeItem = "onvif://www.onvif.org/Profile/Streaming"
                    },
                    new OnvifScope()
                    {
                        ScopeDef = ScopeDefinition.Fixed,
                        ScopeItem = "onvif://www.onvif.org/Profile/G"
                    }
                }
            };
        }

        public override GetServicesResponse GetServices(GetServicesRequest request)
        {
            Uri endpointUri = OnvifOperationContext.RequestUri;

            return new GetServicesResponse()
            {
                Service = new Service[]
                {
                    new Service()
                    {
                        Namespace = OnvifServices.DEVICE_MGMT, 
                        XAddr = OnvifHelpers.ChangeUriPath(endpointUri, "/onvif/device_service").ToString(),
                        Version = new OnvifVersion()
                        {
                            Major = 17,
                            Minor = 12
                        },
                    },
                    new Service()
                    {
                        Namespace = OnvifServices.MEDIA,
                        XAddr = OnvifHelpers.ChangeUriPath(endpointUri, "/onvif/device_service").ToString(),
                        Version = new OnvifVersion()
                        {
                            Major = 17,
                            Minor = 12
                        },
                    },
                    new Service()
                    {
                        Namespace = OnvifServices.EVENTS,
                        XAddr = OnvifHelpers.ChangeUriPath(endpointUri, "/onvif/events_service").ToString(),
                        Version = new OnvifVersion()
                        {
                            Major = 17,
                            Minor = 12
                        },
                    },
                    new Service()
                    {
                        Namespace = OnvifServices.PTZ,
                        XAddr = OnvifHelpers.ChangeUriPath(endpointUri, "/onvif/device_service").ToString(),
                        Version = new OnvifVersion()
                        {
                            Major = 17,
                            Minor = 12
                        },
                    },
                }
            };
        }

        public override GetSystemDateAndTimeResponse GetSystemDateAndTime()
        {
            var now = System.DateTime.UtcNow;
            return new GetSystemDateAndTimeResponse(new SystemDateTime()
            {
                UTCDateTime = new SharpOnvifCommon.Onvif.OnvifDateTime()
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
            });
        }

        public override GetServiceCapabilitiesResponse GetServiceCapabilities()
        {
            return new GetServiceCapabilitiesResponse(new DeviceServiceCapabilities());
        }

        public override SetSystemFactoryDefaultResponse SetSystemFactoryDefault(FactoryDefaultType FactoryDefault)
        {
            _logger.LogInformation("Device: SetSystemFactoryDefault");
            return new SetSystemFactoryDefaultResponse();
        }

        public override SystemRebootResponse SystemReboot()
        {
            _logger.LogInformation("Device: SystemReboot");
            return new SystemRebootResponse("");
        }

        public override GetUsersResponse GetUsers(GetUsersRequest request)
        {
            string userName = _configuration.GetValue("UserRepository:UserName", "");
            if (string.IsNullOrWhiteSpace(userName))
                userName = "admin"; 

            return new GetUsersResponse()
            {
                User = new User[]
                {
                    new User()
                    {
                        Username = userName,
                        UserLevel = UserLevel.Administrator
                    }
                }
            };
        }

        public override GetSystemLogResponse GetSystemLog(SystemLogType LogType)
        {
            return new GetSystemLogResponse(new SystemLog()
            {
                String = "log"
            });
        }

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

        public override SetSystemDateAndTimeResponse SetSystemDateAndTime(SetDateTimeType DateTimeType, bool DaylightSavings, SharpOnvifCommon.Onvif.OnvifTimeZone TimeZone, SharpOnvifCommon.Onvif.OnvifDateTime UTCDateTime)
        {
            _logger.LogInformation("Device: SetSystemDateAndTime");
            return new SetSystemDateAndTimeResponse();
        }

        public override SetRelayOutputStateResponse SetRelayOutputState(string RelayOutputToken, RelayLogicalState LogicalState)
        {
            _logger.LogInformation("Device: SetRelayOutputState");
            return new SetRelayOutputStateResponse();
        }

        public override SetRelayOutputSettingsResponse SetRelayOutputSettings(string RelayOutputToken, RelayOutputSettings Properties)
        {
            _logger.LogInformation("Device: SetRelayOutputSettings");
            return new SetRelayOutputSettingsResponse();
        }

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
            string ret = "127.0.0.1";
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
            string ret = "127.0.0.1";
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

        public static string GetPrimaryIPv4Gateway()
        {
            string ret = "127.0.0.1";
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

        public static string GetPrimaryIPv6Address()
        {
            string ret = "::1";
            var nic = GetPrimaryNetworkInterface();
            if (nic != null)
            {
                var nicProperties = nic.GetIPProperties();
                if (nicProperties != null)
                {
                    var unicastAddresses = nicProperties.UnicastAddresses;
                    if (unicastAddresses != null)
                    {
                        var unicastAddress = unicastAddresses.FirstOrDefault(x => x.Address != null && x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
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

        public static string GetPrimaryIPv6DNS()
        {
            string ret = "::1";
            var nic = GetPrimaryNetworkInterface();
            if (nic != null)
            {
                var nicProperties = nic.GetIPProperties();
                if (nicProperties != null)
                {
                    var dnsAddresses = nicProperties.DnsAddresses;
                    if (dnsAddresses != null)
                    {
                        var dnsAddress = dnsAddresses.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
                        if (dnsAddress != null)
                        {
                            ret = dnsAddress.ToString();
                        }
                    }
                }
            }
            return ret;
        }

        public static string GetPrimaryIPv6Gateway()
        {
            string ret = "::1";
            var nic = GetPrimaryNetworkInterface();
            if (nic != null)
            {
                var nicProperties = nic.GetIPProperties();
                if (nicProperties != null)
                {
                    var gatewayAddresses = nicProperties.GatewayAddresses;
                    if (gatewayAddresses != null)
                    {
                        var gatewayAddress = gatewayAddresses.FirstOrDefault(x => x.Address != null && x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
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

        public static string GetPrimaryNTPAddress(string ntp = "time.windows.com")
        {
            string ret = "127.0.0.1";
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
