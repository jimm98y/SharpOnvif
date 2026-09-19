using System.Text;

namespace WsdlGenerator.Binding;

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
    /// Casing is left alone, because a schema's member names are part of the public surface.
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
    /// the networking, text and XML namespaces a service of this kind pulls in. With implicit
    /// usings on, System is always in scope, so a contract called DateTime would be ambiguous in
    /// any file that also imports the generated namespace.
    /// </para>
    /// <para>
    /// The list is deliberately wider than the names that collide today, so that a future
    /// specification revision introducing, say, a Stream or a Task type does not reintroduce the
    /// problem. A name in this list is given the run's prefix; the XML name is untouched, so the
    /// wire format does not move.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> FrameworkTypeNames = new(StringComparer.Ordinal)
    {
        // Colliding with the schemas this was written for.
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
    /// <param name="prefix">
    /// What to put in front of such a name, or null to leave it and the collision alone. It is a
    /// run's choice because it is a convention for one family of schemas, and one that is
    /// expensive to change afterwards: it appears in every caller's source.
    /// </param>
    public static string TypeName(string localName, string? prefix)
    {
        string identifier = Capitalised(Identifier(localName));

        return prefix is { Length: > 0 } && FrameworkTypeNames.Contains(identifier)
            ? prefix + identifier
            : identifier;
    }

    /// <summary>
    /// Gives a capital to a type name that is nothing but lower-case ASCII letters, because C#
    /// has reserved that shape for itself: CS8981 warns that such a name may become a keyword of
    /// the language. A consumer building with warnings as errors cannot compile the output
    /// otherwise, and a generated file is not somewhere a warning can be suppressed by hand.
    /// <para>
    /// The SOAP 1.2 envelope schema names five of its types that way - detail, faultcode,
    /// faultreason, reasontext and subcode - and no other schema here does.
    /// </para>
    /// <para>
    /// Only the first letter, and only when nothing else in the name distinguishes it: the
    /// narrowest change that answers the warning. Casing is otherwise still left alone, because a
    /// schema's names are part of the public surface. The name on the wire does not move either
    /// way - the type carries it in XmlTypeName, the way a renamed framework collision does.
    /// </para>
    /// </summary>
    private static string Capitalised(string identifier)
    {
        if (identifier.Length == 0) return identifier;

        foreach (char c in identifier)
        {
            if (!char.IsAsciiLetterLower(c)) return identifier;
        }

        return char.ToUpperInvariant(identifier[0]) + identifier.Substring(1);
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
