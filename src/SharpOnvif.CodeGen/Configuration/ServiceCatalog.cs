namespace SharpOnvif.CodeGen.Configuration;

/// <summary>
/// One generated service: a WSDL plus the pair of assemblies produced from it. Client and server
/// share the WSDL but get separate namespaces and output directories, matching the existing
/// SharpOnvifClient.X / SharpOnvifServer.X project layout.
/// </summary>
internal sealed record ServiceDefinition(
    /// <summary>Suffix shared by the two project names, e.g. "DeviceMgmt".</summary>
    string Name,
    /// <summary>Absolute URL of the WSDL, resolved against the offline mirror.</summary>
    string Wsdl,
    /// <summary>
    /// Generate every type in the service's schema closure rather than only those its operations
    /// can put on the wire.
    ///
    /// Pruning to reachable types is the default and matches what svcutil produced for 22 of the
    /// 25 services. For Analytics and DeviceIO it did not prune, and emitted the whole of
    /// onvif.xsd (and, for DeviceIO, the device management schema it imports). Those assemblies
    /// therefore publish several hundred types that nothing in their own operations references.
    /// The flag keeps publishing them so code written against the previous bindings still
    /// compiles; it is a compatibility concession, not a design choice, and can be dropped in a
    /// release that accepts the break.
    /// </summary>
    bool GenerateEntireSchema = false);

internal static class ServiceCatalog
{
    /// <summary>
    /// Every ONVIF service SharpOnvif ships bindings for. The WSDL URLs match wsdl/sources.txt
    /// and the names match the existing project suffixes, so regenerating overwrites in place.
    /// </summary>
    public static readonly IReadOnlyList<ServiceDefinition> All =
    [
        new("AccessControl",          "https://www.onvif.org/ver10/pacs/accesscontrol.wsdl"),
        new("AccessRules",            "https://www.onvif.org/ver10/accessrules/wsdl/accessrules.wsdl"),
        new("ActionEngine",           "https://www.onvif.org/ver10/actionengine.wsdl"),
        new("AdvancedSecurity",       "https://www.onvif.org/ver10/advancedsecurity/wsdl/advancedsecurity.wsdl"),
        new("Analytics",              "https://www.onvif.org/ver20/analytics/wsdl/analytics.wsdl", GenerateEntireSchema: true),
        new("AppMgmt",                "https://www.onvif.org/ver10/appmgmt/wsdl/appmgmt.wsdl"),
        new("AuthenticationBehavior", "https://www.onvif.org/ver10/authenticationbehavior/wsdl/authenticationbehavior.wsdl"),
        new("Credential",             "https://www.onvif.org/ver10/credential/wsdl/credential.wsdl"),
        new("DeviceIO",               "https://www.onvif.org/ver10/deviceio.wsdl", GenerateEntireSchema: true),
        new("DeviceMgmt",             "https://www.onvif.org/ver10/device/wsdl/devicemgmt.wsdl"),
        new("Display",                "https://www.onvif.org/ver10/display.wsdl"),
        new("DoorControl",            "https://www.onvif.org/ver10/pacs/doorcontrol.wsdl"),
        new("Events",                 "https://www.onvif.org/ver10/events/wsdl/event.wsdl"),
        new("Imaging",                "https://www.onvif.org/ver20/imaging/wsdl/imaging.wsdl"),
        new("Media",                  "https://www.onvif.org/ver10/media/wsdl/media.wsdl"),
        new("Media2",                 "https://www.onvif.org/ver20/media/wsdl/media.wsdl"),
        new("PTZ",                    "https://www.onvif.org/ver20/ptz/wsdl/ptz.wsdl"),
        new("Provisioning",           "https://www.onvif.org/ver10/provisioning/wsdl/provisioning.wsdl"),
        new("Receiver",               "https://www.onvif.org/ver10/receiver.wsdl"),
        new("Recording",              "https://www.onvif.org/ver10/recording.wsdl"),
        new("Replay",                 "https://www.onvif.org/ver10/replay.wsdl"),
        new("Schedule",               "https://www.onvif.org/ver10/schedule/wsdl/schedule.wsdl"),
        new("Search",                 "https://www.onvif.org/ver10/search.wsdl"),
        new("Thermal",                "https://www.onvif.org/ver10/thermal/wsdl/thermal.wsdl"),
        new("Uplink",                 "https://www.onvif.org/ver10/uplink/wsdl/uplink.wsdl"),
    ];
}
