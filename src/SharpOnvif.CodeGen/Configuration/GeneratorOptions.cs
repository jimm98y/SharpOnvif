namespace SharpOnvif.CodeGen.Configuration;

/// <summary>
/// One place code is written to: a namespace, a directory, and which side of the service to
/// generate. A run has one target in the general case, and two when the client and the server are
/// separate assemblies as they are for SharpOnvif itself.
/// </summary>
internal sealed record GenerationTarget(string Namespace, string Directory, bool Client, bool Server);

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
}
