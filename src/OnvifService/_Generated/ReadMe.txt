& "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\SvcUtil.exe" -sc /n:*,OnvifDeviceMgmt /out:OnvifDeviceMgmt.cs https://www.onvif.org/ver10/device/wsdl/devicemgmt.wsdl

dotnet-svcutil --sync --namespace *,SharpOnvifServer.Events --outputFile OnvifEvents.cs https://www.onvif.org/ver10/events/wsdl/event.wsdl

Replace System.ServiceModel with CoreWCF