using System.Xml.Linq;

namespace SharpOnvif.CodeGen.Xml;

/// <summary>
/// Loads WSDL and XSD documents from the offline mirror under <c>wsdl/</c>. Absolute URLs map
/// onto mirror paths by dropping the scheme, which is the layout <c>wsdl/fetch.sh</c> writes, so
/// relative schemaLocation references resolve by ordinary URI composition. The generator never
/// touches the network: an unmirrored reference is an error telling the user to re-run fetch.sh.
/// </summary>
internal sealed class DocumentResolver
{
    private readonly string _mirrorRoot;
    private readonly Dictionary<string, XDocument> _loaded = new(StringComparer.Ordinal);

    public DocumentResolver(string mirrorRoot)
    {
        _mirrorRoot = Path.GetFullPath(mirrorRoot);
        if (!Directory.Exists(_mirrorRoot))
            throw new SchemaException($"Schema mirror not found at '{_mirrorRoot}'. Run wsdl/fetch.sh first.");
    }

    /// <summary>All documents loaded so far, keyed by their absolute URL.</summary>
    public IReadOnlyDictionary<string, XDocument> Loaded => _loaded;

    /// <summary>
    /// Resolves <paramref name="reference"/> (absolute, or relative to <paramref name="baseUrl"/>)
    /// and returns the parsed document. Repeat requests return the same instance.
    /// </summary>
    public XDocument Load(string reference, string? baseUrl = null)
    {
        string url = Combine(reference, baseUrl);
        if (_loaded.TryGetValue(url, out var cached)) return cached;

        string path = ToMirrorPath(url);
        if (!File.Exists(path))
        {
            throw new SchemaException(
                $"'{url}' is referenced but not mirrored (expected at '{path}'). " +
                "Add it to wsdl/sources.txt and re-run wsdl/fetch.sh.");
        }

        XDocument document;
        using (var reader = File.OpenText(path))
        {
            // SetBaseUri makes BaseUri available on every node, which SchemaException reports.
            document = XDocument.Load(reader, LoadOptions.SetLineInfo | LoadOptions.SetBaseUri);
        }

        _loaded[url] = document;
        return document;
    }

    /// <summary>Normalises a reference to the absolute URL used as the document's identity.</summary>
    public static string Combine(string reference, string? baseUrl)
    {
        if (baseUrl is null) return Normalise(reference);
        return Normalise(new Uri(new Uri(NormaliseToHttp(baseUrl)), reference).ToString());
    }

    /// <summary>
    /// The mirror stores one copy per host+path, so https and http forms of the same document
    /// must agree. fetch.sh strips the scheme, so we key on the scheme-less form throughout.
    /// </summary>
    private static string Normalise(string url) =>
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? url.Substring(8)
        : url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? url.Substring(7)
        : url;

    private static string NormaliseToHttp(string url) =>
        url.Contains("://", StringComparison.Ordinal) ? url : "http://" + url;

    private string ToMirrorPath(string url)
    {
        string relative = Normalise(url);
        int query = relative.IndexOfAny(['?', '#']);
        if (query >= 0) relative = relative.Substring(0, query);
        return Path.Combine(_mirrorRoot, relative.Replace('/', Path.DirectorySeparatorChar));
    }
}
