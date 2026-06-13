// Generated from WSDL – DeviceService
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Onvif.Device;

public abstract class DeviceServiceBase
{
    private HttpListener? _listener;
    private const string TargetNs = "http://www.onvif.org/ver10/device/wsdl";
    private static readonly XNamespace SoapNs = "http://www.w3.org/2003/05/soap-envelope";

    private Dictionary<string, Func<XElement, Task<XElement?>>> BuildHandlers() => new()
    {
        ["http://www.onvif.org/ver10/device/wsdl/GetServices"] = req => GetServicesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetServiceCapabilities"] = req => GetServiceCapabilitiesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetDeviceInformation"] = req => GetDeviceInformationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetSystemDateAndTime"] = req => SetSystemDateAndTimeAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime"] = req => GetSystemDateAndTimeAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetSystemFactoryDefault"] = req => SetSystemFactoryDefaultAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/UpgradeSystemFirmware"] = req => UpgradeSystemFirmwareAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SystemReboot"] = req => SystemRebootAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/RestoreSystem"] = req => RestoreSystemAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetSystemBackup"] = req => GetSystemBackupAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetSystemLog"] = req => GetSystemLogAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetSystemSupportInformation"] = req => GetSystemSupportInformationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetScopes"] = req => GetScopesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetScopes"] = req => SetScopesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/AddScopes"] = req => AddScopesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/RemoveScopes"] = req => RemoveScopesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetDiscoveryMode"] = req => GetDiscoveryModeAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetDiscoveryMode"] = req => SetDiscoveryModeAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetRemoteDiscoveryMode"] = req => GetRemoteDiscoveryModeAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetRemoteDiscoveryMode"] = req => SetRemoteDiscoveryModeAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetDPAddresses"] = req => GetDPAddressesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetDPAddresses"] = req => SetDPAddressesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetEndpointReference"] = req => GetEndpointReferenceAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetUserRoles"] = req => GetUserRolesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetUserRole"] = req => SetUserRoleAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/DeleteUserRole"] = req => DeleteUserRoleAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetRemoteUser"] = req => GetRemoteUserAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetRemoteUser"] = req => SetRemoteUserAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetUsers"] = req => GetUsersAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/CreateUsers"] = req => CreateUsersAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/DeleteUsers"] = req => DeleteUsersAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetUser"] = req => SetUserAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetWsdlUrl"] = req => GetWsdlUrlAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetPasswordComplexityOptions"] = req => GetPasswordComplexityOptionsAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetPasswordComplexityConfiguration"] = req => GetPasswordComplexityConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetPasswordComplexityConfiguration"] = req => SetPasswordComplexityConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetPasswordHistoryConfiguration"] = req => GetPasswordHistoryConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetPasswordHistoryConfiguration"] = req => SetPasswordHistoryConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetAuthFailureWarningOptions"] = req => GetAuthFailureWarningOptionsAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetAuthFailureWarningConfiguration"] = req => GetAuthFailureWarningConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetAuthFailureWarningConfiguration"] = req => SetAuthFailureWarningConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetCapabilities"] = req => GetCapabilitiesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetHostname"] = req => GetHostnameAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetHostname"] = req => SetHostnameAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetHostnameFromDHCP"] = req => SetHostnameFromDHCPAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetDNS"] = req => GetDNSAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetDNS"] = req => SetDNSAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetNTP"] = req => GetNTPAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetNTP"] = req => SetNTPAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetDynamicDNS"] = req => GetDynamicDNSAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetDynamicDNS"] = req => SetDynamicDNSAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetNetworkInterfaces"] = req => GetNetworkInterfacesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetNetworkInterfaces"] = req => SetNetworkInterfacesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetNetworkProtocols"] = req => GetNetworkProtocolsAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetNetworkProtocols"] = req => SetNetworkProtocolsAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetNetworkDefaultGateway"] = req => GetNetworkDefaultGatewayAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetNetworkDefaultGateway"] = req => SetNetworkDefaultGatewayAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetZeroConfiguration"] = req => GetZeroConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetZeroConfiguration"] = req => SetZeroConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetIPAddressFilter"] = req => GetIPAddressFilterAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetIPAddressFilter"] = req => SetIPAddressFilterAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/AddIPAddressFilter"] = req => AddIPAddressFilterAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/RemoveIPAddressFilter"] = req => RemoveIPAddressFilterAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetAccessPolicy"] = req => GetAccessPolicyAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetAccessPolicy"] = req => SetAccessPolicyAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetRelayOutputs"] = req => GetRelayOutputsAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetRelayOutputSettings"] = req => SetRelayOutputSettingsAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetRelayOutputState"] = req => SetRelayOutputStateAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SendAuxiliaryCommand"] = req => SendAuxiliaryCommandAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetDot11Capabilities"] = req => GetDot11CapabilitiesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetDot11Status"] = req => GetDot11StatusAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/ScanAvailableDot11Networks"] = req => ScanAvailableDot11NetworksAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetSystemUris"] = req => GetSystemUrisAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/StartFirmwareUpgrade"] = req => StartFirmwareUpgradeAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/UpgradeFirmware"] = req => UpgradeFirmwareAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/StartSystemRestore"] = req => StartSystemRestoreAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetStorageConfigurations"] = req => GetStorageConfigurationsAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/CreateStorageConfiguration"] = req => CreateStorageConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetStorageConfiguration"] = req => GetStorageConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetStorageConfiguration"] = req => SetStorageConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/DeleteStorageConfiguration"] = req => DeleteStorageConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetGeoLocation"] = req => GetGeoLocationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetGeoLocation"] = req => SetGeoLocationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/DeleteGeoLocation"] = req => DeleteGeoLocationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetHashingAlgorithm"] = req => SetHashingAlgorithmAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/CreateCertificate"] = req => CreateCertificateAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetCertificates"] = req => GetCertificatesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetCertificatesStatus"] = req => GetCertificatesStatusAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetCertificatesStatus"] = req => SetCertificatesStatusAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/DeleteCertificates"] = req => DeleteCertificatesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetPkcs10Request"] = req => GetPkcs10RequestAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/LoadCertificates"] = req => LoadCertificatesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetClientCertificateMode"] = req => GetClientCertificateModeAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetClientCertificateMode"] = req => SetClientCertificateModeAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetCACertificates"] = req => GetCACertificatesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/LoadCertificateWithPrivateKey"] = req => LoadCertificateWithPrivateKeyAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetCertificateInformation"] = req => GetCertificateInformationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/LoadCACertificates"] = req => LoadCACertificatesAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/CreateDot1XConfiguration"] = req => CreateDot1XConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/SetDot1XConfiguration"] = req => SetDot1XConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetDot1XConfiguration"] = req => GetDot1XConfigurationAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/GetDot1XConfigurations"] = req => GetDot1XConfigurationsAsync(req),
        ["http://www.onvif.org/ver10/device/wsdl/DeleteDot1XConfiguration"] = req => DeleteDot1XConfigurationAsync(req),
    };

    public void Start(string listenerPrefix)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(listenerPrefix);
        _listener.Start();
        _ = AcceptLoopAsync(BuildHandlers());
    }

    public void Stop() => _listener?.Stop();

    private async Task AcceptLoopAsync(Dictionary<string, Func<XElement, Task<XElement?>>> handlers)
    {
        while (_listener?.IsListening == true)
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(ctx, handlers));
            }
            catch (HttpListenerException) { break; }
            catch (ObjectDisposedException) { break; }
        }
    }

    private async Task HandleRequestAsync(
        HttpListenerContext ctx,
        Dictionary<string, Func<XElement, Task<XElement?>>> handlers)
    {
        try
        {
            string soapAction = ctx.Request.Headers["SOAPAction"]?.Trim('"') ?? "";
            using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            string requestXml = await reader.ReadToEndAsync();

            XElement requestBodyElem;
            try
            {
                var doc = XDocument.Parse(requestXml);
                var soapBody = doc.Root?.Elements()
                    .FirstOrDefault(e => e.Name.LocalName == "Body");
                requestBodyElem = soapBody?.Elements().FirstOrDefault() ?? new XElement("Empty");
            }
            catch
            {
                await WriteFaultAsync(ctx, "Could not parse SOAP envelope");
                return;
            }

            if (!handlers.TryGetValue(soapAction, out var handler))
            {
                await WriteFaultAsync(ctx, $"Unknown SOAPAction: {soapAction}");
                return;
            }

            XElement? responseBody = await handler(requestBodyElem);

            var envelope = new XElement(SoapNs + "Envelope",
                new XElement(SoapNs + "Body",
                    responseBody != null ? (object)responseBody : ""));
            string responseXml = $"<?xml version=\"1.0\" encoding=\"utf-8\"?>{envelope}";
            await WriteResponseAsync(ctx, responseXml, 200);
        }
        catch (Exception ex)
        {
            await WriteFaultAsync(ctx, ex.Message);
        }
        finally
        {
            ctx.Response.Close();
        }
    }

    private static async Task WriteFaultAsync(HttpListenerContext ctx, string message)
    {
        string escaped = System.Security.SecurityElement.Escape(message) ?? message;
        string fault = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <s:Envelope xmlns:s="http://www.w3.org/2003/05/soap-envelope">
              <s:Body>
                <s:Fault>
                  <s:Code><s:Value>s:Receiver</s:Value></s:Code>
                  <s:Reason><s:Text>{escaped}</s:Text></s:Reason>
                </s:Fault>
              </s:Body>
            </s:Envelope>
            """;
        await WriteResponseAsync(ctx, fault.Trim(), 500);
    }

    private static async Task WriteResponseAsync(HttpListenerContext ctx, string xml, int statusCode)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/soap+xml; charset=utf-8";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes);
    }

    // Implement these methods to handle incoming SOAP requests.
    // The 'request' parameter is the inner body element (the operation element).
    // Return the response body element, or null for empty responses.

    protected abstract Task<XElement?> GetServicesAsync(XElement request);
    protected abstract Task<XElement?> GetServiceCapabilitiesAsync(XElement request);
    protected abstract Task<XElement?> GetDeviceInformationAsync(XElement request);
    protected abstract Task<XElement?> SetSystemDateAndTimeAsync(XElement request);
    protected abstract Task<XElement?> GetSystemDateAndTimeAsync(XElement request);
    protected abstract Task<XElement?> SetSystemFactoryDefaultAsync(XElement request);
    protected abstract Task<XElement?> UpgradeSystemFirmwareAsync(XElement request);
    protected abstract Task<XElement?> SystemRebootAsync(XElement request);
    protected abstract Task<XElement?> RestoreSystemAsync(XElement request);
    protected abstract Task<XElement?> GetSystemBackupAsync(XElement request);
    protected abstract Task<XElement?> GetSystemLogAsync(XElement request);
    protected abstract Task<XElement?> GetSystemSupportInformationAsync(XElement request);
    protected abstract Task<XElement?> GetScopesAsync(XElement request);
    protected abstract Task<XElement?> SetScopesAsync(XElement request);
    protected abstract Task<XElement?> AddScopesAsync(XElement request);
    protected abstract Task<XElement?> RemoveScopesAsync(XElement request);
    protected abstract Task<XElement?> GetDiscoveryModeAsync(XElement request);
    protected abstract Task<XElement?> SetDiscoveryModeAsync(XElement request);
    protected abstract Task<XElement?> GetRemoteDiscoveryModeAsync(XElement request);
    protected abstract Task<XElement?> SetRemoteDiscoveryModeAsync(XElement request);
    protected abstract Task<XElement?> GetDPAddressesAsync(XElement request);
    protected abstract Task<XElement?> SetDPAddressesAsync(XElement request);
    protected abstract Task<XElement?> GetEndpointReferenceAsync(XElement request);
    protected abstract Task<XElement?> GetUserRolesAsync(XElement request);
    protected abstract Task<XElement?> SetUserRoleAsync(XElement request);
    protected abstract Task<XElement?> DeleteUserRoleAsync(XElement request);
    protected abstract Task<XElement?> GetRemoteUserAsync(XElement request);
    protected abstract Task<XElement?> SetRemoteUserAsync(XElement request);
    protected abstract Task<XElement?> GetUsersAsync(XElement request);
    protected abstract Task<XElement?> CreateUsersAsync(XElement request);
    protected abstract Task<XElement?> DeleteUsersAsync(XElement request);
    protected abstract Task<XElement?> SetUserAsync(XElement request);
    protected abstract Task<XElement?> GetWsdlUrlAsync(XElement request);
    protected abstract Task<XElement?> GetPasswordComplexityOptionsAsync(XElement request);
    protected abstract Task<XElement?> GetPasswordComplexityConfigurationAsync(XElement request);
    protected abstract Task<XElement?> SetPasswordComplexityConfigurationAsync(XElement request);
    protected abstract Task<XElement?> GetPasswordHistoryConfigurationAsync(XElement request);
    protected abstract Task<XElement?> SetPasswordHistoryConfigurationAsync(XElement request);
    protected abstract Task<XElement?> GetAuthFailureWarningOptionsAsync(XElement request);
    protected abstract Task<XElement?> GetAuthFailureWarningConfigurationAsync(XElement request);
    protected abstract Task<XElement?> SetAuthFailureWarningConfigurationAsync(XElement request);
    protected abstract Task<XElement?> GetCapabilitiesAsync(XElement request);
    protected abstract Task<XElement?> GetHostnameAsync(XElement request);
    protected abstract Task<XElement?> SetHostnameAsync(XElement request);
    protected abstract Task<XElement?> SetHostnameFromDHCPAsync(XElement request);
    protected abstract Task<XElement?> GetDNSAsync(XElement request);
    protected abstract Task<XElement?> SetDNSAsync(XElement request);
    protected abstract Task<XElement?> GetNTPAsync(XElement request);
    protected abstract Task<XElement?> SetNTPAsync(XElement request);
    protected abstract Task<XElement?> GetDynamicDNSAsync(XElement request);
    protected abstract Task<XElement?> SetDynamicDNSAsync(XElement request);
    protected abstract Task<XElement?> GetNetworkInterfacesAsync(XElement request);
    protected abstract Task<XElement?> SetNetworkInterfacesAsync(XElement request);
    protected abstract Task<XElement?> GetNetworkProtocolsAsync(XElement request);
    protected abstract Task<XElement?> SetNetworkProtocolsAsync(XElement request);
    protected abstract Task<XElement?> GetNetworkDefaultGatewayAsync(XElement request);
    protected abstract Task<XElement?> SetNetworkDefaultGatewayAsync(XElement request);
    protected abstract Task<XElement?> GetZeroConfigurationAsync(XElement request);
    protected abstract Task<XElement?> SetZeroConfigurationAsync(XElement request);
    protected abstract Task<XElement?> GetIPAddressFilterAsync(XElement request);
    protected abstract Task<XElement?> SetIPAddressFilterAsync(XElement request);
    protected abstract Task<XElement?> AddIPAddressFilterAsync(XElement request);
    protected abstract Task<XElement?> RemoveIPAddressFilterAsync(XElement request);
    protected abstract Task<XElement?> GetAccessPolicyAsync(XElement request);
    protected abstract Task<XElement?> SetAccessPolicyAsync(XElement request);
    protected abstract Task<XElement?> GetRelayOutputsAsync(XElement request);
    protected abstract Task<XElement?> SetRelayOutputSettingsAsync(XElement request);
    protected abstract Task<XElement?> SetRelayOutputStateAsync(XElement request);
    protected abstract Task<XElement?> SendAuxiliaryCommandAsync(XElement request);
    protected abstract Task<XElement?> GetDot11CapabilitiesAsync(XElement request);
    protected abstract Task<XElement?> GetDot11StatusAsync(XElement request);
    protected abstract Task<XElement?> ScanAvailableDot11NetworksAsync(XElement request);
    protected abstract Task<XElement?> GetSystemUrisAsync(XElement request);
    protected abstract Task<XElement?> StartFirmwareUpgradeAsync(XElement request);
    protected abstract Task<XElement?> UpgradeFirmwareAsync(XElement request);
    protected abstract Task<XElement?> StartSystemRestoreAsync(XElement request);
    protected abstract Task<XElement?> GetStorageConfigurationsAsync(XElement request);
    protected abstract Task<XElement?> CreateStorageConfigurationAsync(XElement request);
    protected abstract Task<XElement?> GetStorageConfigurationAsync(XElement request);
    protected abstract Task<XElement?> SetStorageConfigurationAsync(XElement request);
    protected abstract Task<XElement?> DeleteStorageConfigurationAsync(XElement request);
    protected abstract Task<XElement?> GetGeoLocationAsync(XElement request);
    protected abstract Task<XElement?> SetGeoLocationAsync(XElement request);
    protected abstract Task<XElement?> DeleteGeoLocationAsync(XElement request);
    protected abstract Task<XElement?> SetHashingAlgorithmAsync(XElement request);
    protected abstract Task<XElement?> CreateCertificateAsync(XElement request);
    protected abstract Task<XElement?> GetCertificatesAsync(XElement request);
    protected abstract Task<XElement?> GetCertificatesStatusAsync(XElement request);
    protected abstract Task<XElement?> SetCertificatesStatusAsync(XElement request);
    protected abstract Task<XElement?> DeleteCertificatesAsync(XElement request);
    protected abstract Task<XElement?> GetPkcs10RequestAsync(XElement request);
    protected abstract Task<XElement?> LoadCertificatesAsync(XElement request);
    protected abstract Task<XElement?> GetClientCertificateModeAsync(XElement request);
    protected abstract Task<XElement?> SetClientCertificateModeAsync(XElement request);
    protected abstract Task<XElement?> GetCACertificatesAsync(XElement request);
    protected abstract Task<XElement?> LoadCertificateWithPrivateKeyAsync(XElement request);
    protected abstract Task<XElement?> GetCertificateInformationAsync(XElement request);
    protected abstract Task<XElement?> LoadCACertificatesAsync(XElement request);
    protected abstract Task<XElement?> CreateDot1XConfigurationAsync(XElement request);
    protected abstract Task<XElement?> SetDot1XConfigurationAsync(XElement request);
    protected abstract Task<XElement?> GetDot1XConfigurationAsync(XElement request);
    protected abstract Task<XElement?> GetDot1XConfigurationsAsync(XElement request);
    protected abstract Task<XElement?> DeleteDot1XConfigurationAsync(XElement request);
}
