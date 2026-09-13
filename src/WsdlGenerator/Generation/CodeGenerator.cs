using WsdlGenerator.Binding;
using WsdlGenerator.Configuration;
using WsdlGenerator.Emit;
using WsdlGenerator.Wsdl;
using WsdlGenerator.Xml;
using WsdlGenerator.Xsd;

namespace WsdlGenerator.Generation;

/// <summary>What a run produced, for reporting.</summary>
internal sealed record GenerationResult(
    int FilesWritten,
    int Operations,
    int SharedTypes,
    int RuntimeFiles,
    IReadOnlyList<(string Service, int Types, int PortTypes, int Operations)> Services);

/// <summary>
/// Compiles a set of WSDLs into C#.
///
/// Every WSDL is parsed into one schema set, because services routinely share schemas and a
/// single definition of each type is what allows the shared ones to be generated once. A type
/// belongs to a service when that service's own WSDL declares it; everything else is shared.
/// </summary>
internal sealed class CodeGenerator
{
    private readonly GeneratorOptions _options;

    public CodeGenerator(GeneratorOptions options) => _options = options;

    public GenerationResult Run()
    {
        var resolver = _options.MirrorRoot is { } mirror
            ? DocumentResolver.FromMirror(mirror)
            : DocumentResolver.FromFileSystem();

        var schema = new XsdSchemaSet();
        var schemaParser = new XsdParser(resolver, schema);

        var parsed = new List<(ServiceDefinition Definition, WsdlParser Wsdl)>();
        foreach (var definition in _options.Services)
        {
            var wsdl = new WsdlParser(resolver, schemaParser);
            wsdl.Parse(definition.Wsdl);
            wsdl.ResolveSoapActions();
            parsed.Add((definition, wsdl));
        }

        // Before anything is modelled, so that an added value reaches the enum and the conversions
        // generated beside it alike.
        SchemaExtensions.Apply(schema, _options.EnumerationExtensions);

        var serviceNamespaces = parsed
            .SelectMany(s => s.Wsdl.TargetNamespaces)
            .ToHashSet(StringComparer.Ordinal);

        var sharedModel = ModelBuilder.BuildShared(
            schema, parsed.Select(s => s.Wsdl).ToList(), serviceNamespaces, _options.SharedNamespace);
        var sharedTypes = SharedTypeIndex.From(sharedModel);

        // The runtime first, because everything else is compiled against it.
        int runtimeFiles = _options.Runtime.Directory is { } runtime
            ? new RuntimeEmitter(_options.Runtime.Namespace).Emit(runtime)
            : 0;

        int written = runtimeFiles + EmitShared(sharedModel);
        int operations = 0;
        var services = new List<(string, int, int, int)>();

        foreach (var (definition, wsdl) in parsed)
        {
            CsModel? summary = null;

            // A target names its own namespace, so the model is built per target even though the
            // shape it describes is the same.
            foreach (var target in _options.Targets)
            {
                if (target.Client)
                    written += Emit(target, definition, wsdl, schema, sharedTypes, server: false, ref summary);

                if (target.Server)
                    written += Emit(target, definition, wsdl, schema, sharedTypes, server: true, ref summary);
            }

            if (summary is null)
                throw new SchemaException("Nothing was generated: no target asked for a client or a service.");

            int serviceOperations = summary.Services.Sum(s => s.Operations.Count);
            operations += serviceOperations;
            services.Add((definition.Name, summary.Classes.Count + summary.Enums.Count,
                summary.Services.Count, serviceOperations));
        }

        return new GenerationResult(
            written, operations, sharedModel.Classes.Count + sharedModel.Enums.Count, runtimeFiles, services);
    }

    private int EmitShared(CsModel model)
    {
        var file = new GeneratedFile(
            Path.Combine(_options.SharedDirectory, "DataContracts.cs"),
            new DataContractEmitter(
                model, _options.SharedNamespace, isShared: true, _options.Runtime.Namespace).Emit());

        return file.WriteIfChanged() ? 1 : 0;
    }

    private int Emit(
        GenerationTarget target,
        ServiceDefinition definition,
        WsdlParser wsdl,
        XsdSchemaSet schema,
        SharedTypeIndex sharedTypes,
        bool server,
        ref CsModel? summary)
    {
        string @namespace = target.Namespace + "." + definition.Name;
        string directory = Path.Combine(target.Directory, definition.Name);

        var model = new ModelBuilder(definition.Name, schema, wsdl, @namespace, sharedTypes).Build();
        summary ??= model;

        var contracts = new GeneratedFile(
            Path.Combine(directory, "DataContracts.cs"),
            new DataContractEmitter(
                model, @namespace, isShared: false, _options.Runtime.Namespace, _options.SharedNamespace).Emit());

        string runtime = _options.Runtime.Namespace;

        var api = server
            ? new GeneratedFile(
                Path.Combine(directory, "Service.cs"), new ServerEmitter(model, @namespace, runtime).Emit())
            : new GeneratedFile(
                Path.Combine(directory, "Client.cs"), new ClientEmitter(model, @namespace, runtime, _options.SettingsType).Emit());

        int written = 0;
        if (contracts.WriteIfChanged()) written++;
        if (api.WriteIfChanged()) written++;
        return written;
    }
}
