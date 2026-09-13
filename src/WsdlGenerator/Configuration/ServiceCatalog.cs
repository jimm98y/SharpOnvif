using WsdlGenerator.Xml;
using WsdlGenerator.Xsd;

namespace WsdlGenerator.Configuration;

/// <summary>
/// One generated service: a WSDL plus the pair of assemblies produced from it. Client and server
/// share the WSDL but get separate namespaces and output directories, matching the existing
/// SharpOnvifClient.X / SharpOnvifServer.X project layout.
/// </summary>
internal sealed record ServiceDefinition(
    /// <summary>Suffix shared by the two project names, e.g. "DeviceMgmt".</summary>
    string Name,
    /// <summary>Absolute URL of the WSDL, resolved against the offline mirror.</summary>
    string Wsdl);

internal static class ServiceCatalog
{
    /// <summary>
    /// How this repository generates its own bindings: from the mirror in <c>wsdl/</c>, with the
    /// client and the server in separate assemblies and the schemas they share in a third.
    /// </summary>
    public static GeneratorOptions OnvifOptions(string repositoryRoot, string outputRoot) => new()
    {
        Services = All,
        MirrorRoot = Path.Combine(repositoryRoot, "wsdl"),
        SharedNamespace = "SharpOnvifCommon.Onvif",
        SharedDirectory = Path.Combine(outputRoot, "SharpOnvifCommon", "Generated"),
        Runtime = new RuntimeTarget(
            "SharpOnvifCommon", Path.Combine(outputRoot, "SharpOnvifCommon", "Generated", "Runtime")),
        EnvelopePrologue = EnvelopePrefixes,
        PreAuthActions = PreAuth,
        Targets =
        [
            new GenerationTarget(
                "SharpOnvifClient", Path.Combine(outputRoot, "SharpOnvifClient", "Generated"),
                Client: true, Server: false),
            new GenerationTarget(
                "SharpOnvifServer", Path.Combine(outputRoot, "SharpOnvifServer", "Generated"),
                Client: false, Server: true),
        ],
        EnumerationExtensions = [.. VideoEncodings, .. AudioEncodings],
    };

    /// <summary>
    /// Prefixes declared on the envelope of every message, whether or not the body uses them.
    /// <para>
    /// The bindings this replaces were CoreWCF-based and declared both, so devices and tools that
    /// have been talking to SharpOnvif keep seeing what they saw. "tns1" is needed for a second
    /// reason: an event topic is written as "tns1:Path", and a prefix used in element content has
    /// to be in scope wherever that content ends up.
    /// </para>
    /// </summary>
    private static readonly IReadOnlyList<EnvelopeDeclaration> EnvelopePrefixes =
    [
        new("tt", "http://www.onvif.org/ver10/schema", "OnvifSchema"),
        new("tns1", "http://www.onvif.org/ver10/topics", "OnvifTopics"),
    ];

    /// <summary>
    /// The actions the Onvif core specification places in the PRE_AUTH category, which a device
    /// must answer without credentials.
    /// </summary>
    private static readonly IReadOnlyList<string> PreAuth =
    [
        "http://www.onvif.org/ver10/device/wsdl/GetWsdlUrl",
        "http://www.onvif.org/ver10/device/wsdl/GetServices",
        "http://www.onvif.org/ver10/device/wsdl/GetServiceCapabilities",
        "http://www.onvif.org/ver10/device/wsdl/GetCapabilities",
        "http://www.onvif.org/ver10/device/wsdl/GetHostname",
        "http://www.onvif.org/ver10/device/wsdl/GetSystemDateAndTime",
        "http://www.onvif.org/ver10/device/wsdl/GetEndpointReference",
    ];

    /// <summary>
    /// onvif.xsd still enumerates tt:VideoEncoding as JPEG, MPEG4 and H264, and a generated enum
    /// with no name for a codec cannot report one. Cameras have been answering H265 for years,
    /// AV1 is following it, and H266 and AV2 are the generation after that. ONVIF's own newer
    /// tt:VideoEncodingMimeNames already lists H265.
    /// <para>
    /// The order matters as much as the membership: these are appended, so the number behind each
    /// existing name stays what it was and a value persisted by an older build still reads back as
    /// the codec it named.
    /// </para>
    /// </summary>
    private static readonly IReadOnlyList<EnumerationExtension> VideoEncodings =
    [
        new(Schema("VideoEncoding"), "H265", "H.265 / HEVC. Sent by devices; not listed by onvif.xsd."),
        new(Schema("VideoEncoding"), "AV1", "AV1. Sent by devices; not listed by onvif.xsd."),
        new(Schema("VideoEncoding"), "H266", "H.266 / VVC. Sent by devices; not listed by onvif.xsd."),
        new(Schema("VideoEncoding"), "AV2", "AV2. Sent by devices; not listed by onvif.xsd."),
    ];

    /// <summary>
    /// onvif.xsd enumerates tt:AudioEncoding as G711, G726 and AAC, all of them older than the
    /// codec anything streaming audio today would reach for. Opus is spelled to match the
    /// enumeration it joins, which is the only convention onvif.xsd offers here - tt:AudioEncoding
    /// is not the IANA media subtype list, and the one that is, tt:AudioEncodingMimeNames, is not
    /// the type of any generated property.
    /// </summary>
    private static readonly IReadOnlyList<EnumerationExtension> AudioEncodings =
    [
        new(Schema("AudioEncoding"), "OPUS", "Opus. Not listed by onvif.xsd."),
    ];

    /// <summary>A name in the shared Onvif schema, the one onvif.xsd declares.</summary>
    private static QName Schema(string localName) => new("http://www.onvif.org/ver10/schema", localName);

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
        new("Analytics",              "https://www.onvif.org/ver20/analytics/wsdl/analytics.wsdl"),
        new("AppMgmt",                "https://www.onvif.org/ver10/appmgmt/wsdl/appmgmt.wsdl"),
        new("AuthenticationBehavior", "https://www.onvif.org/ver10/authenticationbehavior/wsdl/authenticationbehavior.wsdl"),
        new("Credential",             "https://www.onvif.org/ver10/credential/wsdl/credential.wsdl"),
        new("DeviceIO",               "https://www.onvif.org/ver10/deviceio.wsdl"),
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
