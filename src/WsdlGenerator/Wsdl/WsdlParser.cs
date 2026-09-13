using System.Xml.Linq;
using WsdlGenerator.Xml;
using WsdlGenerator.Xsd;

namespace WsdlGenerator.Wsdl;

/// <summary>
/// Parses WSDL 1.1 documents, following wsdl:import and feeding every inline xs:schema to the
/// schema parser. Only document/literal SOAP 1.2 bindings are accepted.
/// </summary>
internal sealed class WsdlParser
{
    private static readonly XNamespace W = Ns.Wsdl;
    private static readonly XNamespace Xs = Ns.Xsd;

    private readonly DocumentResolver _resolver;
    private readonly XsdParser _schemaParser;
    private readonly HashSet<string> _parsed = new(StringComparer.Ordinal);

    private readonly Dictionary<QName, WsdlMessage> _messages = [];
    private readonly Dictionary<QName, WsdlPortType> _portTypes = [];
    private readonly Dictionary<QName, WsdlBinding> _bindings = [];
    private readonly HashSet<string> _targetNamespaces = new(StringComparer.Ordinal);
    private string _rootDocument = "";

    public WsdlParser(DocumentResolver resolver, XsdParser schemaParser)
    {
        _resolver = resolver;
        _schemaParser = schemaParser;
    }

    public IReadOnlyDictionary<QName, WsdlMessage> Messages => _messages;
    public IReadOnlyDictionary<QName, WsdlPortType> PortTypes => _portTypes;
    public IReadOnlyDictionary<QName, WsdlBinding> Bindings => _bindings;

    /// <summary>
    /// Target namespaces of every WSDL parsed here. Types declared in these namespaces belong to
    /// the service that declares them; everything else comes from a schema services share.
    /// </summary>
    public IReadOnlyCollection<string> TargetNamespaces => _targetNamespaces;

    /// <summary>
    /// The portTypes this service exposes: those bound by a binding declared in the service's own
    /// WSDL. A WSDL may import another purely to reuse its schema types, and deviceio.wsdl does
    /// exactly that with devicemgmt.wsdl, so merely being reachable is not enough. Conversely
    /// event.wsdl deliberately declares bindings for the six WS-Notification portTypes it imports,
    /// and those do belong to the service.
    /// </summary>
    public IEnumerable<WsdlPortType> BoundPortTypes =>
        _bindings.Values
            .Where(b => string.Equals(b.Document, _rootDocument, StringComparison.Ordinal))
            .Select(b => b.PortType)
            .Distinct()
            .Where(_portTypes.ContainsKey)
            .Select(name => _portTypes[name]);

    /// <summary>Parses the document at <paramref name="url"/> and everything it imports.</summary>
    public void Parse(string url)
    {
        url = _resolver.Combine(url, null);
        if (_rootDocument.Length == 0) _rootDocument = url;
        if (!_parsed.Add(url)) return;

        var document = _resolver.Load(url);
        var definitions = document.Root
            ?? throw new SchemaException($"'{url}' is empty.");

        if (definitions.Name != W + "definitions")
            throw new SchemaException(definitions, $"Expected wsdl:definitions, found '{definitions.Name}'.");

        string targetNamespace = definitions.Attribute("targetNamespace")?.Value ?? "";
        _targetNamespaces.Add(targetNamespace);

        foreach (var import in definitions.Elements(W + "import"))
        {
            if (import.Attribute("location")?.Value is { } location)
                Parse(_resolver.Combine(location, url));
        }

        foreach (var schema in definitions.Elements(W + "types").Elements(Xs + "schema"))
            _schemaParser.ParseSchema(schema, url);

        foreach (var message in definitions.Elements(W + "message"))
            ParseMessage(message, targetNamespace);

        foreach (var portType in definitions.Elements(W + "portType"))
            ParsePortType(portType, targetNamespace);

        foreach (var binding in definitions.Elements(W + "binding"))
            ParseBinding(binding, targetNamespace, url);
    }

    private void ParseMessage(XElement message, string targetNamespace)
    {
        var name = new QName(targetNamespace, Required(message, "name"));
        var parts = message.Elements(W + "part").Select(part => new WsdlPart(
            Required(part, "name"),
            part.Attribute("element") is { } e ? QName.Parse(e.Value, part) : null,
            part.Attribute("type") is { } t ? QName.Parse(t.Value, part) : null)).ToList();

        _messages[name] = new WsdlMessage { Name = name, Parts = parts };
    }

