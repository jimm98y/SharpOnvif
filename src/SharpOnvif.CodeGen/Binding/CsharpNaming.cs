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

    /// <summary>
    /// Type names a generated type must not take, because a consumer is almost certain to have
    /// them in scope already and the two would be ambiguous.
    /// <para>
    /// Drawn from the net10.0 reference assemblies for the namespaces a consumer typically has
    /// imported: the implicit usings of a modern project (System, System.Collections.Generic,
    /// System.IO, System.Linq, System.Net.Http, System.Threading, System.Threading.Tasks) plus
    /// the networking, text and XML namespaces this domain pulls in. With implicit usings on,
    /// System is always in scope, so a contract called DateTime would be ambiguous in any file
    /// that also imports the Onvif namespace.
    /// </para>
    /// <para>
    /// The list is deliberately wider than the names that collide today, so that a future
    /// specification revision introducing, say, a Stream or a Task type does not reintroduce the
    /// problem. A name in this list is prefixed with "Onvif"; the XML name is untouched, so the
    /// wire format does not move.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> FrameworkTypeNames = new(StringComparer.Ordinal)
    {
        // Colliding with the Onvif schema today.
        "Action", "Attribute", "DateTime", "IPAddress", "NetworkInterface", "Object", "Scope",
        "TimeZone",

        // System, and the parts of it that carry names a schema might plausibly reuse.
        "Array", "Boolean", "Buffer", "Byte", "Char", "Comparison", "Console", "Convert",
        "DateOnly", "DateTimeOffset", "Decimal", "Delegate", "Double", "Enum", "Environment",
        "Exception", "Func", "Guid", "Half", "Index", "Int16", "Int32", "Int64", "Math",
        "Nullable", "Predicate", "Random", "Range", "Single", "String", "TimeOnly", "TimeSpan",
        "TimeProvider", "Tuple", "Type", "UInt16", "UInt32", "UInt64", "Uri", "UriBuilder",
        "ValueType", "Version", "Void",

        // Collections, IO, threading and tasks.
        "Comparer", "Dictionary", "HashSet", "KeyValuePair", "List", "Queue", "Stack",
        "Directory", "File", "Path", "Stream", "StreamReader", "StreamWriter", "TextReader",
        "TextWriter", "CancellationToken", "Mutex", "Semaphore", "Task", "Timer",

        // Networking, text and XML.
        "Cookie", "Credential", "DnsEndPoint", "EndPoint", "HttpClient", "HttpMethod",
        "IPEndPoint", "NetworkCredential", "Socket", "WebClient", "WebRequest", "WebResponse",
        "Encoding", "Rune", "StringBuilder", "XmlDocument", "XmlElement", "XmlNode", "XmlReader",
        "XmlWriter", "XmlQualifiedName",
    };

    /// <summary>
    /// Gives a generated type a name that will not be ambiguous with a framework type a consumer
    /// has in scope. Only the C# name changes; the schema name it serialises as does not.
    /// </summary>
    public static string TypeName(string localName)
    {
        string identifier = Identifier(localName);
        return FrameworkTypeNames.Contains(identifier) ? "Onvif" + identifier : identifier;
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
