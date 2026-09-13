using WsdlGenerator.Binding;

namespace WsdlGenerator.Emit;

/// <summary>
/// Emits the server side of a service: an abstract base class per portType whose methods an
/// implementation overrides, and the dispatcher that routes a SOAP action to one of them.
///
/// Each operation appears three times on the base, each layer defaulting to the next:
/// an async method taking the request wrapper, a synchronous one taking the same wrapper, and a
/// synchronous one taking the request members as ordinary arguments. An implementation overrides
/// whichever suits it, which is what lets code written against the previous bindings keep working
/// whichever style svcutil happened to generate for that operation.
/// </summary>
internal sealed class ServerEmitter
{
    private const string Task = "System.Threading.Tasks.Task";
    private const string Token = "System.Threading.CancellationToken";

    private readonly CsModel _model;
    private readonly string _namespace;

    /// <summary>Namespace of the contract, reader and writer.</summary>
    private readonly string Xml;

    /// <summary>
    /// Namespace of the dispatch a generated service is routed by. Unlike everything else a
    /// service names, this one is not generated - routing a SOAP action to a method over ASP.NET
    /// Core is a library, and naming it is all the generator can do about that.
    /// </summary>
    private readonly string Dispatch;

    public ServerEmitter(CsModel model, string @namespace, string runtime, string dispatch)
    {
        _model = model;
        _namespace = @namespace;
        Xml = runtime + ".Xml";
        Dispatch = dispatch;
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

                EmitBase(writer, service.Name + "Base", service.Operations, service.Documentation);
                writer.Line();
                EmitDispatcher(writer, service.Name + "Base", service.Name + "Dispatcher", service.Operations);
            }