    private void ParsePortType(XElement portType, string targetNamespace)
    {
        var name = new QName(targetNamespace, Required(portType, "name"));
        var operations = new List<WsdlOperation>();

        foreach (var operation in portType.Elements(W + "operation"))
        {
            var input = operation.Element(W + "input");
            var output = operation.Element(W + "output");

            // Solicit-response and notification (output before input) would need a different
            // client shape; request-response and one-way are what this generates.
            if (input is null && output is not null)
                throw new SchemaException(operation, "Notification-style operations are not supported.");

            operations.Add(new WsdlOperation
            {
                Name = Required(operation, "name"),
                Input = input?.Attribute("message") is { } i ? QName.Parse(i.Value, input) : null,
                Output = output?.Attribute("message") is { } o ? QName.Parse(o.Value, output) : null,
                Faults = operation.Elements(W + "fault")
                    .Where(f => f.Attribute("message") is not null)
                    .Select(f => new WsdlFault(f.Attribute("name")?.Value ?? "fault", QName.Parse(f.Attribute("message")!.Value, f)))
                    .ToList(),
                Documentation = operation.Element(W + "documentation")?.Value.Trim() is { Length: > 0 } d ? Collapse(d) : null,
            });
        }

        _portTypes[name] = new WsdlPortType
        {
            Name = name,
            Operations = operations,
            Documentation = portType.Element(W + "documentation")?.Value.Trim() is { Length: > 0 } pd ? Collapse(pd) : null,
        };
    }

    private void ParseBinding(XElement binding, string targetNamespace, string documentUrl)
    {
        var name = new QName(targetNamespace, Required(binding, "name"));
        var portType = QName.Parse(Required(binding, "type"), binding);

        // Accept SOAP 1.2 and 1.1 binding namespaces; 1.2 is what is generated, but the
        // element name is the same in both so the check stays on the namespace.
        var soapBinding = binding.Elements().FirstOrDefault(e =>
            e.Name.LocalName == "binding" && (e.Name.NamespaceName == Ns.WsdlSoap12 || e.Name.NamespaceName == Ns.WsdlSoap11));

        if (soapBinding is null) return; // Not a SOAP binding (for example HTTP GET); ignore it.

        string style = soapBinding.Attribute("style")?.Value ?? "document";
        if (style != "document")
            throw new SchemaException(soapBinding, $"Only document-style SOAP bindings are supported, found '{style}'.");

        var actions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var operation in binding.Elements(W + "operation"))
        {
            string operationName = Required(operation, "name");
            var soapOperation = operation.Elements().FirstOrDefault(e =>
                e.Name.LocalName == "operation" &&
                (e.Name.NamespaceName == Ns.WsdlSoap12 || e.Name.NamespaceName == Ns.WsdlSoap11));

            foreach (var body in operation.Descendants().Where(e => e.Name.LocalName == "body"))
            {
                string use = body.Attribute("use")?.Value ?? "literal";
                if (use != "literal")
                    throw new SchemaException(body, $"Only literal SOAP bodies are supported, found '{use}'.");
            }

            if (soapOperation?.Attribute("soapAction")?.Value is { Length: > 0 } action)
                actions[operationName] = action;
        }

        _bindings[name] = new WsdlBinding
        {
            Name = name,
            PortType = portType,
            Document = documentUrl,
            Actions = actions,
        };
    }

    /// <summary>
    /// Copies SOAP actions from bindings onto the operations of the portTypes they bind.
    /// Call once after every document has been parsed.
    /// </summary>
    public void ResolveSoapActions()
    {
        foreach (var binding in _bindings.Values)
        {
            if (!_portTypes.TryGetValue(binding.PortType, out var portType)) continue;
            foreach (var operation in portType.Operations)
            {
                if (binding.Actions.TryGetValue(operation.Name, out var action))
                    operation.SoapAction = action;
            }
        }
    }

    private static string Required(XElement element, string attribute) =>
        element.Attribute(attribute)?.Value
        ?? throw new SchemaException(element, $"'{element.Name.LocalName}' requires a '{attribute}' attribute.");

    private static string Collapse(string text) =>
        string.Join(" ", text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
}
