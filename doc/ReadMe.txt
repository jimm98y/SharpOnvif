To make SvcUtil.exe working on Windows 11, import the included svcutil.reg into registry. This should fix the SSL issues, but it will affect all NET Framework apps on the machine. Be careful!

To generate service contracts for the server, run:
& "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\SvcUtil.exe" -sc /n:*,OnvifDeviceMgmt /out:OnvifDeviceMgmt.cs https://www.onvif.org/ver10/device/wsdl/devicemgmt.wsdl

To generate the event service contracts for the server, SvcUtil.exe is failing so we have to use dotnet-svcutil:
dotnet-svcutil --sync --namespace *,SharpOnvifServer.Events --outputFile OnvifEvents.cs https://www.onvif.org/ver10/events/wsdl/event.wsdl

Replace System.ServiceModel with CoreWCF in all auto-generated files.

---

To generate the clients, run:
dotnet-svcutil --namespace *,SharpOnvifClient.Events -d "Connected Services/OnvifEvents" https://www.onvif.org/ver10/events/wsdl/event.wsdl