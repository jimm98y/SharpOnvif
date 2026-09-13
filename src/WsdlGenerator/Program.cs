using WsdlGenerator.Configuration;
using WsdlGenerator.Generation;
using WsdlGenerator.Xml;

// Generates C# clients and services from WSDL. Run with --help for the options.
//
// With no arguments it regenerates this repository's own Onvif bindings from the mirror in wsdl/.
// See doc/codegen.md.

string[] arguments = args;

if (arguments.Contains("--help") || arguments.Contains("-h"))
{
    Console.WriteLine(CommandLine.Usage);
    return 0;
}

try
{
    GeneratorOptions options = CommandLine.Parse(arguments) ?? OnvifDefaults();
    Report(new CodeGenerator(options).Run(), options);
    return 0;
}
catch (SchemaException error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

static GeneratorOptions OnvifDefaults()
{
    string root = FindRepositoryRoot();
    return ServiceCatalog.OnvifOptions(root, Path.Combine(root, "src"));
}

static void Report(GenerationResult result, GeneratorOptions options)
{
    Console.WriteLine($"{"(shared schema)",-24} types={result.SharedTypes,5}");

    foreach (var (service, types, portTypes, operations) in result.Services)
        Console.WriteLine($"{service,-24} types={types,5}  ports={portTypes,2}  operations={operations,3}");

    Console.WriteLine();
    Console.WriteLine(
        $"{result.Services.Count} service(s), {result.Operations} operations, " +
        $"{result.SharedTypes} shared types");

    string where = string.Join(", ", options.Targets.Select(t => t.Directory).Distinct());
    Console.WriteLine(result.FilesWritten == 0
        ? $"{where}: already up to date"
        : $"{where}: {result.FilesWritten} file(s) written");
}

/// <summary>
/// Walks up from the executable to the repository, which is where the schema mirror and the
/// projects live. Running through `dotnet run` puts the binary several directories deep.
/// </summary>
static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (Directory.Exists(Path.Combine(directory.FullName, "wsdl"))
            && Directory.Exists(Path.Combine(directory.FullName, "src")))
        {
            return directory.FullName;
        }
        directory = directory.Parent;
    }

    throw new SchemaException(
        "Could not locate the repository root: expected a directory containing both 'wsdl' and 'src'. " +
        "Pass --wsdl, --namespace and --out to generate from somewhere else.");
}
