using WsdlGenerator.Xml;

namespace WsdlGenerator.Xsd;

/// <summary>
/// A value added to a schema enumeration that the schema itself does not list.
/// </summary>
/// <remarks>
/// Published schemas trail the implementations that follow them: an enumeration of codecs still
/// enumerates JPEG, MPEG4 and H264, while cameras have been answering H265 and AV1 for years, and
/// a generated enum that cannot name a value cannot carry it. Adding the value here rather than to
/// the generated file means regenerating keeps it, and keeps the conversions to and from its XML
/// form in step with it.
/// </remarks>
internal sealed record EnumerationExtension(QName Type, string Value, string? Documentation = null);

/// <summary>
/// A named type given to an element of a complex type in place of the one the schema declares.
/// </summary>
/// <remarks>
/// Published schemas also change under the implementations that follow them. An element once typed
/// can be loosened to a wildcard in a later revision, with only a comment left to say what it
/// holds, and a wildcard leaves the caller parsing that content out of raw XML. What goes over the
/// wire is the same either way, so the named type reads it as before.
/// </remarks>
internal sealed record ElementTypeOverride(QName Type, string Element, QName ElementType);

/// <summary>
/// An optional, unqualified attribute given to a complex type that the schema does not declare.
/// </summary>
/// <remarks>
/// A later revision of a schema can drop an attribute that implementations went on sending, and
/// that callers went on reading. Declaring it again keeps it a typed property, rather than one
/// more entry among the attributes a wildcard kept.
/// </remarks>
internal sealed record AttributeAddition(QName Type, string Attribute, QName AttributeType, string? Documentation = null);

internal static class SchemaExtensions
{
    /// <summary>
    /// Adds the configured values to the enumerations they name, before any code is modelled from
    /// the schema, so the enum and the conversions generated alongside it agree.
    /// </summary>
    public static void Apply(XsdSchemaSet schema, IReadOnlyList<EnumerationExtension> extensions)
    {
        foreach (var extension in extensions)
        {
            if (schema.FindType(extension.Type) is not XsdSimpleType simple)
                throw new SchemaException($"No simple type {extension.Type} to add '{extension.Value}' to.");

            if (!simple.IsEnumeration)
                throw new SchemaException($"{extension.Type} does not enumerate its values, so '{extension.Value}' cannot be added to it.");

            // A schema that catches up with the devices makes the extension redundant rather than
            // wrong, so leave the one the schema now carries in place.
            if (simple.Enumerations.Any(value => string.Equals(value.Value, extension.Value, StringComparison.Ordinal)))
                continue;

            simple.Extend(new XsdEnumValue(extension.Value, extension.Documentation));
        }
    }

    /// <summary>
    /// Retypes the configured elements, before any code is modelled from the schema, so the
    /// anonymous type the element declared is never generated.
    /// </summary>
    public static void Apply(XsdSchemaSet schema, IReadOnlyList<ElementTypeOverride> overrides)
    {
        foreach (var @override in overrides)
        {
            if (schema.FindType(@override.Type) is not XsdComplexType complex)
                throw new SchemaException($"No complex type {@override.Type} to retype '{@override.Element}' in.");

            if (schema.FindType(@override.ElementType) is null)
                throw new SchemaException($"No type {@override.ElementType} to give {@override.Type}/{@override.Element}.");

            var element = FindElement(complex.Particle, @override.Element)
                ?? throw new SchemaException($"{@override.Type} declares no element '{@override.Element}'.");

            element.Retype(@override.ElementType);
        }
    }

    /// <summary>
    /// Adds the configured attributes to the types they name, before any code is modelled from
    /// the schema, so they are read and written like the ones the schema declares.
    /// </summary>
    public static void Apply(XsdSchemaSet schema, IReadOnlyList<AttributeAddition> additions)
    {
        foreach (var addition in additions)
        {
            if (schema.FindType(addition.Type) is not XsdComplexType complex)
                throw new SchemaException($"No complex type {addition.Type} to add the attribute '{addition.Attribute}' to.");

            if (schema.FindType(addition.AttributeType) is null && !IsBuiltIn(addition.AttributeType))
                throw new SchemaException($"No type {addition.AttributeType} to give {addition.Type}/@{addition.Attribute}.");

            // A schema that declares it again makes the addition redundant rather than wrong.
            if (complex.Attributes.Any(a => a.Name.LocalName == addition.Attribute || a.Ref?.LocalName == addition.Attribute))
                continue;

            complex.AddAttribute(new XsdAttribute
            {
                Name = new QName("", addition.Attribute),
                TypeName = addition.AttributeType,
                Use = AttributeUse.Optional,
                Documentation = addition.Documentation,
            });
        }
    }

    private static bool IsBuiltIn(QName type) => type.Namespace == Ns.Xsd;

    /// <summary>The local element of that name in a content model, searched through its groups.</summary>
    private static XsdElement? FindElement(XsdParticle? particle, string localName)
    {
        switch (particle)
        {
            case XsdElementParticle { Element: var element } when element.Name.LocalName == localName:
                return element;
            case XsdSequence sequence:
                return sequence.Items.Select(item => FindElement(item, localName)).FirstOrDefault(e => e is not null);
            case XsdChoice choice:
                return choice.Items.Select(item => FindElement(item, localName)).FirstOrDefault(e => e is not null);
            default:
                return null;
        }
    }
}
