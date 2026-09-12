using SharpOnvif.CodeGen.Binding;
using SharpOnvif.CodeGen.Configuration;
using SharpOnvif.CodeGen.Emit;
using SharpOnvif.CodeGen.Wsdl;
using SharpOnvif.CodeGen.Xml;
using SharpOnvif.CodeGen.Xsd;

// Generates the SharpOnvif client and server bindings from the schemas mirrored in wsdl/.
// See doc/codegen.md.
//
//   dotnet run --project src/SharpOnvif.CodeGen
//   dotnet run --project src/SharpOnvif.CodeGen -- --out <directory>
//
// Pass --out to write somewhere other than src/, which is useful for comparing a change to the
// generator against what is committed.

string[] arguments = Environment.GetCommandLineArgs();
string repoRoot = FindRepositoryRoot();
string outputRoot = ArgumentValue(arguments, "--out") ?? Path.Combine(repoRoot, "src");

// One resolver for the whole run: parsing onvif.xsd is expensive and every service imports it.
// Everything downstream is per-service, because each generated assembly carries its own copy of
// the shared schema types.
var resolver = new DocumentResolver(Path.Combine(repoRoot, "wsdl"));

int written = 0;
int operations = 0;
int types = 0;

foreach (var service in ServiceCatalog.All)
{
    var schema = new XsdSchemaSet();
    var wsdl = new WsdlParser(resolver, new XsdParser(resolver, schema));
    wsdl.Parse(service.Wsdl);
    wsdl.ResolveSoapActions();

    var model = new ModelBuilder(service.Name, schema, wsdl, service.GenerateEntireSchema).Build();

    // Both sides of a service are generated from one model into the single client and server
    // projects, each service in its own folder and its own namespace.
    written += Emit(outputRoot, "SharpOnvifClient", service.Name, model, server: false);
    written += Emit(outputRoot, "SharpOnvifServer", service.Name, model, server: true);

    int serviceOperations = model.Services.Sum(s => s.Operations.Count);
    operations += serviceOperations;
    types += model.Classes.Count + model.Enums.Count;

    Console.WriteLine(
        $"{service.Name,-24} types={model.Classes.Count + model.Enums.Count,5}  " +
        $"ports={model.Services.Count,2}  operations={serviceOperations,3}");
}

Console.WriteLine();
Console.WriteLine($"{ServiceCatalog.All.Count} services, {operations} operations, {types} types");
Console.WriteLine(written == 0
    ? $"{outputRoot}: already up to date"
    : $"{outputRoot}: {written} file(s) written");

static int Emit(string outputRoot, string project, string service, CsModel model, bool server)
{
    string @namespace = project + "." + service;
    string directory = Path.Combine(outputRoot, project, "Generated", service);

    var contracts = new GeneratedFile(
        Path.Combine(directory, "DataContracts.cs"),
        new DataContractEmitter(model, @namespace).Emit());

    var api = server
        ? new GeneratedFile(Path.Combine(directory, "Service.cs"), new ServerEmitter(model, @namespace).Emit())
        : new GeneratedFile(Path.Combine(directory, "Client.cs"), new ClientEmitter(model, @namespace).Emit());

    int written = 0;
    if (contracts.WriteIfChanged()) written++;
    if (api.WriteIfChanged()) written++;
    return written;
}

static string? ArgumentValue(string[] arguments, string name)
{
    int index = Array.IndexOf(arguments, name);
    return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
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

    throw new InvalidOperationException(
        "Could not locate the repository root: expected a directory containing both 'wsdl' and 'src'.");
}
