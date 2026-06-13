namespace WsdlGenerator.Parser;

public sealed record WsdlDefinition(
    string TargetNamespace,
    string ServiceName,
    IReadOnlyDictionary<string, string> Namespaces,
    IReadOnlyList<WsdlOperation> Operations,
    IReadOnlyDictionary<string, XsdElement> Elements);

public sealed record WsdlOperation(
    string Name,
    string SoapAction,
    string InputElementName,
    string OutputElementName);

public sealed record XsdElement(
    string Name,
    IReadOnlyList<XsdField> Fields);

public sealed record XsdField(
    string Name,
    string XsdType,
    bool IsOptional,
    bool IsMultiple);
