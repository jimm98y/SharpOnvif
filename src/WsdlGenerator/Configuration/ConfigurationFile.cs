using System.Text.Json;
using System.Text.Json.Serialization;
using WsdlGenerator.Xml;
using WsdlGenerator.Xsd;

namespace WsdlGenerator.Configuration;

/// <summary>
/// A generation run described in a file rather than on a command line.
/// </summary>
/// <remarks>
/// Twenty-five services, the namespaces they are generated into, the schema values to add - none
/// of that fits on a command line, and none of it belongs in the generator: it describes one
/// service family, and the generator describes how to compile any of them. This repository's
/// own run is onvif.codegen.json at its root.
/// </remarks>
internal static class ConfigurationFile
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Reads a run from a file. Paths in it are relative to the file itself.</summary>
    public static GeneratorOptions Load(string path)
    {
        string full = Path.GetFullPath(path);

        if (!File.Exists(full))
            throw new SchemaException($"No configuration at '{full}'.");

        Document document;
        try
        {
            document = JsonSerializer.Deserialize<Document>(File.ReadAllText(full), Json)
                ?? throw new SchemaException($"'{full}' is empty.");
        }
        catch (JsonException error)
        {
            throw new SchemaException($"'{full}' could not be read: {error.Message}");
        }

        string directory = Path.GetDirectoryName(full) ?? ".";
        string Resolve(string relative) => Path.GetFullPath(Path.Combine(directory, relative));

        if (document.Services is not { Count: > 0 })
            throw new SchemaException($"'{full}' names no services to generate.");
        if (document.Targets is not { Count: > 0 })
            throw new SchemaException($"'{full}' names nowhere to generate them.");
        if (document.Shared is null)
            throw new SchemaException($"'{full}' does not say where the shared types go.");
        if (document.Runtime is null)
            throw new SchemaException($"'{full}' does not say where the runtime goes.");

        return new GeneratorOptions
        {
            Services = document.Services
                .Select(s => new ServiceDefinition(
                    s.Name ?? throw new SchemaException($"A service in '{full}' has no name."),
                    s.Wsdl ?? throw new SchemaException($"Service '{s.Name}' in '{full}' has no wsdl.")))
                .ToList(),
            MirrorRoot = document.Mirror is null ? null : Resolve(document.Mirror),
            SharedNamespace = document.Shared.Namespace
                ?? throw new SchemaException($"'{full}' does not name the shared namespace."),
            SharedDirectory = Resolve(document.Shared.Out
                ?? throw new SchemaException($"'{full}' does not say where the shared types go.")),
            Runtime = new RuntimeTarget(
                document.Runtime.Namespace
                    ?? throw new SchemaException($"'{full}' does not name the runtime namespace."),
                document.Runtime.Out is null ? null : Resolve(document.Runtime.Out)),
            Targets = document.Targets.Select(t => new GenerationTarget(
                t.Namespace ?? throw new SchemaException($"A target in '{full}' has no namespace."),
                Resolve(t.Out ?? throw new SchemaException($"Target '{t.Namespace}' in '{full}' has no out.")),
                t.Client,
                t.Server)).ToList(),
            SettingsType = document.Settings,
            DispatchNamespace = document.Dispatch,
            TypeNamePrefix = document.TypeNamePrefix,
            EnumerationExtensions = (document.EnumerationValues ?? []).Select(ToEnumerationExtension).ToList(),
        };
    }

    private static EnumerationExtension ToEnumerationExtension(EnumerationValue value)
    {
        if (value.Type is null || value.Value is null)
            throw new SchemaException("An enumeration value needs both a type and a value.");

        if (!value.Type.StartsWith('{') || !value.Type.Contains('}'))
        {
            throw new SchemaException(
                $"'{value.Type}' names a type as {{namespace}}LocalName. A type in no namespace is {{}}LocalName.");
        }

        int close = value.Type.IndexOf('}');

        return new EnumerationExtension(
            new QName(value.Type.Substring(1, close - 1), value.Type.Substring(close + 1)),
            value.Value,
            value.Documentation);
    }

    private sealed record Document(
        [property: JsonPropertyName("mirror")] string? Mirror,
        [property: JsonPropertyName("typeNamePrefix")] string? TypeNamePrefix,
        [property: JsonPropertyName("shared")] Place? Shared,
        [property: JsonPropertyName("runtime")] Place? Runtime,
        [property: JsonPropertyName("settings")] string? Settings,
        [property: JsonPropertyName("dispatch")] string? Dispatch,
        [property: JsonPropertyName("targets")] IReadOnlyList<Target>? Targets,
        [property: JsonPropertyName("services")] IReadOnlyList<Service>? Services,
        [property: JsonPropertyName("enumerationValues")] IReadOnlyList<EnumerationValue>? EnumerationValues);

    private sealed record Place(string? Namespace, string? Out);

    private sealed record Target(string? Namespace, string? Out, bool Client, bool Server);

    private sealed record Service(string? Name, string? Wsdl);

    private sealed record EnumerationValue(string? Type, string? Value, string? Documentation);
}
