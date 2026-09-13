namespace WsdlGenerator.Xml;

/// <summary>
/// XML namespaces the parsers and emitters need to recognise by name.
/// </summary>
internal static class Ns
{
    public const string Xsd = "http://www.w3.org/2001/XMLSchema";
    public const string Xsi = "http://www.w3.org/2001/XMLSchema-instance";
    public const string Xml = "http://www.w3.org/XML/1998/namespace";
    public const string Wsdl = "http://schemas.xmlsoap.org/wsdl/";
    public const string WsdlSoap12 = "http://schemas.xmlsoap.org/wsdl/soap12/";
    public const string WsdlSoap11 = "http://schemas.xmlsoap.org/wsdl/soap/";
    public const string Soap12Envelope = "http://www.w3.org/2003/05/soap-envelope";
    public const string Addressing = "http://www.w3.org/2005/08/addressing";
}

/// <summary>
/// A namespace-qualified XML name. Used as a dictionary key throughout the schema set,
/// so equality is ordinal on both halves.
/// </summary>
internal readonly record struct QName(string Namespace, string LocalName)
{
    public override string ToString() =>
        string.IsNullOrEmpty(Namespace) ? LocalName : "{" + Namespace + "}" + LocalName;

    /// <summary>
    /// Resolves a possibly prefixed name ("tt:Foo") against the prefix scope of <paramref name="context"/>.
    /// </summary>
    public static QName Parse(string value, System.Xml.Linq.XElement context)
    {
        int colon = value.IndexOf(':');
        if (colon < 0)
        {
            // An unprefixed QName binds to whatever xmlns="..." is in scope, or to no
            // namespace at all when the document declares no default.
            return new QName(context.GetDefaultNamespace().NamespaceName, value);
        }

        string prefix = value.Substring(0, colon);
        string local = value.Substring(colon + 1);
        var resolved = context.GetNamespaceOfPrefix(prefix)
            ?? throw new SchemaException(context, $"Undeclared namespace prefix '{prefix}' in QName '{value}'.");
        return new QName(resolved.NamespaceName, local);
    }
}

/// <summary>
/// Raised when a source document is malformed or uses a construct the generator does not model.
/// Carries the document line so the offending WSDL can be found quickly.
/// </summary>
internal sealed class SchemaException : Exception
{
    public SchemaException(string message) : base(message) { }

    public SchemaException(System.Xml.Linq.XObject? at, string message)
        : base(Describe(at) + message) { }

    private static string Describe(System.Xml.Linq.XObject? at)
    {
        if (at is null) return "";

        // The document records where it came from; XObject.BaseUri only knows about readers that
        // had a URI of their own, which the loader does not use.
        string where = at.Document?.Annotation<DocumentResolver.DocumentUri>()?.Value ?? "";

        var line = at as System.Xml.IXmlLineInfo;
        if (line is not null && line.HasLineInfo()) where += $"({line.LineNumber},{line.LinePosition})";

        return where.Length == 0 ? "" : where + ": ";
    }
}
