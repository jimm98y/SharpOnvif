namespace WsdlGenerator.Generator;

internal static class TypeMapper
{
    public static string ToCSharpType(string xsdType, bool isOptional, bool isMultiple)
    {
        string baseType = MapXsdToCSharp(xsdType);
        if (isMultiple) return $"IEnumerable<{baseType}>";
        if (isOptional && IsValueType(baseType)) return $"{baseType}?";
        if (isOptional) return $"{baseType}?";
        return baseType;
    }

    public static string MapXsdToCSharp(string xsdType) => xsdType switch
    {
        "xs:string" or "xs:token" or "xs:anyURI" or "xs:duration"
            or "xs:dateTime" or "xs:date" or "xs:time" or "xs:base64Binary"
            or "xs:hexBinary" or "xs:NCName" or "xs:QName" or "xs:ID"
            or "xs:NMTOKEN" or "xs:normalizedString" or "xs:Name"
            or "xs:IDREF" or "xs:IDREFS" => "string",
        "xs:boolean" => "bool",
        "xs:int" or "xs:integer" or "xs:unsignedShort" or "xs:short" or "xs:nonNegativeInteger" => "int",
        "xs:long" or "xs:unsignedInt" => "long",
        "xs:float" => "float",
        "xs:double" => "double",
        "xs:decimal" => "decimal",
        "xs:byte" or "xs:unsignedByte" => "byte",
        _ => "XElement"    // external/complex types (tt:*, tds:*, xs:anyType, etc.)
    };

    public static bool IsValueType(string csharpType) =>
        csharpType is "bool" or "int" or "long" or "float" or "double" or "decimal" or "byte";

    public static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    // Produces the C# expression for writing a field value into XML text content
    public static string ToXmlStringExpression(string csharpType, string varName) => csharpType switch
    {
        "bool" => $"({varName} ? \"true\" : \"false\")",
        "bool?" => $"({varName}.HasValue ? ({varName}.Value ? \"true\" : \"false\") : null)",
        "string" or "string?" => varName,
        "XElement" or "XElement?" => varName,
        _ => $"{varName}.ToString()"
    };
}
