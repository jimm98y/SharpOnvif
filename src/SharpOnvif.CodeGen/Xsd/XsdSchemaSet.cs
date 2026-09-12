using SharpOnvif.CodeGen.Xml;

namespace SharpOnvif.CodeGen.Xsd;

/// <summary>
/// The union of every schema reachable from the WSDLs being generated, indexed for lookup by
/// qualified name. ONVIF services import the same shared schemas (onvif.xsd above all), so
/// duplicate definitions of the same name are expected and are required to be identical.
/// </summary>
internal sealed class XsdSchemaSet
{
    private readonly Dictionary<QName, XsdType> _types = [];
    private readonly Dictionary<QName, XsdElement> _elements = [];
    private readonly Dictionary<QName, XsdAttribute> _attributes = [];

    public IReadOnlyDictionary<QName, XsdType> Types => _types;
    public IReadOnlyDictionary<QName, XsdElement> Elements => _elements;
    public IReadOnlyDictionary<QName, XsdAttribute> Attributes => _attributes;

    public void AddType(XsdType type)
    {
        var name = type.Name ?? throw new SchemaException("Only named types belong in the schema set.");
        // Re-parsing a shared schema is prevented upstream, so a collision here means two
        // different schemas genuinely declare the same name.
        if (_types.TryGetValue(name, out var existing) && !ReferenceEquals(existing, type))
            throw new SchemaException($"Conflicting definitions of type {name}.");
        _types[name] = type;
    }

    public void AddElement(XsdElement element)
    {
        if (_elements.TryGetValue(element.Name, out var existing) && !ReferenceEquals(existing, element))
            throw new SchemaException($"Conflicting definitions of global element {element.Name}.");
        _elements[element.Name] = element;
    }

    public void AddAttribute(XsdAttribute attribute)
    {
        _attributes[attribute.Name] = attribute;
    }

    public XsdType? FindType(QName name) => _types.GetValueOrDefault(name);

    public XsdElement? FindElement(QName name) => _elements.GetValueOrDefault(name);

    public XsdElement ResolveElement(XsdElement element) =>
        element.Ref is { } reference
            ? FindElement(reference) ?? throw new SchemaException($"Unresolved element ref {reference}.")
            : element;

    public XsdAttribute ResolveAttribute(XsdAttribute attribute) =>
        attribute.Ref is { } reference
            ? _attributes.GetValueOrDefault(reference) ?? throw new SchemaException($"Unresolved attribute ref {reference}.")
            : attribute;

    /// <summary>
    /// Walks a complex type's inheritance chain from the given type up to its root. The chain
    /// stops at types outside the schema set (for example xs:anyType).
    /// </summary>
    public IEnumerable<XsdComplexType> BaseChain(XsdComplexType type)
    {
        var current = type;
        var guard = new HashSet<QName>();
        while (current.BaseType is { } baseName)
        {
            if (!guard.Add(baseName))
                throw new SchemaException($"Cyclic type inheritance involving {baseName}.");
            if (FindType(baseName) is not XsdComplexType parent) yield break;
            yield return parent;
            current = parent;
        }
    }
}
