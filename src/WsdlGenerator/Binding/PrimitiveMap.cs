using WsdlGenerator.Xml;

namespace WsdlGenerator.Binding;

/// <summary>
/// Maps XSD built-in types onto C# types, reproducing the mapping xsd.exe and svcutil used so
/// that the generated member types stay source-compatible with the previous bindings.
///
/// Several XSD primitives have no distinct CLR representation and are carried as strings; those
/// record a <c>DataType</c> so the emitted XmlSerializer attributes still describe the wire form.
/// </summary>
internal static class PrimitiveMap
{
    private static readonly Dictionary<string, CsTypeRef> Map = new(StringComparer.Ordinal)
    {
        // Types with a natural CLR equivalent.
        ["string"] = new("string", TypeKind.Primitive, false, "string"),
        ["boolean"] = new("bool", TypeKind.Primitive, true, "boolean"),
        ["byte"] = new("sbyte", TypeKind.Primitive, true, "byte"),
        ["unsignedByte"] = new("byte", TypeKind.Primitive, true, "unsignedByte"),
        ["short"] = new("short", TypeKind.Primitive, true, "short"),
        ["unsignedShort"] = new("ushort", TypeKind.Primitive, true, "unsignedShort"),
        ["int"] = new("int", TypeKind.Primitive, true, "int"),
        ["unsignedInt"] = new("uint", TypeKind.Primitive, true, "unsignedInt"),
        ["long"] = new("long", TypeKind.Primitive, true, "long"),
        ["unsignedLong"] = new("ulong", TypeKind.Primitive, true, "unsignedLong"),
        ["decimal"] = new("decimal", TypeKind.Primitive, true, "decimal"),
        ["float"] = new("float", TypeKind.Primitive, true, "float"),
        ["double"] = new("double", TypeKind.Primitive, true, "double"),
        ["dateTime"] = new("System.DateTime", TypeKind.Primitive, true, "dateTime"),
        ["base64Binary"] = new("byte[]", TypeKind.Primitive, false, "base64Binary"),
        ["QName"] = new("System.Xml.XmlQualifiedName", TypeKind.Primitive, false, "QName"),

        // Dates and times that XmlSerializer still surfaces as DateTime, but which need the
        // DataType annotation to be written back in the right lexical form.
        ["date"] = new("System.DateTime", TypeKind.Primitive, true, "date", "date"),
        ["time"] = new("System.DateTime", TypeKind.Primitive, true, "time", "time"),

        // Binary that is not base64.
        ["hexBinary"] = new("byte[]", TypeKind.Primitive, false, "hexBinary", "hexBinary"),

        // String-valued primitives. XmlSerializer keeps these as string and relies on DataType
        // to preserve the schema type.
        ["anyURI"] = new("string", TypeKind.Primitive, false, "anyURI", "anyURI"),
        ["duration"] = new("string", TypeKind.Primitive, false, "duration", "duration"),
        ["token"] = new("string", TypeKind.Primitive, false, "token", "token"),
        ["normalizedString"] = new("string", TypeKind.Primitive, false, "normalizedString", "normalizedString"),
        ["language"] = new("string", TypeKind.Primitive, false, "language", "language"),
        ["Name"] = new("string", TypeKind.Primitive, false, "Name", "Name"),
        ["NCName"] = new("string", TypeKind.Primitive, false, "NCName", "NCName"),
        ["NMTOKEN"] = new("string", TypeKind.Primitive, false, "NMTOKEN", "NMTOKEN"),
        ["NMTOKENS"] = new("string", TypeKind.Primitive, false, "NMTOKENS", "NMTOKENS"),
        ["ID"] = new("string", TypeKind.Primitive, false, "ID", "ID"),
        ["IDREF"] = new("string", TypeKind.Primitive, false, "IDREF", "IDREF"),
        ["IDREFS"] = new("string", TypeKind.Primitive, false, "IDREFS", "IDREFS"),
        ["ENTITY"] = new("string", TypeKind.Primitive, false, "ENTITY", "ENTITY"),
        ["ENTITIES"] = new("string", TypeKind.Primitive, false, "ENTITIES", "ENTITIES"),
        ["NOTATION"] = new("string", TypeKind.Primitive, false, "NOTATION", "NOTATION"),
        ["gYear"] = new("string", TypeKind.Primitive, false, "gYear", "gYear"),
        ["gMonth"] = new("string", TypeKind.Primitive, false, "gMonth", "gMonth"),
        ["gDay"] = new("string", TypeKind.Primitive, false, "gDay", "gDay"),
        ["gYearMonth"] = new("string", TypeKind.Primitive, false, "gYearMonth", "gYearMonth"),
        ["gMonthDay"] = new("string", TypeKind.Primitive, false, "gMonthDay", "gMonthDay"),

        // Unbounded integers have no CLR equivalent, so they stay strings.
        ["integer"] = new("string", TypeKind.Primitive, false, "integer", "integer"),
        ["nonNegativeInteger"] = new("string", TypeKind.Primitive, false, "nonNegativeInteger", "nonNegativeInteger"),
        ["positiveInteger"] = new("string", TypeKind.Primitive, false, "positiveInteger", "positiveInteger"),
        ["negativeInteger"] = new("string", TypeKind.Primitive, false, "negativeInteger", "negativeInteger"),
        ["nonPositiveInteger"] = new("string", TypeKind.Primitive, false, "nonPositiveInteger", "nonPositiveInteger"),

        // Wildcards.
        ["anyType"] = new("object", TypeKind.Object, false, "anyType"),
        ["anySimpleType"] = new("string", TypeKind.Primitive, false, "anySimpleType"),
    };

    /// <summary>The set of primitives whose values cannot survive as plain WCF parameters.</summary>
    private static readonly HashSet<string> NeedsAnnotation =
        new(Map.Where(kv => kv.Value.DataType is not null).Select(kv => kv.Key), StringComparer.Ordinal);

    public static bool IsXsd(QName name) => name.Namespace == Ns.Xsd;

    public static CsTypeRef? Lookup(QName name) =>
        name.Namespace == Ns.Xsd && Map.TryGetValue(name.LocalName, out var type) ? type : null;

    /// <summary>
    /// True for primitives that XmlSerializer can only round-trip with an explicit DataType, which
    /// is one of the signals that an operation prefers message-contract style. See doc/codegen.md.
    /// </summary>
    public static bool RequiresDataTypeAnnotation(QName name) =>
        name.Namespace == Ns.Xsd && NeedsAnnotation.Contains(name.LocalName);
}