            // A service that publishes several portTypes also gets a base covering all of them,
            // because C# has single inheritance and an implementation may want to answer for the
            // whole service from one class.
            if (_model.Services.Count > 1)
            {
                var combined = CombinedOperations();
                writer.Line();
                EmitBase(writer, _model.ServiceName + "Base", combined,
                    $"Implements every portType the {_model.ServiceName} service publishes.");
                writer.Line();
                EmitDispatcher(writer, _model.ServiceName + "Base", _model.ServiceName + "Dispatcher", combined);
            }
        }

        return writer.ToString();
    }

    /// <summary>
    /// The union of every portType's operations, keyed by the signature the base will expose, so
    /// an operation two portTypes share is emitted once.
    /// </summary>
    private List<CsOperation> CombinedOperations()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var combined = new List<CsOperation>();

        foreach (var operation in _model.Services.SelectMany(s => s.Operations))
        {
            if (seen.Add(operation.Name + "(" + operation.Request.Name + ")")) combined.Add(operation);
        }

        return combined;
    }

    private void EmitBase(CSharpWriter writer, string className, IReadOnlyList<CsOperation> operations, string? documentation)
    {
        writer.Doc(documentation ?? $"Base class for a {className.Replace("Base", "")} implementation.");
        writer.Line($"public abstract class {className} : {Dispatch}.IDispatchedService");

        using (writer.Braces())
        {
            writer.Line("/// <summary>Routes SOAP actions to this service's operations.</summary>");
            writer.Line($"public static {Dispatch}.ServiceDispatcher Dispatcher {{ get; }} = " +
                        $"new {className.Substring(0, className.Length - "Base".Length)}Dispatcher();");

            foreach (var operation in operations)
            {
                writer.Line();
                EmitOperation(writer, operation);
            }
        }
    }

    private void EmitOperation(CSharpWriter writer, CsOperation operation)
    {
        string name = CsharpNaming.Escape(operation.Name);
        string request = operation.Request.Name;
        string response = operation.Response.Name;
        var inputs = operation.Input.Where(m => m.Kind != MemberKind.ChoiceIdentifier).ToList();

        // Layer 1: what the dispatcher calls. Override it for an operation that needs to await.
        writer.Doc(operation.Documentation);
        writer.Line($"public virtual {Task}<{response}> {name}Async({request} request, {Token} cancellationToken)");
        using (writer.Braces())
        {
            writer.Line($"return {Task}.FromResult({name}(request));");
        }
        writer.Line();

        // Layer 2: the synchronous wrapper form.
        writer.Line($"/// <summary>Handles the operation from its request wrapper.</summary>");
        writer.Line($"public virtual {response} {name}({request} request)");
        using (writer.Braces())
        {
            EmitUnwrappedCall(writer, operation, inputs);
        }
        writer.Line();

        // Layer 3: the unwrapped form, which is where an implementation usually starts.
        writer.Line("/// <summary>Handles the operation from its request members.</summary>");
        writer.Line($"public virtual {UnwrappedReturn(operation)} {name}({UnwrappedParameters(inputs)})");
        using (writer.Braces())
        {
            writer.Line("throw new System.NotImplementedException();");
        }
    }

    /// <summary>Emits the wrapper-form body, which forwards to the unwrapped overload.</summary>
    private void EmitUnwrappedCall(CSharpWriter writer, CsOperation operation, List<CsMember> inputs)
    {
        string name = CsharpNaming.Escape(operation.Name);
        string arguments = string.Join(", ", inputs.Select(m => $"request.{CsharpNaming.Escape(m.Name)}"));
        string call = $"{name}({arguments})";

        writer.Line($"return {call};");
    }

    /// <summary>
    /// The unwrapped overload returns the response wrapper like the others: it differs only in
    /// taking the request's members as arguments rather than the request itself.
    /// </summary>
    private static string UnwrappedReturn(CsOperation operation) => operation.Response.Name;

    private static string UnwrappedParameters(List<CsMember> inputs) =>
        string.Join(", ", inputs.Select(m => $"{TypeOf(m)} {CsharpNaming.Escape(m.Name)}"));

    private static string TypeOf(CsMember member) =>
        member.IsArray ? member.Type.CsName + "[]" : member.Type.CsName;

    // ---------------------------------------------------------------- dispatcher

    private void EmitDispatcher(
        CSharpWriter writer, string baseClass, string dispatcherName, IReadOnlyList<CsOperation> operations)
    {
        writer.Line($"/// <summary>Routes SOAP actions to <see cref=\"{baseClass}\"/>.</summary>");
        writer.Line($"internal sealed class {dispatcherName} : {Dispatch}.ServiceDispatcher");

        using (writer.Braces())
        {
            writer.Line($"public override System.Type ServiceType {{ get {{ return typeof({baseClass}); }} }}");
            writer.Line();

            writer.Line("public override bool CanHandle(string action)");
            using (writer.Braces())
            {
                writer.Line("switch (action)");
                using (writer.Braces())
                {
                    foreach (var operation in operations)
                        writer.Line($"case SoapActions.{CsharpNaming.Escape(operation.Name)}:");
                    writer.Line("    return true;");
                    writer.Line("default:");
                    writer.Line("    return false;");
                }
            }
            writer.Line();

            EmitTryResolveAction(writer, operations);
            writer.Line();

            EmitInvoke(writer, baseClass, operations);
            writer.Line();

            writer.Line($"public override {Xml}.XmlContract ResolveXmlType(string ns, string name)");
            using (writer.Braces())
            {
                writer.Line("return XmlTypeFactory.Create(ns, name);");
            }
        }
    }

    private void EmitTryResolveAction(CSharpWriter writer, IReadOnlyList<CsOperation> operations)
    {
        writer.Line("public override bool TryResolveAction(string ns, string elementName, out string action)");
        using (writer.Braces())
        {
            writer.Line("switch (elementName)");
            using (writer.Braces())
            {
                foreach (var group in operations
                    .Where(o => o.Request.WrapperElement is not null)
                    .GroupBy(o => o.Request.WrapperElement!.Value.LocalName, StringComparer.Ordinal))
                {
                    writer.Line($"case \"{group.Key}\":");
                    writer.Indent();
                    foreach (var operation in group)
                    {
                        var element = operation.Request.WrapperElement!.Value;
                        writer.Line($"if (ns == \"{element.Namespace}\")");
                        using (writer.Braces())
                        {
                            writer.Line($"action = SoapActions.{CsharpNaming.Escape(operation.Name)};");
                            writer.Line("return true;");
                        }
                    }
                    writer.Line("break;");
                    writer.Outdent();
                }
            }
            writer.Line("action = null;");
            writer.Line("return false;");
        }
    }

    private void EmitInvoke(CSharpWriter writer, string baseClass, IReadOnlyList<CsOperation> operations)
    {
        writer.Line($"public override async {Task}<{Dispatch}.DispatchResult> InvokeAsync(");
        writer.Line($"    object service, string action, System.Xml.XmlReader body, {Token} cancellationToken)");

        using (writer.Braces())
        {
            writer.Line($"{baseClass} target = ({baseClass})service;");
            writer.Line("var reader = CreateReader(body);");
            writer.Line();
            writer.Line("switch (action)");
            using (writer.Braces())
            {
                foreach (var operation in operations)
                {
                    writer.Line($"case SoapActions.{CsharpNaming.Escape(operation.Name)}:");
                    using (writer.Braces())
                    {
                        writer.Line($"var request = new {operation.Request.Name}();");
                        writer.Line("reader.ReadInto(request);");
                        writer.Line($"var response = await target.{CsharpNaming.Escape(operation.Name)}Async(request, cancellationToken).ConfigureAwait(false);");

                        var element = operation.Response.WrapperElement;
                        string ns = element is { } e ? $"\"{e.Namespace}\"" : "null";
                        string localName = element is { } el ? $"\"{el.LocalName}\"" : $"\"{operation.Name}Response\"";
                        writer.Line($"return new {Dispatch}.DispatchResult(response, {ns}, {localName});");
                    }
                }
            }
            writer.Line();
            writer.Line("throw new System.NotImplementedException(action);");
        }
    }
}
