using CoreWCF;
using OnvifScMedia;
using SharpOnvifServer.Security;

namespace OnvifService.Onvif
{
    [DisableMustUnderstandValidation]
    public class OnvifMediaImpl : Media
    {
        public virtual void AddAudioDecoderConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void AddAudioEncoderConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void AddAudioOutputConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void AddAudioSourceConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void AddMetadataConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void AddPTZConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void AddVideoAnalyticsConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void AddVideoEncoderConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void AddVideoSourceConfiguration(string ProfileToken, string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual CreateOSDResponse CreateOSD(CreateOSDRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Profile")]
        public virtual Profile CreateProfile(string Name, string Token)
        {
            throw new System.NotImplementedException();
        }

        public virtual DeleteOSDResponse DeleteOSD(DeleteOSDRequest request)
        {
            throw new System.NotImplementedException();
        }

        public virtual void DeleteProfile(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual AudioDecoderConfiguration GetAudioDecoderConfiguration(string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual AudioDecoderConfigurationOptions GetAudioDecoderConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetAudioDecoderConfigurationsResponse GetAudioDecoderConfigurations(GetAudioDecoderConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual AudioEncoderConfiguration GetAudioEncoderConfiguration(string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual AudioEncoderConfigurationOptions GetAudioEncoderConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetAudioEncoderConfigurationsResponse GetAudioEncoderConfigurations(GetAudioEncoderConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual AudioOutputConfiguration GetAudioOutputConfiguration(string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual AudioOutputConfigurationOptions GetAudioOutputConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetAudioOutputConfigurationsResponse GetAudioOutputConfigurations(GetAudioOutputConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "AudioOutputs")]
        public virtual GetAudioOutputsResponse GetAudioOutputs(GetAudioOutputsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual AudioSourceConfiguration GetAudioSourceConfiguration(string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual AudioSourceConfigurationOptions GetAudioSourceConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetAudioSourceConfigurationsResponse GetAudioSourceConfigurations(GetAudioSourceConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "AudioSources")]
        public virtual GetAudioSourcesResponse GetAudioSources(GetAudioSourcesRequest request)
        {
            //throw new System.NotImplementedException();
            return new GetAudioSourcesResponse()
            {
                AudioSources = new AudioSource[]
                {
                    
                }
            };
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleAudioDecoderConfigurationsResponse GetCompatibleAudioDecoderConfigurations(GetCompatibleAudioDecoderConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleAudioEncoderConfigurationsResponse GetCompatibleAudioEncoderConfigurations(GetCompatibleAudioEncoderConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleAudioOutputConfigurationsResponse GetCompatibleAudioOutputConfigurations(GetCompatibleAudioOutputConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleAudioSourceConfigurationsResponse GetCompatibleAudioSourceConfigurations(GetCompatibleAudioSourceConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleMetadataConfigurationsResponse GetCompatibleMetadataConfigurations(GetCompatibleMetadataConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleVideoAnalyticsConfigurationsResponse GetCompatibleVideoAnalyticsConfigurations(GetCompatibleVideoAnalyticsConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleVideoEncoderConfigurationsResponse GetCompatibleVideoEncoderConfigurations(GetCompatibleVideoEncoderConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetCompatibleVideoSourceConfigurationsResponse GetCompatibleVideoSourceConfigurations(GetCompatibleVideoSourceConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        public virtual GetGuaranteedNumberOfVideoEncoderInstancesResponse GetGuaranteedNumberOfVideoEncoderInstances(GetGuaranteedNumberOfVideoEncoderInstancesRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual MetadataConfiguration GetMetadataConfiguration(string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual MetadataConfigurationOptions GetMetadataConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetMetadataConfigurationsResponse GetMetadataConfigurations(GetMetadataConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        public virtual GetOSDResponse GetOSD(GetOSDRequest request)
        {
            throw new System.NotImplementedException();
        }

        public virtual GetOSDOptionsResponse GetOSDOptions(GetOSDOptionsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "OSDs")]
        public virtual GetOSDsResponse GetOSDs(GetOSDsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Profile")]
        public virtual Profile GetProfile(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Profiles")]
        public virtual GetProfilesResponse GetProfiles(GetProfilesRequest request)
        {
            //throw new System.NotImplementedException();
            return new GetProfilesResponse()
            {
                Profiles = new Profile[]
                {
                    new Profile()
                    {
                        Name = "mainStream",
                        token = "Profile_1"
                    }
                }
            };

            /*
<?xml version="1.0" encoding="UTF-8"?>
<env:Envelope xmlns:env="http://www.w3.org/2003/05/soap-envelope" xmlns:soapenc="http://www.w3.org/2003/05/soap-encoding" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:tt="http://www.onvif.org/ver10/schema" xmlns:tds="http://www.onvif.org/ver10/device/wsdl" xmlns:trt="http://www.onvif.org/ver10/media/wsdl" xmlns:timg="http://www.onvif.org/ver20/imaging/wsdl" xmlns:tev="http://www.onvif.org/ver10/events/wsdl" xmlns:tptz="http://www.onvif.org/ver20/ptz/wsdl" xmlns:tan="http://www.onvif.org/ver20/analytics/wsdl" xmlns:tst="http://www.onvif.org/ver10/storage/wsdl" xmlns:ter="http://www.onvif.org/ver10/error" xmlns:dn="http://www.onvif.org/ver10/network/wsdl" xmlns:tns1="http://www.onvif.org/ver10/topics" xmlns:tmd="http://www.onvif.org/ver10/deviceIO/wsdl" xmlns:wsdl="http://schemas.xmlsoap.org/wsdl" xmlns:wsoap12="http://schemas.xmlsoap.org/wsdl/soap12" xmlns:http="http://schemas.xmlsoap.org/wsdl/http" xmlns:d="http://schemas.xmlsoap.org/ws/2005/04/discovery" xmlns:wsadis="http://schemas.xmlsoap.org/ws/2004/08/addressing" xmlns:wsnt="http://docs.oasis-open.org/wsn/b-2" xmlns:wsa="http://www.w3.org/2005/08/addressing" xmlns:wstop="http://docs.oasis-open.org/wsn/t-1" xmlns:wsrf-bf="http://docs.oasis-open.org/wsrf/bf-2" xmlns:wsntw="http://docs.oasis-open.org/wsn/bw-2" xmlns:wsrf-rw="http://docs.oasis-open.org/wsrf/rw-2" xmlns:wsaw="http://www.w3.org/2006/05/addressing/wsdl" xmlns:wsrf-r="http://docs.oasis-open.org/wsrf/r-2" xmlns:trc="http://www.onvif.org/ver10/recording/wsdl" xmlns:tse="http://www.onvif.org/ver10/search/wsdl" xmlns:trp="http://www.onvif.org/ver10/replay/wsdl" xmlns:tnshik="http://www.hikvision.com/2011/event/topics" xmlns:hikwsd="http://www.onvifext.com/onvif/ext/ver10/wsdl" xmlns:hikxsd="http://www.onvifext.com/onvif/ext/ver10/schema" xmlns:tas="http://www.onvif.org/ver10/advancedsecurity/wsdl" xmlns:tr2="http://www.onvif.org/ver20/media/wsdl" xmlns:axt="http://www.onvif.org/ver20/analytics"><env:Body><trt:GetProfilesResponse><trt:Profiles token="Profile_1" fixed="true"><tt:Name>mainStream</tt:Name>
<tt:VideoSourceConfiguration token="VideoSourceToken"><tt:Name>VideoSourceConfig</tt:Name>
<tt:UseCount>2</tt:UseCount>
<tt:SourceToken>VideoSource_1</tt:SourceToken>
<tt:Bounds x="0" y="0" width="2688" height="1520"></tt:Bounds>
</tt:VideoSourceConfiguration>
<tt:VideoEncoderConfiguration token="VideoEncoderToken_1" encoding="H265"><tt:Name>VideoEncoder_1</tt:Name>
<tt:UseCount>1</tt:UseCount>
<tt:Encoding>H264</tt:Encoding>
<tt:Resolution><tt:Width>1920</tt:Width>
<tt:Height>1080</tt:Height>
</tt:Resolution>
<tt:Quality>5.000000</tt:Quality>
<tt:RateControl><tt:FrameRateLimit>10</tt:FrameRateLimit>
<tt:EncodingInterval>1</tt:EncodingInterval>
<tt:BitrateLimit>16384</tt:BitrateLimit>
</tt:RateControl>
<tt:H264><tt:GovLength>10</tt:GovLength>
<tt:H264Profile>Main</tt:H264Profile>
</tt:H264>
<tt:Multicast><tt:Address><tt:Type>IPv4</tt:Type>
<tt:IPv4Address>0.0.0.0</tt:IPv4Address>
</tt:Address>
<tt:Port>8860</tt:Port>
<tt:TTL>128</tt:TTL>
<tt:AutoStart>false</tt:AutoStart>
</tt:Multicast>
<tt:SessionTimeout>PT5S</tt:SessionTimeout>
</tt:VideoEncoderConfiguration>
<tt:VideoAnalyticsConfiguration token="VideoAnalyticsToken"><tt:Name>VideoAnalyticsName</tt:Name>
<tt:UseCount>2</tt:UseCount>
<tt:AnalyticsEngineConfiguration><tt:AnalyticsModule Name="MyCellMotionModule" Type="tt:CellMotionEngine"><tt:Parameters><tt:SimpleItem Name="Sensitivity" Value="80"/>
<tt:ElementItem Name="Layout"><tt:CellLayout Columns="22" Rows="18"><tt:Transformation><tt:Translate x="-1.000000" y="-1.000000"/>
<tt:Scale x="0.090909" y="0.111111"/>
</tt:Transformation>
</tt:CellLayout>
</tt:ElementItem>
</tt:Parameters>
</tt:AnalyticsModule>
<tt:AnalyticsModule Name="MyLineDetectorModule" Type="tt:LineDetectorEngine"><tt:Parameters><tt:SimpleItem Name="Sensitivity" Value="50"/>
<tt:ElementItem Name="Layout"><tt:Transformation><tt:Translate x="-1.000000" y="-1.000000"/>
<tt:Scale x="0.002000" y="0.002000"/>
</tt:Transformation>
</tt:ElementItem>
<tt:ElementItem Name="Field"><tt:PolygonConfiguration><tt:Polygon><tt:Point x="0" y="0"/>
<tt:Point x="0" y="1000"/>
<tt:Point x="1000" y="1000"/>
<tt:Point x="1000" y="0"/>
</tt:Polygon>
</tt:PolygonConfiguration>
</tt:ElementItem>
</tt:Parameters>
</tt:AnalyticsModule>
<tt:AnalyticsModule Name="MyFieldDetectorModule" Type="tt:FieldDetectorEngine"><tt:Parameters><tt:SimpleItem Name="Sensitivity" Value="50"/>
<tt:ElementItem Name="Layout"><tt:Transformation><tt:Translate x="-1.000000" y="-1.000000"/>
<tt:Scale x="0.002000" y="0.002000"/>
</tt:Transformation>
</tt:ElementItem>
<tt:ElementItem Name="Field"><tt:PolygonConfiguration><tt:Polygon><tt:Point x="0" y="0"/>
<tt:Point x="0" y="1000"/>
<tt:Point x="1000" y="1000"/>
<tt:Point x="1000" y="0"/>
</tt:Polygon>
</tt:PolygonConfiguration>
</tt:ElementItem>
</tt:Parameters>
</tt:AnalyticsModule>
<tt:AnalyticsModule Name="MyTamperDetecModule" Type="hikxsd:TamperEngine"><tt:Parameters><tt:SimpleItem Name="Sensitivity" Value="0"/>
<tt:ElementItem Name="Transformation"><tt:Transformation><tt:Translate x="-1.000000" y="-1.000000"/>
<tt:Scale x="0.002841" y="0.003472"/>
</tt:Transformation>
</tt:ElementItem>
<tt:ElementItem Name="Field"><tt:PolygonConfiguration><tt:Polygon><tt:Point x="0" y="0"/>
<tt:Point x="0" y="576"/>
<tt:Point x="704" y="576"/>
<tt:Point x="704" y="0"/>
</tt:Polygon>
</tt:PolygonConfiguration>
</tt:ElementItem>
</tt:Parameters>
</tt:AnalyticsModule>
</tt:AnalyticsEngineConfiguration>
<tt:RuleEngineConfiguration><tt:Rule Name="MyMotionDetectorRule" Type="tt:CellMotionDetector"><tt:Parameters><tt:SimpleItem Name="MinCount" Value="5"/>
<tt:SimpleItem Name="AlarmOnDelay" Value="1000"/>
<tt:SimpleItem Name="AlarmOffDelay" Value="1000"/>
<tt:SimpleItem Name="ActiveCells" Value="0P8A8A=="/>
</tt:Parameters>
</tt:Rule>
<tt:Rule Name="MyTamperDetectorRule" Type="hikxsd:TamperDetector"><tt:Parameters><tt:ElementItem Name="Field"><tt:PolygonConfiguration><tt:Polygon><tt:Point x="0" y="0"/>
<tt:Point x="0" y="0"/>
<tt:Point x="0" y="0"/>
<tt:Point x="0" y="0"/>
</tt:Polygon>
</tt:PolygonConfiguration>
</tt:ElementItem>
</tt:Parameters>
</tt:Rule>
</tt:RuleEngineConfiguration>
</tt:VideoAnalyticsConfiguration>
<tt:Extension></tt:Extension>
</trt:Profiles>
<trt:Profiles token="Profile_2" fixed="true"><tt:Name>subStream</tt:Name>
<tt:VideoSourceConfiguration token="VideoSourceToken"><tt:Name>VideoSourceConfig</tt:Name>
<tt:UseCount>2</tt:UseCount>
<tt:SourceToken>VideoSource_1</tt:SourceToken>
<tt:Bounds x="0" y="0" width="2688" height="1520"></tt:Bounds>
</tt:VideoSourceConfiguration>
<tt:VideoEncoderConfiguration token="VideoEncoderToken_2"><tt:Name>VideoEncoder_2</tt:Name>
<tt:UseCount>1</tt:UseCount>
<tt:Encoding>H264</tt:Encoding>
<tt:Resolution><tt:Width>640</tt:Width>
<tt:Height>360</tt:Height>
</tt:Resolution>
<tt:Quality>3.000000</tt:Quality>
<tt:RateControl><tt:FrameRateLimit>25</tt:FrameRateLimit>
<tt:EncodingInterval>1</tt:EncodingInterval>
<tt:BitrateLimit>768</tt:BitrateLimit>
</tt:RateControl>
<tt:H264><tt:GovLength>50</tt:GovLength>
<tt:H264Profile>Main</tt:H264Profile>
</tt:H264>
<tt:Multicast><tt:Address><tt:Type>IPv4</tt:Type>
<tt:IPv4Address>0.0.0.0</tt:IPv4Address>
</tt:Address>
<tt:Port>8866</tt:Port>
<tt:TTL>128</tt:TTL>
<tt:AutoStart>false</tt:AutoStart>
</tt:Multicast>
<tt:SessionTimeout>PT5S</tt:SessionTimeout>
</tt:VideoEncoderConfiguration>
<tt:VideoAnalyticsConfiguration token="VideoAnalyticsToken"><tt:Name>VideoAnalyticsName</tt:Name>
<tt:UseCount>2</tt:UseCount>
<tt:AnalyticsEngineConfiguration><tt:AnalyticsModule Name="MyCellMotionModule" Type="tt:CellMotionEngine"><tt:Parameters><tt:SimpleItem Name="Sensitivity" Value="80"/>
<tt:ElementItem Name="Layout"><tt:CellLayout Columns="22" Rows="18"><tt:Transformation><tt:Translate x="-1.000000" y="-1.000000"/>
<tt:Scale x="0.090909" y="0.111111"/>
</tt:Transformation>
</tt:CellLayout>
</tt:ElementItem>
</tt:Parameters>
</tt:AnalyticsModule>
<tt:AnalyticsModule Name="MyLineDetectorModule" Type="tt:LineDetectorEngine"><tt:Parameters><tt:SimpleItem Name="Sensitivity" Value="50"/>
<tt:ElementItem Name="Layout"><tt:Transformation><tt:Translate x="-1.000000" y="-1.000000"/>
<tt:Scale x="0.002000" y="0.002000"/>
</tt:Transformation>
</tt:ElementItem>
<tt:ElementItem Name="Field"><tt:PolygonConfiguration><tt:Polygon><tt:Point x="0" y="0"/>
<tt:Point x="0" y="1000"/>
<tt:Point x="1000" y="1000"/>
<tt:Point x="1000" y="0"/>
</tt:Polygon>
</tt:PolygonConfiguration>
</tt:ElementItem>
</tt:Parameters>
</tt:AnalyticsModule>
<tt:AnalyticsModule Name="MyFieldDetectorModule" Type="tt:FieldDetectorEngine"><tt:Parameters><tt:SimpleItem Name="Sensitivity" Value="50"/>
<tt:ElementItem Name="Layout"><tt:Transformation><tt:Translate x="-1.000000" y="-1.000000"/>
<tt:Scale x="0.002000" y="0.002000"/>
</tt:Transformation>
</tt:ElementItem>
<tt:ElementItem Name="Field"><tt:PolygonConfiguration><tt:Polygon><tt:Point x="0" y="0"/>
<tt:Point x="0" y="1000"/>
<tt:Point x="1000" y="1000"/>
<tt:Point x="1000" y="0"/>
</tt:Polygon>
</tt:PolygonConfiguration>
</tt:ElementItem>
</tt:Parameters>
</tt:AnalyticsModule>
<tt:AnalyticsModule Name="MyTamperDetecModule" Type="hikxsd:TamperEngine"><tt:Parameters><tt:SimpleItem Name="Sensitivity" Value="0"/>
<tt:ElementItem Name="Transformation"><tt:Transformation><tt:Translate x="-1.000000" y="-1.000000"/>
<tt:Scale x="0.002841" y="0.003472"/>
</tt:Transformation>
</tt:ElementItem>
<tt:ElementItem Name="Field"><tt:PolygonConfiguration><tt:Polygon><tt:Point x="0" y="0"/>
<tt:Point x="0" y="576"/>
<tt:Point x="704" y="576"/>
<tt:Point x="704" y="0"/>
</tt:Polygon>
</tt:PolygonConfiguration>
</tt:ElementItem>
</tt:Parameters>
</tt:AnalyticsModule>
</tt:AnalyticsEngineConfiguration>
<tt:RuleEngineConfiguration><tt:Rule Name="MyMotionDetectorRule" Type="tt:CellMotionDetector"><tt:Parameters><tt:SimpleItem Name="MinCount" Value="5"/>
<tt:SimpleItem Name="AlarmOnDelay" Value="1000"/>
<tt:SimpleItem Name="AlarmOffDelay" Value="1000"/>
<tt:SimpleItem Name="ActiveCells" Value="0P8A8A=="/>
</tt:Parameters>
</tt:Rule>
<tt:Rule Name="MyTamperDetectorRule" Type="hikxsd:TamperDetector"><tt:Parameters><tt:ElementItem Name="Field"><tt:PolygonConfiguration><tt:Polygon><tt:Point x="0" y="0"/>
<tt:Point x="0" y="0"/>
<tt:Point x="0" y="0"/>
<tt:Point x="0" y="0"/>
</tt:Polygon>
</tt:PolygonConfiguration>
</tt:ElementItem>
</tt:Parameters>
</tt:Rule>
</tt:RuleEngineConfiguration>
</tt:VideoAnalyticsConfiguration>
<tt:Extension></tt:Extension>
</trt:Profiles>
</trt:GetProfilesResponse>
</env:Body>
</env:Envelope>
             */
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual Capabilities GetServiceCapabilities()
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "MediaUri")]
        public virtual MediaUri GetSnapshotUri(string ProfileToken)
        {
            //throw new System.NotImplementedException();
            return new MediaUri()
            {
                Uri = "http://localhost/snapshot"
            };
        }

        [return: MessageParameter(Name = "MediaUri")]
        public virtual MediaUri GetStreamUri(StreamSetup StreamSetup, string ProfileToken)
        {
            //throw new System.NotImplementedException();
            return new MediaUri()
            {
                Uri = "rtsp://localhost:554"
            };
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual VideoAnalyticsConfiguration GetVideoAnalyticsConfiguration(string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetVideoAnalyticsConfigurationsResponse GetVideoAnalyticsConfigurations(GetVideoAnalyticsConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual VideoEncoderConfiguration GetVideoEncoderConfiguration(string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual VideoEncoderConfigurationOptions GetVideoEncoderConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetVideoEncoderConfigurationsResponse GetVideoEncoderConfigurations(GetVideoEncoderConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configuration")]
        public virtual VideoSourceConfiguration GetVideoSourceConfiguration(string ConfigurationToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Options")]
        public virtual VideoSourceConfigurationOptions GetVideoSourceConfigurationOptions(string ConfigurationToken, string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Configurations")]
        public virtual GetVideoSourceConfigurationsResponse GetVideoSourceConfigurations(GetVideoSourceConfigurationsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "VideoSourceModes")]
        public virtual GetVideoSourceModesResponse GetVideoSourceModes(GetVideoSourceModesRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "VideoSources")]
        public virtual GetVideoSourcesResponse GetVideoSources(GetVideoSourcesRequest request)
        {
            //throw new System.NotImplementedException();
            return new GetVideoSourcesResponse()
            {
                VideoSources = new VideoSource[]
                {
                    new VideoSource()
                    {
                        token = "VideoSourceToken",
                        Resolution = new VideoResolution()
                        {
                            Width = 1920,
                            Height = 1080
                        }
                    }
                }
            };
        }

        public virtual void RemoveAudioDecoderConfiguration(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void RemoveAudioEncoderConfiguration(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void RemoveAudioOutputConfiguration(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void RemoveAudioSourceConfiguration(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void RemoveMetadataConfiguration(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void RemovePTZConfiguration(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void RemoveVideoAnalyticsConfiguration(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void RemoveVideoEncoderConfiguration(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void RemoveVideoSourceConfiguration(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetAudioDecoderConfiguration(AudioDecoderConfiguration Configuration, bool ForcePersistence)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetAudioEncoderConfiguration(AudioEncoderConfiguration Configuration, bool ForcePersistence)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetAudioOutputConfiguration(AudioOutputConfiguration Configuration, bool ForcePersistence)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetAudioSourceConfiguration(AudioSourceConfiguration Configuration, bool ForcePersistence)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetMetadataConfiguration(MetadataConfiguration Configuration, bool ForcePersistence)
        {
            throw new System.NotImplementedException();
        }

        public virtual SetOSDResponse SetOSD(SetOSDRequest request)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetSynchronizationPoint(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetVideoAnalyticsConfiguration(VideoAnalyticsConfiguration Configuration, bool ForcePersistence)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetVideoEncoderConfiguration(VideoEncoderConfiguration Configuration, bool ForcePersistence)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetVideoSourceConfiguration(VideoSourceConfiguration Configuration, bool ForcePersistence)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Reboot")]
        public virtual bool SetVideoSourceMode(string VideoSourceToken, string VideoSourceModeToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void StartMulticastStreaming(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void StopMulticastStreaming(string ProfileToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
