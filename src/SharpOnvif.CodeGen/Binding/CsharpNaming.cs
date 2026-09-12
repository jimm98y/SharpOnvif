using System.Text;

namespace SharpOnvif.CodeGen.Binding;

/// <summary>
/// Turns XML names into legal, non-colliding C# identifiers, following the same conventions as
/// xsd.exe so the generated surface matches the bindings it replaces.
/// </summary>
internal static class CsharpNaming
{
    private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
        "void", "volatile", "while",
    };

    /// <summary>
    /// Turns an XML name into a C# identifier by dropping characters that cannot appear in one,
    /// which is what xsd.exe does: the element "TLS1.0" becomes TLS10, not TLS1_0. The original
    /// name is preserved on the wire and in the emitted XmlElement attribute.
    /// <para>
    /// Casing is left alone, because ONVIF member names are part of the public surface.
    /// </para>
    /// </summary>
    public static string Identifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Item";

        var builder = new StringBuilder(name.Length);
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_') builder.Append(c);
        }

        if (builder.Length == 0) return "Item";
        if (char.IsDigit(builder[0])) builder.Insert(0, 'i');
        return builder.ToString();
    }

    /// <summary>Escapes a C# keyword so it can still be used as an identifier.</summary>
    public static string Escape(string identifier) =>
        Keywords.Contains(identifier) ? "@" + identifier : identifier;

    /// <summary>
    /// Appends a numeric suffix until the name is unused, matching how xsd.exe disambiguates
    /// two schema types that map onto the same C# name (for example a type and an element of
    /// the same name in different namespaces).
    /// </summary>
    public static string Unique(string candidate, ISet<string> taken)
    {
        if (taken.Add(candidate)) return candidate;
        for (int suffix = 1; ; suffix++)
        {
            string attempt = candidate + suffix;
            if (taken.Add(attempt)) return attempt;
        }
    }
}
