using WsdlGenerator.Binding;

namespace WsdlGenerator.Emit;

/// <summary>
/// Emits the client side of a service: one interface and one HttpClient-backed proxy per portType,
/// the SOAP action constants, and the xsi:type factory for the assembly.
///
/// Every operation gets two call styles, which never collide because they differ in arity or in
/// parameter type. See doc/codegen.md for why both are emitted rather than one.
/// </summary>
internal sealed class ClientEmitter
{
    private const string Task = "System.Threading.Tasks.Task";
    private const string Token = "System.Threading.CancellationToken";

    private readonly CsModel _model;
    private readonly string _namespace;

    /// <summary>Namespace of the client base class and the settings it takes.</summary>
    private readonly string Runtime;

    /// <summary>Namespace of the contract, reader and writer.</summary>
    private readonly string Xml;

    /// <summary>
    /// Type a client can build its own settings from, or null when the run named none - in which
    /// case a client is only ever handed them, having nothing it could construct.
    /// </summary>
    private readonly string? _settingsType;

    public ClientEmitter(CsModel model, string @namespace, string runtime, string? settingsType)
    {
        _model = model;
        _namespace = @namespace;
        _settingsType = settingsType;
        Runtime = runtime + ".Soap";
        Xml = runtime + ".Xml";
    }

    public string Emit()
    {
        var writer = new CSharpWriter();
        writer.Lines(GeneratedFile.Header);
        writer.Line();
        writer.Line($"namespace {_namespace}");

        using (writer.Braces())
        {
            bool first = true;
            foreach (var service in _model.Services)
            {
                if (!first) writer.Line();
                first = false;
                EmitInterface(writer, service);
                writer.Line();
                EmitClient(writer, service);
            }
        }

        return writer.ToString();
    }

    private void EmitInterface(CSharpWriter writer, CsService service)
    {
        writer.Doc(service.Documentation ?? $"The {service.Name} Onvif service.");
        writer.Line($"public interface {CsharpNaming.Escape(service.Name)}");
        using (writer.Braces())
        {
            bool first = true;
            foreach (var operation in service.Operations)
            {
                if (!first) writer.Line();
                first = false;

                writer.Doc(operation.Documentation);
                writer.Line(Signature(operation, wrapped: true) + ";");

                writer.Line();
                writer.Doc(UnwrappedDoc(operation));
                writer.Line(Signature(operation, wrapped: false) + ";");
            }
        }
    }

    private void EmitClient(CSharpWriter writer, CsService service)
    {
        string name = service.Name + "Client";

        writer.Doc($"Talks to the {service.Name} service on a device over SOAP 1.2.");
        writer.Line($"public partial class {name} : {Runtime}.SoapClientBase, {CsharpNaming.Escape(service.Name)}");

        using (writer.Braces())
        {
            if (_settingsType is { } settings)
            {
                writer.Doc("Creates a client that sends no credentials.");
                writer.Line($"public {name}(string endpointUri)");
                writer.Line($"    : base(endpointUri, new {settings}())");
                using (writer.Braces()) { }
                writer.Line();

                writer.Doc("Creates a client that authenticates however its settings say to.");
                writer.Line($"public {name}(string endpointUri, string userName, string password)");
                writer.Line($"    : base(endpointUri, new {settings}(userName, password))");
                using (writer.Braces()) { }
                writer.Line();
            }

            writer.Doc("Creates a client with full control over transport and authentication.");
            writer.Line($"public {name}(string endpointUri, {Runtime}.IClientSettings settings)");
            writer.Line("    : base(endpointUri, settings)");
            using (writer.Braces()) { }
            writer.Line();

            writer.Line($"protected override {Xml}.OnvifContract ResolveXmlType(string ns, string name)");
            using (writer.Braces())
            {
                writer.Line("return XmlTypeFactory.Create(ns, name);");
            }

            foreach (var operation in service.Operations)
            {
                writer.Line();
                EmitOperation(writer, operation);
            }
        }
    }

    private void EmitOperation(CSharpWriter writer, CsOperation operation)
    {
        var wrapper = operation.Request;
        string bodyNamespace = wrapper.WrapperElement is { } element
            ? $"\"{element.Namespace}\""
            : "null";
        string bodyName = wrapper.WrapperElement is { } bodyElement
            ? $"\"{bodyElement.LocalName}\""
            : $"\"{operation.Name}\"";

        writer.Doc(operation.Documentation);
        writer.Line("public " + Signature(operation, wrapped: true));
        using (writer.Braces())
        {
            string action = $"OnvifActions.{CsharpNaming.Escape(operation.Name)}";

            if (operation.IsOneWay)
            {
                writer.Line($"return InvokeAsync({action}, {bodyNamespace}, {bodyName}, request, cancellationToken);");
            }
            else
            {
                writer.Line($"return InvokeAsync({action}, {bodyNamespace}, {bodyName}, request,");
                writer.Line($"    () => new {operation.Response.Name}(), cancellationToken);");
            }
        }

        writer.Line();
        writer.Doc(UnwrappedDoc(operation));

        var inputs = Inputs(operation);
        string arguments = string.Join(", ", inputs.Select(m => CsharpNaming.Escape(m.Name)));
        string construct = $"new {operation.Request.Name}({arguments})";
        string call = $"{CsharpNaming.Escape(operation.Name)}Async({construct}, cancellationToken)";

        writer.Line("public " + Signature(operation, wrapped: false));
        using (writer.Braces())
        {
            writer.Line($"return {call};");
        }
    }

    private static List<CsMember> Inputs(CsOperation operation) =>
        operation.Input.Where(m => m.Kind != MemberKind.ChoiceIdentifier).ToList();

    private static string UnwrappedDoc(CsOperation operation) =>
        operation.Input.Count == 0
            ? "Calls the operation with an empty request."
            : "Calls the operation with its request members as arguments, instead of building the request.";

    private string Signature(CsOperation operation, bool wrapped)
    {
        string name = CsharpNaming.Escape(operation.Name) + "Async";

        if (wrapped)
        {
            string returns = operation.IsOneWay ? Task : $"{Task}<{operation.Response.Name}>";
            return $"{returns} {name}({operation.Request.Name} request, {Token} cancellationToken = default({Token}))";
        }

        string parameters = string.Join("", Inputs(operation)
            .Select(m => $"{TypeOf(m)} {CsharpNaming.Escape(m.Name)}, "));

        return $"{UnwrappedReturn(operation)} {name}({parameters}{Token} cancellationToken = default({Token}))";
    }

    /// <summary>
    /// Both overloads return the response wrapper: the unwrapped one only saves the caller from
    /// building the request, it does not change what comes back.
    /// </summary>
    private string UnwrappedReturn(CsOperation operation) =>
        operation.IsOneWay ? Task : $"{Task}<{operation.Response.Name}>";

    private static string TypeOf(CsMember member) =>
        member.IsArray ? member.Type.CsName + "[]" : member.Type.CsName;
}
