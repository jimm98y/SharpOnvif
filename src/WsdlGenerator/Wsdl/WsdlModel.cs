using WsdlGenerator.Xml;

namespace WsdlGenerator.Wsdl;

/// <summary>A wsdl:message part. Document/literal services use element parts.</summary>
internal sealed record WsdlPart(string Name, QName? Element, QName? Type);

internal sealed class WsdlMessage
{
    public required QName Name { get; init; }
    public IReadOnlyList<WsdlPart> Parts { get; init; } = [];
}

internal sealed class WsdlOperation
{
    public required string Name { get; init; }
    public QName? Input { get; init; }
    public QName? Output { get; init; }
    public IReadOnlyList<WsdlFault> Faults { get; init; } = [];
    public string? Documentation { get; init; }

    /// <summary>
    /// SOAP action from the binding. Filled in once the portType is matched with its binding;
    /// A service usually derives it from the operation name, but it is always read from the document.
    /// </summary>
    public string? SoapAction { get; set; }

    public override string ToString() => Name;
}

internal sealed record WsdlFault(string Name, QName Message);

internal sealed class WsdlPortType
{
    public required QName Name { get; init; }
    public IReadOnlyList<WsdlOperation> Operations { get; init; } = [];
    public string? Documentation { get; init; }

    public override string ToString() => Name.ToString();
}

internal sealed class WsdlBinding
{
    public required QName Name { get; init; }
    public required QName PortType { get; init; }

    /// <summary>URL of the document that declared this binding.</summary>
    public required string Document { get; init; }

    /// <summary>SOAP action keyed by operation name.</summary>
    public IReadOnlyDictionary<string, string> Actions { get; init; } = new Dictionary<string, string>();
}
