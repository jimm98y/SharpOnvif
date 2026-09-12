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

// One schema set for the whole run. The services agree on the schemas they share, so parsing
// them once yields a single definition of every type - and lets the shared ones be generated
// once instead of twenty-five times over.
var resolver = new DocumentResolver(Path.Combine(repoRoot, "wsdl"));
var schema = new XsdSchemaSet();
var schemaParser = new XsdParser(resolver, schema);

var services = new List<(ServiceDefinition Definition, WsdlParser Wsdl)>();
foreach (var definition in ServiceCatalog.All)
{
    var wsdl = new WsdlParser(resolver, schemaParser);
    wsdl.Parse(definition.Wsdl);
    wsdl.ResolveSoapActions();
    services.Add((definition, wsdl));
}

// A type belongs to a service when its own WSDL declares it; everything else comes from a schema
// the services share and is generated once into the common assembly.
var serviceNamespaces = services.SelectMany(s => s.Wsdl.TargetNamespaces).ToHashSet(StringComparer.Ordinal);

const string SharedNamespace = "SharpOnvifCommon.Onvif";
var sharedModel = ModelBuilder.BuildShared(
    schema, services.Select(s => s.Wsdl).ToList(), serviceNamespaces, SharedNamespace);
var sharedTypes = SharedTypeIndex.From(sharedModel);

int written = 0;
int operations = 0;
int sharedTypeCount = sharedModel.Classes.Count + sharedModel.Enums.Count;
int serviceTypeCount = 0;

written += EmitShared(outputRoot, SharedNamespace, sharedModel);
Console.WriteLine($"{"(shared schema)",-24} types={sharedTypeCount,5}");

foreach (var (definition, wsdl) in services)
{
    written += EmitService(outputRoot, "SharpOnvifClient", definition, wsdl, schema, sharedTypes, false, out var model);
    written += EmitService(outputRoot, "SharpOnvifServer", definition, wsdl, schema, sharedTypes, true, out _);

    int serviceOperations = model.Services.Sum(s => s.Operations.Count);
    operations += serviceOperations;
    serviceTypeCount += model.Classes.Count + model.Enums.Count;

    Console.WriteLine(
        $"{definition.Name,-24} types={model.Classes.Count + model.Enums.Count,5}  " +
        $"ports={model.Services.Count,2}  operations={serviceOperations,3}");
}

Console.WriteLine();
Console.WriteLine(
    $"{ServiceCatalog.All.Count} services, {operations} operations, " +
    $"{sharedTypeCount} shared types, {serviceTypeCount} service types");
Console.WriteLine(written == 0
    ? $"{outputRoot}: already up to date"
    : $"{outputRoot}: {written} file(s) written");

/// <summary>Emits the schema every service shares, once, into the common assembly.</summary>
static int EmitShared(string outputRoot, string @namespace, CsModel model)
{
    var file = new GeneratedFile(
        Path.Combine(outputRoot, "SharpOnvifCommon", "Generated", "DataContracts.cs"),
        new DataContractEmitter(model, @namespace).Emit());

    return file.WriteIfChanged() ? 1 : 0;
}

static int EmitService(
    string outputRoot,
    string project,
    ServiceDefinition definition,
    WsdlParser wsdl,
    XsdSchemaSet schema,
    SharedTypeIndex sharedTypes,
    bool server,
    out CsModel model)
{
    string @namespace = project + "." + definition.Name;
    string directory = Path.Combine(outputRoot, project, "Generated", definition.Name);

    model = new ModelBuilder(
        definition.Name, schema, wsdl, generateEntireSchema: false, @namespace, sharedTypes).Build();

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
