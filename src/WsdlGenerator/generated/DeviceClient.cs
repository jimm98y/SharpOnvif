// Generated from WSDL – DeviceService
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Onvif.Device;

public class DeviceClient
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private const string TargetNs = "http://www.onvif.org/ver10/device/wsdl";
    private static readonly XNamespace SoapNs = "http://www.w3.org/2003/05/soap-envelope";

    public DeviceClient(string endpoint, HttpClient httpClient)
    {
        _endpoint = endpoint;
        _httpClient = httpClient;
    }

    private async Task<XElement?> SendAsync(string soapAction, XElement bodyContent, CancellationToken ct)
    {
        var envelope = new XElement(SoapNs + "Envelope",
            new XElement(SoapNs + "Body", bodyContent));
        string xml = $"<?xml version=\"1.0\" encoding=\"utf-8\"?>{envelope}";
        using var content = new StringContent(xml, Encoding.UTF8, "application/soap+xml");
        content.Headers.TryAddWithoutValidation("SOAPAction", $"\"{soapAction}\"");
        using var response = await _httpClient.PostAsync(_endpoint, content, ct);
        response.EnsureSuccessStatusCode();
        string responseXml = await response.Content.ReadAsStringAsync(ct);
        var doc = XDocument.Parse(responseXml);
        var body = doc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "Body");
        return body?.Elements().FirstOrDefault();
    }

    public async Task<XElement?> GetServicesAsync(bool includeCapability, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetServices";
        var body = new XElement(XName.Get("GetServices", TargetNs));
        body.Add(new XElement(XName.Get("IncludeCapability", TargetNs), includeCapability ? "true" : "false"));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetServiceCapabilitiesAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetServiceCapabilities";
        var body = new XElement(XName.Get("GetServiceCapabilities", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetDeviceInformationAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetDeviceInformation";
        var body = new XElement(XName.Get("GetDeviceInformation", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetSystemDateAndTimeAsync(XElement dateTimeType, bool daylightSavings, XElement? timeZone, XElement? uTCDateTime, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetSystemDateAndTime";
        var body = new XElement(XName.Get("SetSystemDateAndTime", TargetNs));
        body.Add(dateTimeType);
        body.Add(new XElement(XName.Get("DaylightSavings", TargetNs), daylightSavings ? "true" : "false"));
        if (timeZone != null)
            body.Add(timeZone);
        if (uTCDateTime != null)
            body.Add(uTCDateTime);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetSystemDateAndTimeAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime";
        var body = new XElement(XName.Get("GetSystemDateAndTime", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetSystemFactoryDefaultAsync(XElement factoryDefault, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetSystemFactoryDefault";
        var body = new XElement(XName.Get("SetSystemFactoryDefault", TargetNs));
        body.Add(factoryDefault);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> UpgradeSystemFirmwareAsync(XElement firmware, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/UpgradeSystemFirmware";
        var body = new XElement(XName.Get("UpgradeSystemFirmware", TargetNs));
        body.Add(firmware);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SystemRebootAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SystemReboot";
        var body = new XElement(XName.Get("SystemReboot", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> RestoreSystemAsync(IEnumerable<XElement> backupFiles, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/RestoreSystem";
        var body = new XElement(XName.Get("RestoreSystem", TargetNs));
        foreach (var item in backupFiles)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetSystemBackupAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetSystemBackup";
        var body = new XElement(XName.Get("GetSystemBackup", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetSystemLogAsync(XElement logType, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetSystemLog";
        var body = new XElement(XName.Get("GetSystemLog", TargetNs));
        body.Add(logType);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetSystemSupportInformationAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetSystemSupportInformation";
        var body = new XElement(XName.Get("GetSystemSupportInformation", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetScopesAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetScopes";
        var body = new XElement(XName.Get("GetScopes", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetScopesAsync(IEnumerable<string> scopes, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetScopes";
        var body = new XElement(XName.Get("SetScopes", TargetNs));
        foreach (var item in scopes)
            body.Add(new XElement(XName.Get("Scopes", TargetNs), item.ToString()));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> AddScopesAsync(IEnumerable<string> scopeItem, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/AddScopes";
        var body = new XElement(XName.Get("AddScopes", TargetNs));
        foreach (var item in scopeItem)
            body.Add(new XElement(XName.Get("ScopeItem", TargetNs), item.ToString()));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> RemoveScopesAsync(IEnumerable<string> scopeItem, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/RemoveScopes";
        var body = new XElement(XName.Get("RemoveScopes", TargetNs));
        foreach (var item in scopeItem)
            body.Add(new XElement(XName.Get("ScopeItem", TargetNs), item.ToString()));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetDiscoveryModeAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetDiscoveryMode";
        var body = new XElement(XName.Get("GetDiscoveryMode", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetDiscoveryModeAsync(XElement discoveryMode, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetDiscoveryMode";
        var body = new XElement(XName.Get("SetDiscoveryMode", TargetNs));
        body.Add(discoveryMode);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetRemoteDiscoveryModeAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetRemoteDiscoveryMode";
        var body = new XElement(XName.Get("GetRemoteDiscoveryMode", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetRemoteDiscoveryModeAsync(XElement remoteDiscoveryMode, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetRemoteDiscoveryMode";
        var body = new XElement(XName.Get("SetRemoteDiscoveryMode", TargetNs));
        body.Add(remoteDiscoveryMode);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetDPAddressesAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetDPAddresses";
        var body = new XElement(XName.Get("GetDPAddresses", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetDPAddressesAsync(IEnumerable<XElement> dPAddress, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetDPAddresses";
        var body = new XElement(XName.Get("SetDPAddresses", TargetNs));
        foreach (var item in dPAddress)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetEndpointReferenceAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetEndpointReference";
        var body = new XElement(XName.Get("GetEndpointReference", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetUserRolesAsync(string? userRole, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetUserRoles";
        var body = new XElement(XName.Get("GetUserRoles", TargetNs));
        if (userRole != null)
            body.Add(new XElement(XName.Get("UserRole", TargetNs), userRole));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetUserRoleAsync(XElement userRole, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetUserRole";
        var body = new XElement(XName.Get("SetUserRole", TargetNs));
        body.Add(userRole);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> DeleteUserRoleAsync(string userRole, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/DeleteUserRole";
        var body = new XElement(XName.Get("DeleteUserRole", TargetNs));
        body.Add(new XElement(XName.Get("UserRole", TargetNs), userRole));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetRemoteUserAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetRemoteUser";
        var body = new XElement(XName.Get("GetRemoteUser", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetRemoteUserAsync(XElement? remoteUser, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetRemoteUser";
        var body = new XElement(XName.Get("SetRemoteUser", TargetNs));
        if (remoteUser != null)
            body.Add(remoteUser);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetUsersAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetUsers";
        var body = new XElement(XName.Get("GetUsers", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> CreateUsersAsync(IEnumerable<XElement> user, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/CreateUsers";
        var body = new XElement(XName.Get("CreateUsers", TargetNs));
        foreach (var item in user)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> DeleteUsersAsync(IEnumerable<string> username, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/DeleteUsers";
        var body = new XElement(XName.Get("DeleteUsers", TargetNs));
        foreach (var item in username)
            body.Add(new XElement(XName.Get("Username", TargetNs), item.ToString()));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetUserAsync(IEnumerable<XElement> user, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetUser";
        var body = new XElement(XName.Get("SetUser", TargetNs));
        foreach (var item in user)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetWsdlUrlAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetWsdlUrl";
        var body = new XElement(XName.Get("GetWsdlUrl", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetPasswordComplexityOptionsAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetPasswordComplexityOptions";
        var body = new XElement(XName.Get("GetPasswordComplexityOptions", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetPasswordComplexityConfigurationAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetPasswordComplexityConfiguration";
        var body = new XElement(XName.Get("GetPasswordComplexityConfiguration", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetPasswordComplexityConfigurationAsync(int? minLen, int? uppercase, int? number, int? specialChars, bool? blockUsernameOccurrence, bool? policyConfigurationLocked, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetPasswordComplexityConfiguration";
        var body = new XElement(XName.Get("SetPasswordComplexityConfiguration", TargetNs));
        if (minLen != null)
            body.Add(new XElement(XName.Get("MinLen", TargetNs), minLen));
        if (uppercase != null)
            body.Add(new XElement(XName.Get("Uppercase", TargetNs), uppercase));
        if (number != null)
            body.Add(new XElement(XName.Get("Number", TargetNs), number));
        if (specialChars != null)
            body.Add(new XElement(XName.Get("SpecialChars", TargetNs), specialChars));
        if (blockUsernameOccurrence != null)
            body.Add(new XElement(XName.Get("BlockUsernameOccurrence", TargetNs), blockUsernameOccurrence.Value ? "true" : "false"));
        if (policyConfigurationLocked != null)
            body.Add(new XElement(XName.Get("PolicyConfigurationLocked", TargetNs), policyConfigurationLocked.Value ? "true" : "false"));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetPasswordHistoryConfigurationAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetPasswordHistoryConfiguration";
        var body = new XElement(XName.Get("GetPasswordHistoryConfiguration", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetPasswordHistoryConfigurationAsync(bool enabled, int length, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetPasswordHistoryConfiguration";
        var body = new XElement(XName.Get("SetPasswordHistoryConfiguration", TargetNs));
        body.Add(new XElement(XName.Get("Enabled", TargetNs), enabled ? "true" : "false"));
        body.Add(new XElement(XName.Get("Length", TargetNs), length));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetAuthFailureWarningOptionsAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetAuthFailureWarningOptions";
        var body = new XElement(XName.Get("GetAuthFailureWarningOptions", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetAuthFailureWarningConfigurationAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetAuthFailureWarningConfiguration";
        var body = new XElement(XName.Get("GetAuthFailureWarningConfiguration", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetAuthFailureWarningConfigurationAsync(bool enabled, int monitorPeriod, int maxAuthFailures, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetAuthFailureWarningConfiguration";
        var body = new XElement(XName.Get("SetAuthFailureWarningConfiguration", TargetNs));
        body.Add(new XElement(XName.Get("Enabled", TargetNs), enabled ? "true" : "false"));
        body.Add(new XElement(XName.Get("MonitorPeriod", TargetNs), monitorPeriod));
        body.Add(new XElement(XName.Get("MaxAuthFailures", TargetNs), maxAuthFailures));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetCapabilitiesAsync(IEnumerable<XElement> category, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetCapabilities";
        var body = new XElement(XName.Get("GetCapabilities", TargetNs));
        foreach (var item in category)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetHostnameAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetHostname";
        var body = new XElement(XName.Get("GetHostname", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetHostnameAsync(string name, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetHostname";
        var body = new XElement(XName.Get("SetHostname", TargetNs));
        body.Add(new XElement(XName.Get("Name", TargetNs), name));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetHostnameFromDHCPAsync(bool fromDHCP, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetHostnameFromDHCP";
        var body = new XElement(XName.Get("SetHostnameFromDHCP", TargetNs));
        body.Add(new XElement(XName.Get("FromDHCP", TargetNs), fromDHCP ? "true" : "false"));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetDNSAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetDNS";
        var body = new XElement(XName.Get("GetDNS", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetDNSAsync(bool fromDHCP, IEnumerable<string> searchDomain, IEnumerable<XElement> dNSManual, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetDNS";
        var body = new XElement(XName.Get("SetDNS", TargetNs));
        body.Add(new XElement(XName.Get("FromDHCP", TargetNs), fromDHCP ? "true" : "false"));
        foreach (var item in searchDomain)
            body.Add(new XElement(XName.Get("SearchDomain", TargetNs), item.ToString()));
        foreach (var item in dNSManual)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetNTPAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetNTP";
        var body = new XElement(XName.Get("GetNTP", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetNTPAsync(bool fromDHCP, IEnumerable<XElement> nTPManual, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetNTP";
        var body = new XElement(XName.Get("SetNTP", TargetNs));
        body.Add(new XElement(XName.Get("FromDHCP", TargetNs), fromDHCP ? "true" : "false"));
        foreach (var item in nTPManual)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetDynamicDNSAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetDynamicDNS";
        var body = new XElement(XName.Get("GetDynamicDNS", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetDynamicDNSAsync(XElement type, XElement? name, string? tTL, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetDynamicDNS";
        var body = new XElement(XName.Get("SetDynamicDNS", TargetNs));
        body.Add(type);
        if (name != null)
            body.Add(name);
        if (tTL != null)
            body.Add(new XElement(XName.Get("TTL", TargetNs), tTL));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetNetworkInterfacesAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetNetworkInterfaces";
        var body = new XElement(XName.Get("GetNetworkInterfaces", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetNetworkInterfacesAsync(XElement interfaceToken, XElement networkInterface, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetNetworkInterfaces";
        var body = new XElement(XName.Get("SetNetworkInterfaces", TargetNs));
        body.Add(interfaceToken);
        body.Add(networkInterface);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetNetworkProtocolsAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetNetworkProtocols";
        var body = new XElement(XName.Get("GetNetworkProtocols", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetNetworkProtocolsAsync(IEnumerable<XElement> networkProtocols, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetNetworkProtocols";
        var body = new XElement(XName.Get("SetNetworkProtocols", TargetNs));
        foreach (var item in networkProtocols)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetNetworkDefaultGatewayAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetNetworkDefaultGateway";
        var body = new XElement(XName.Get("GetNetworkDefaultGateway", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetNetworkDefaultGatewayAsync(IEnumerable<XElement> iPv4Address, IEnumerable<XElement> iPv6Address, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetNetworkDefaultGateway";
        var body = new XElement(XName.Get("SetNetworkDefaultGateway", TargetNs));
        foreach (var item in iPv4Address)
            body.Add(item);
        foreach (var item in iPv6Address)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetZeroConfigurationAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetZeroConfiguration";
        var body = new XElement(XName.Get("GetZeroConfiguration", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetZeroConfigurationAsync(XElement interfaceToken, bool enabled, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetZeroConfiguration";
        var body = new XElement(XName.Get("SetZeroConfiguration", TargetNs));
        body.Add(interfaceToken);
        body.Add(new XElement(XName.Get("Enabled", TargetNs), enabled ? "true" : "false"));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetIPAddressFilterAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetIPAddressFilter";
        var body = new XElement(XName.Get("GetIPAddressFilter", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetIPAddressFilterAsync(XElement iPAddressFilter, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetIPAddressFilter";
        var body = new XElement(XName.Get("SetIPAddressFilter", TargetNs));
        body.Add(iPAddressFilter);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> AddIPAddressFilterAsync(XElement iPAddressFilter, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/AddIPAddressFilter";
        var body = new XElement(XName.Get("AddIPAddressFilter", TargetNs));
        body.Add(iPAddressFilter);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> RemoveIPAddressFilterAsync(XElement iPAddressFilter, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/RemoveIPAddressFilter";
        var body = new XElement(XName.Get("RemoveIPAddressFilter", TargetNs));
        body.Add(iPAddressFilter);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetAccessPolicyAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetAccessPolicy";
        var body = new XElement(XName.Get("GetAccessPolicy", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetAccessPolicyAsync(XElement policyFile, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetAccessPolicy";
        var body = new XElement(XName.Get("SetAccessPolicy", TargetNs));
        body.Add(policyFile);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetRelayOutputsAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetRelayOutputs";
        var body = new XElement(XName.Get("GetRelayOutputs", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetRelayOutputSettingsAsync(XElement relayOutputToken, XElement properties, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetRelayOutputSettings";
        var body = new XElement(XName.Get("SetRelayOutputSettings", TargetNs));
        body.Add(relayOutputToken);
        body.Add(properties);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetRelayOutputStateAsync(XElement relayOutputToken, XElement logicalState, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetRelayOutputState";
        var body = new XElement(XName.Get("SetRelayOutputState", TargetNs));
        body.Add(relayOutputToken);
        body.Add(logicalState);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SendAuxiliaryCommandAsync(XElement auxiliaryCommand, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SendAuxiliaryCommand";
        var body = new XElement(XName.Get("SendAuxiliaryCommand", TargetNs));
        body.Add(auxiliaryCommand);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetDot11CapabilitiesAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetDot11Capabilities";
        var body = new XElement(XName.Get("GetDot11Capabilities", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetDot11StatusAsync(XElement interfaceToken, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetDot11Status";
        var body = new XElement(XName.Get("GetDot11Status", TargetNs));
        body.Add(interfaceToken);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> ScanAvailableDot11NetworksAsync(XElement interfaceToken, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/ScanAvailableDot11Networks";
        var body = new XElement(XName.Get("ScanAvailableDot11Networks", TargetNs));
        body.Add(interfaceToken);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetSystemUrisAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetSystemUris";
        var body = new XElement(XName.Get("GetSystemUris", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> StartFirmwareUpgradeAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/StartFirmwareUpgrade";
        var body = new XElement(XName.Get("StartFirmwareUpgrade", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> UpgradeFirmwareAsync(string version, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/UpgradeFirmware";
        var body = new XElement(XName.Get("UpgradeFirmware", TargetNs));
        body.Add(new XElement(XName.Get("Version", TargetNs), version));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> StartSystemRestoreAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/StartSystemRestore";
        var body = new XElement(XName.Get("StartSystemRestore", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetStorageConfigurationsAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetStorageConfigurations";
        var body = new XElement(XName.Get("GetStorageConfigurations", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> CreateStorageConfigurationAsync(XElement storageConfiguration, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/CreateStorageConfiguration";
        var body = new XElement(XName.Get("CreateStorageConfiguration", TargetNs));
        body.Add(storageConfiguration);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetStorageConfigurationAsync(XElement token, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetStorageConfiguration";
        var body = new XElement(XName.Get("GetStorageConfiguration", TargetNs));
        body.Add(token);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetStorageConfigurationAsync(XElement storageConfiguration, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetStorageConfiguration";
        var body = new XElement(XName.Get("SetStorageConfiguration", TargetNs));
        body.Add(storageConfiguration);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> DeleteStorageConfigurationAsync(XElement token, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/DeleteStorageConfiguration";
        var body = new XElement(XName.Get("DeleteStorageConfiguration", TargetNs));
        body.Add(token);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetGeoLocationAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetGeoLocation";
        var body = new XElement(XName.Get("GetGeoLocation", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetGeoLocationAsync(IEnumerable<XElement> location, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetGeoLocation";
        var body = new XElement(XName.Get("SetGeoLocation", TargetNs));
        foreach (var item in location)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> DeleteGeoLocationAsync(IEnumerable<XElement> location, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/DeleteGeoLocation";
        var body = new XElement(XName.Get("DeleteGeoLocation", TargetNs));
        foreach (var item in location)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetHashingAlgorithmAsync(XElement algorithm, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetHashingAlgorithm";
        var body = new XElement(XName.Get("SetHashingAlgorithm", TargetNs));
        body.Add(algorithm);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> CreateCertificateAsync(string? certificateID, string? subject, string? validNotBefore, string? validNotAfter, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/CreateCertificate";
        var body = new XElement(XName.Get("CreateCertificate", TargetNs));
        if (certificateID != null)
            body.Add(new XElement(XName.Get("CertificateID", TargetNs), certificateID));
        if (subject != null)
            body.Add(new XElement(XName.Get("Subject", TargetNs), subject));
        if (validNotBefore != null)
            body.Add(new XElement(XName.Get("ValidNotBefore", TargetNs), validNotBefore));
        if (validNotAfter != null)
            body.Add(new XElement(XName.Get("ValidNotAfter", TargetNs), validNotAfter));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetCertificatesAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetCertificates";
        var body = new XElement(XName.Get("GetCertificates", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetCertificatesStatusAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetCertificatesStatus";
        var body = new XElement(XName.Get("GetCertificatesStatus", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetCertificatesStatusAsync(IEnumerable<XElement> certificateStatus, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetCertificatesStatus";
        var body = new XElement(XName.Get("SetCertificatesStatus", TargetNs));
        foreach (var item in certificateStatus)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> DeleteCertificatesAsync(IEnumerable<string> certificateID, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/DeleteCertificates";
        var body = new XElement(XName.Get("DeleteCertificates", TargetNs));
        foreach (var item in certificateID)
            body.Add(new XElement(XName.Get("CertificateID", TargetNs), item.ToString()));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetPkcs10RequestAsync(string certificateID, string? subject, XElement? attributes, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetPkcs10Request";
        var body = new XElement(XName.Get("GetPkcs10Request", TargetNs));
        body.Add(new XElement(XName.Get("CertificateID", TargetNs), certificateID));
        if (subject != null)
            body.Add(new XElement(XName.Get("Subject", TargetNs), subject));
        if (attributes != null)
            body.Add(attributes);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> LoadCertificatesAsync(IEnumerable<XElement> nVTCertificate, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/LoadCertificates";
        var body = new XElement(XName.Get("LoadCertificates", TargetNs));
        foreach (var item in nVTCertificate)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetClientCertificateModeAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetClientCertificateMode";
        var body = new XElement(XName.Get("GetClientCertificateMode", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetClientCertificateModeAsync(bool enabled, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetClientCertificateMode";
        var body = new XElement(XName.Get("SetClientCertificateMode", TargetNs));
        body.Add(new XElement(XName.Get("Enabled", TargetNs), enabled ? "true" : "false"));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetCACertificatesAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetCACertificates";
        var body = new XElement(XName.Get("GetCACertificates", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> LoadCertificateWithPrivateKeyAsync(IEnumerable<XElement> certificateWithPrivateKey, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/LoadCertificateWithPrivateKey";
        var body = new XElement(XName.Get("LoadCertificateWithPrivateKey", TargetNs));
        foreach (var item in certificateWithPrivateKey)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetCertificateInformationAsync(string certificateID, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetCertificateInformation";
        var body = new XElement(XName.Get("GetCertificateInformation", TargetNs));
        body.Add(new XElement(XName.Get("CertificateID", TargetNs), certificateID));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> LoadCACertificatesAsync(IEnumerable<XElement> cACertificate, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/LoadCACertificates";
        var body = new XElement(XName.Get("LoadCACertificates", TargetNs));
        foreach (var item in cACertificate)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> CreateDot1XConfigurationAsync(XElement dot1XConfiguration, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/CreateDot1XConfiguration";
        var body = new XElement(XName.Get("CreateDot1XConfiguration", TargetNs));
        body.Add(dot1XConfiguration);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> SetDot1XConfigurationAsync(XElement dot1XConfiguration, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/SetDot1XConfiguration";
        var body = new XElement(XName.Get("SetDot1XConfiguration", TargetNs));
        body.Add(dot1XConfiguration);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetDot1XConfigurationAsync(XElement dot1XConfigurationToken, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetDot1XConfiguration";
        var body = new XElement(XName.Get("GetDot1XConfiguration", TargetNs));
        body.Add(dot1XConfigurationToken);
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> GetDot1XConfigurationsAsync(CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/GetDot1XConfigurations";
        var body = new XElement(XName.Get("GetDot1XConfigurations", TargetNs));
        return await SendAsync(soapAction, body, ct);
    }

    public async Task<XElement?> DeleteDot1XConfigurationAsync(IEnumerable<XElement> dot1XConfigurationToken, CancellationToken ct = default)
    {
        const string soapAction = "http://www.onvif.org/ver10/device/wsdl/DeleteDot1XConfiguration";
        var body = new XElement(XName.Get("DeleteDot1XConfiguration", TargetNs));
        foreach (var item in dot1XConfigurationToken)
            body.Add(item);
        return await SendAsync(soapAction, body, ct);
    }

}
