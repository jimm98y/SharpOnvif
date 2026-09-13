using System.Reflection;
using WsdlGenerator.Xml;

namespace WsdlGenerator.Emit;

/// <summary>
/// Emits the runtime the generated code is compiled against: the client base class, the SOAP
/// envelope, the XML reader and writer, and the authentication that goes with them.
///
/// The runtime is written rather than referenced so that generated code depends on nothing but
/// itself - a client generated from any WSDL compiles against its own runtime and no library of
/// ours. Its source lives beside this class in Runtime/, embedded in the generator and emitted
/// verbatim except for the namespace, which the run chooses.
///
/// Nothing about any particular service is emitted with it. What a client authenticates with,
/// what it declares on its envelopes, how long it waits - all of that reaches the runtime through
/// the interfaces it declares, from whatever implements them. That is what keeps the embedded
/// source free of any particular service's conventions.
/// </summary>
internal sealed class RuntimeEmitter
{
    /// <summary>
    /// The namespace the embedded source is written in, replaced with the run's own. It is a
    /// valid identifier so that the templates stay compilable C# in the editor.
    /// </summary>
    private const string Token = "__RUNTIME__";

    /// <summary>Name the embedded runtime source is filed under, from the project directory.</summary>
    private const string ResourcePrefix = "WsdlGenerator.Runtime.";

    private readonly string _namespace;

    public RuntimeEmitter(string @namespace) => _namespace = @namespace;

    /// <summary>Writes the runtime, and returns how many files that changed.</summary>
    public int Emit(string directory)
    {
        int written = 0;

        foreach (var (path, source) in Sources())
        {
            var file = new GeneratedFile(
                Path.Combine(directory, path),
                GeneratedFile.RuntimeHeader + Environment.NewLine + source.Replace(Token, _namespace));

            if (file.WriteIfChanged()) written++;
        }

        return written;
    }

    /// <summary>
    /// The embedded source, as a relative path and its content.
    /// </summary>
    /// <remarks>
    /// A resource is named for its path with the separators written as dots, so the path is
    /// recovered by putting them back - which holds as long as no directory or file name in
    /// Runtime/ contains a dot of its own, and the extension is the only one that does.
    /// </remarks>
    private static IEnumerable<(string Path, string Source)> Sources()
    {
        Assembly assembly = typeof(RuntimeEmitter).Assembly;

        var names = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal) && n.EndsWith(".cs", StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        if (names.Count == 0)
        {
            throw new SchemaException(
                "The generator carries no runtime source. Runtime/**/*.cs has to be embedded in " +
                "the generator assembly for a runtime to be emitted.");
        }

        foreach (string name in names)
        {
            string relative = name.Substring(ResourcePrefix.Length);
            string path = relative.Substring(0, relative.Length - ".cs".Length).Replace('.', Path.DirectorySeparatorChar) + ".cs";

            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            yield return (path, reader.ReadToEnd());
        }
    }

}
