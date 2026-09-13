using System.Reflection;
using WsdlGenerator.Configuration;
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
/// Two things the runtime cannot know about itself come from the run instead, and are emitted
/// into RuntimeDefaults: the prefixes to declare on every envelope, and what a client
/// authenticates with. That is what keeps the embedded source free of Onvif - and of any other
/// service's idea of how to prove who is calling.
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

    private const string DefaultsFile = "RuntimeDefaults.cs";

    private readonly GeneratorOptions _options;

    public RuntimeEmitter(GeneratorOptions options) => _options = options;

    /// <summary>Writes the runtime, and returns how many files that changed.</summary>
    public int Emit(string directory)
    {
        int written = 0;

        foreach (var (path, source) in Sources())
        {
            var file = new GeneratedFile(
                Path.Combine(directory, path),
                GeneratedFile.RuntimeHeader + Environment.NewLine + source.Replace(Token, _options.Runtime.Namespace));

            if (file.WriteIfChanged()) written++;
        }

        var defaults = new GeneratedFile(Path.Combine(directory, DefaultsFile), EmitDefaults());
        if (defaults.WriteIfChanged()) written++;

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

    /// <summary>
    /// Writes what the runtime was generated for: the envelope prologue, what a client
    /// authenticates with, and a constant for each declared namespace that was given a name.
    /// </summary>
    private string EmitDefaults()
    {
        string @namespace = _options.Runtime.Namespace;

        var writer = new CSharpWriter();
        writer.Lines(GeneratedFile.RuntimeHeader);
        writer.Line();
        writer.Line($"using {@namespace}.Soap;");
        writer.Line($"using {@namespace}.Xml;");
        writer.Line();
        writer.Line($"namespace {@namespace}");

        using (writer.Braces())
        {
            writer.Doc(
                "What this runtime was generated for. The rest of the runtime is the same whatever " +
                "the schemas are; this is the part that is not.");
            writer.Line("public static class RuntimeDefaults");

            using (writer.Braces())
            {
                writer.Doc(
                    "Prefixes declared on the envelope element of every message, whether or not the " +
                    "body uses them.");
                writer.Line("public static readonly XmlNamespaceDeclaration[] EnvelopePrologue = new XmlNamespaceDeclaration[]");
                using (writer.Braces(semicolon: true))
                {
                    foreach (var declaration in _options.EnvelopePrologue)
                    {
                        writer.Line(
                            $"new XmlNamespaceDeclaration({Quote(declaration.Prefix)}, {Quote(declaration.Namespace)}),");
                    }
                }

                writer.Line();
                writer.Doc(
                    "How a client proves who it is unless it is told otherwise. The contract is " +
                    "generated; what meets it is named when the runtime is, and is null when " +
                    "nothing was named.");
                writer.Line("public static IClientAuthentication CreateAuthentication()");
                using (writer.Braces())
                {
                    if (_options.AuthenticationType is { } authentication)
                    {
                        writer.Line($"return new {authentication}();");
                    }
                    else
                    {
                        writer.Line("// Nothing was named, so a client sends no credentials until it is given something.");
                        writer.Line("return null;");
                    }
                }
            }
        }

        var named = _options.EnvelopePrologue.Where(d => d.Constant is not null).ToList();
        if (named.Count > 0)
        {
            writer.Line();
            writer.Line($"namespace {@namespace}.Xml");
            using (writer.Braces())
            {
                writer.Doc("The namespaces this runtime was generated for.");
                writer.Line("public static partial class OnvifXmlNamespaces");
                using (writer.Braces())
                {
                    bool first = true;
                    foreach (var declaration in named)
                    {
                        if (!first) writer.Line();
                        first = false;

                        writer.Doc($"Conventionally bound to the \"{declaration.Prefix}\" prefix.");
                        writer.Line($"public const string {declaration.Constant} = {Quote(declaration.Namespace)};");
                    }
                }
            }
        }

        return writer.ToString();
    }

    private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
