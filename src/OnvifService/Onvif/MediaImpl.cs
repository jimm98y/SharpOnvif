using SharpOnvifServer.Media;

namespace OnvifService.Onvif
{
    public class MediaImpl : MediaBase
    {
        public override GetAudioSourcesResponse GetAudioSources(GetAudioSourcesRequest request)
        {
            return new GetAudioSourcesResponse()
            {
                AudioSources = new AudioSource[]
                { }
            };
        }

        public override GetProfilesResponse GetProfiles(GetProfilesRequest request)
        {
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
        }

        public override MediaUri GetSnapshotUri(string ProfileToken)
        {
            return new MediaUri()
            {
                Uri = "http://localhost/snapshot"
            };
        }

        public override MediaUri GetStreamUri(StreamSetup StreamSetup, string ProfileToken)
        {
            return new MediaUri()
            {
                Uri = "rtsp://localhost:554"
            };
        }

        public override GetVideoSourcesResponse GetVideoSources(GetVideoSourcesRequest request)
        {
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
    }
}

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
