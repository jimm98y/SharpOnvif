using WsdlGenerator.Xsd;

namespace WsdlGenerator.Configuration;

/// <summary>
/// One place code is written to: a namespace, a directory, and which side of the service to
/// generate. A run has one target in the general case, and two when the client and the server are
/// separate assemblies as they are for SharpOnvif itself.
/// </summary>
internal sealed record GenerationTarget(string Namespace, string Directory, bool Client, bool Server);

/// <summary>
/// The runtime the generated code is compiled against: the client base class it derives from, the
/// SOAP envelope, the XML reader and writer, and the authentication that goes with them.
/// <para>
/// <paramref name="Directory"/> is where the generator writes that runtime, so that generated code
/// depends on nothing but itself. Leave it null to compile against a copy that already exists -
/// which is what a second run generating into the same solution wants, so the two do not each
/// emit their own.
/// </para>
/// </summary>
internal sealed record RuntimeTarget(string Namespace, string? Directory);

/// <summary>
/// A prefix declared on the envelope element of every message the runtime writes.
/// </summary>
/// <param name="Prefix">The prefix, without the xmlns colon.</param>
/// <param name="Namespace">What it stands for.</param>
/// <param name="Constant">
/// Name to publish the namespace under as a constant on <c>OnvifXmlNamespaces</c>, or null to
/// declare it on the envelope without naming it.
/// </param>
internal sealed record EnvelopeDeclaration(string Prefix, string Namespace, string? Constant = null);

/// <summary>Everything a generation run needs.</summary>
internal sealed record GeneratorOptions
{
    /// <summary>The WSDLs to generate from.</summary>
    public required IReadOnlyList<ServiceDefinition> Services { get; init; }

    /// <summary>
    /// Directory holding a mirror of the documents, or null to read from disk and the network.
    /// A mirror keeps generation reproducible and offline.
    /// </summary>
    public string? MirrorRoot { get; init; }

    /// <summary>Namespace for the types the services share.</summary>
    public required string SharedNamespace { get; init; }

    /// <summary>Directory the shared types are written to.</summary>
    public required string SharedDirectory { get; init; }

    /// <summary>Where the per-service code goes.</summary>
    public required IReadOnlyList<GenerationTarget> Targets { get; init; }

    /// <summary>The runtime the generated code is compiled against, and whether to emit it.</summary>
    public required RuntimeTarget Runtime { get; init; }

    /// <summary>
    /// Prefixes the runtime declares on every envelope it writes. Devices in the wild do expect
    /// to see the conventional ones, whether or not the message body uses them.
    /// </summary>
    public IReadOnlyList<EnvelopeDeclaration> EnvelopePrologue { get; init; } = [];

    /// <summary>
    /// Actions a device answers without credentials, which a client therefore sends unauthenticated.
    /// Onvif calls these PRE_AUTH.
    /// </summary>
    public IReadOnlyList<string> PreAuthActions { get; init; } = [];

    /// <summary>
    /// Values to add to schema enumerations that the schema does not list, for the case where the
    /// published schema trails what devices actually send.
    /// </summary>
    public IReadOnlyList<EnumerationExtension> EnumerationExtensions { get; init; } = [];
}
