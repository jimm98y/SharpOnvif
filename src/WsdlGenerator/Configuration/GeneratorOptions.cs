using WsdlGenerator.Xsd;

namespace WsdlGenerator.Configuration;

/// <summary>
/// One place code is written to: a namespace, a directory, and which side of the service to
/// generate. A run has one target in the general case, and two when the client and the server are
/// separate assemblies, as they are for this repository.
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
/// One generated service: a WSDL, and the name the code generated from it is filed under.
/// </summary>
internal sealed record ServiceDefinition(
    /// <summary>Name of the namespace and folder, e.g. "DeviceMgmt".</summary>
    string Name,
    /// <summary>Where the WSDL is, as a path or an http(s) URL.</summary>
    string Wsdl);

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
    /// Namespace of the dispatch a generated service is routed by: where its base class and
    /// <c>DispatchResult</c> come from.
    /// </summary>
    /// <remarks>
    /// The one thing a generated service names that is not generated. Routing an action to a
    /// method over ASP.NET Core is a library rather than anything a schema describes, so the run
    /// names one instead of the generator writing one. Generate with <c>--client</c> for output
    /// that names nothing at all.
    /// </remarks>
    public string? DispatchNamespace { get; init; }

    /// <summary>
    /// Prefix put on a generated type whose schema name is one the framework already uses -
    /// <c>DateTime</c>, <c>IPAddress</c> - so that both stay usable without an alias. Null leaves
    /// such a name as the schema wrote it, and the collision to the caller.
    /// </summary>
    /// <remarks>
    /// A run's choice, because it is a naming convention for one family of schemas, and an
    /// expensive one to change afterwards: the prefix appears in every caller's source.
    /// </remarks>
    public string? TypeNamePrefix { get; init; }

    /// <summary>
    /// Type a generated client builds its settings from when it is given none: the name of
    /// something implementing the generated <c>IClientSettings</c>, with a parameterless
    /// constructor and one taking a user name and a password.
    /// </summary>
    /// <remarks>
    /// Naming it rather than generating it is the point. Which defaults are sensible, what a
    /// client authenticates with, what it declares on its envelopes - all of that is the
    /// service's business rather than WSDL's, so the contract is generated and everything behind
    /// it comes from the library that owns the runtime. Null emits only the constructor that
    /// takes settings, which is all a client strictly needs.
    /// </remarks>
    public string? SettingsType { get; init; }

    /// <summary>
    /// Values to add to schema enumerations that the schema does not list, for the case where the
    /// published schema trails what devices actually send.
    /// </summary>
    public IReadOnlyList<EnumerationExtension> EnumerationExtensions { get; init; } = [];
}
