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
}
