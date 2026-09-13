using System.Net.Http;
using System.Xml.Linq;

namespace WsdlGenerator.Xml;

/// <summary>
/// Loads WSDL and XSD documents, following the references between them.
///
/// A document is identified by an absolute URI, so a relative schemaLocation resolves by ordinary
/// URI composition no matter where the document came from. Two sources are supported: an offline
/// mirror, and the file system with the network as a fallback.
/// </summary>
internal sealed class DocumentResolver
{
    private readonly string? _mirrorRoot;
    private readonly Dictionary<string, XDocument> _loaded = new(StringComparer.Ordinal);
    private readonly Lazy<HttpClient> _http = new(() => new HttpClient());

    private DocumentResolver(string? mirrorRoot) => _mirrorRoot = mirrorRoot;

    /// <summary>
    /// Resolves every document from a directory holding a mirror of the remote layout, the one
    /// <c>wsdl/fetch.sh</c> writes: the URL with its scheme stripped is the path under the root.
    /// Nothing is fetched, so generation is reproducible and works offline.
    /// </summary>
    public static DocumentResolver FromMirror(string mirrorRoot)
    {
        string root = Path.GetFullPath(mirrorRoot);
        if (!Directory.Exists(root))
            throw new SchemaException($"Schema mirror not found at '{root}'.");

        return new DocumentResolver(root);
    }

    /// <summary>
    /// Resolves documents from the file system, downloading the ones referenced by an http or
    /// https URL. Use a mirror instead when generation has to be reproducible or offline.
    /// </summary>
    public static DocumentResolver FromFileSystem() => new(null);

    /// <summary>All documents loaded so far, keyed by their absolute URI.</summary>
    public IReadOnlyDictionary<string, XDocument> Loaded => _loaded;

    /// <summary>
    /// Resolves <paramref name="reference"/>, absolute or relative to <paramref name="baseUri"/>,
    /// and returns the parsed document. Repeat requests return the same instance.
    /// </summary>
    public XDocument Load(string reference, string? baseUri = null)
    {
        string uri = Combine(reference, baseUri);
        if (_loaded.TryGetValue(uri, out var cached)) return cached;

        string text = _mirrorRoot is null ? ReadDirect(uri) : ReadFromMirror(uri);

        XDocument document;
        using (var reader = new StringReader(text))
        {
            // SetBaseUri would give the temporary reader's identity, so the URI is recorded by
            // annotating the document instead; SchemaException reports it.
            document = XDocument.Load(reader, LoadOptions.SetLineInfo);
        }

        document.AddAnnotation(new DocumentUri(uri));
        _loaded[uri] = document;
        return document;
    }

    /// <summary>Records where a document came from, for error messages.</summary>
    internal sealed record DocumentUri(string Value);

    private string ReadFromMirror(string uri)
    {
        string path = ToMirrorPath(uri);
        if (File.Exists(path)) return File.ReadAllText(path);

        throw new SchemaException(
            $"'{uri}' is referenced but not mirrored (expected at '{path}'). " +
            "Add it to the mirror's source list and fetch it again.");
    }

    private string ReadDirect(string uri)
    {
        if (uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            string path = new Uri(uri).LocalPath;
            if (!File.Exists(path)) throw new SchemaException($"'{path}' does not exist.");
            return File.ReadAllText(path);
        }

        try
        {
            return _http.Value.GetStringAsync(uri).GetAwaiter().GetResult();
        }
        catch (HttpRequestException error)
        {
            throw new SchemaException($"Could not download '{uri}': {error.Message}");
        }
    }

    /// <summary>
    /// Turns a reference into the identity of the document it names. A bare path becomes a file
    /// URI, so documents from disk and from the web compose the same way.
    /// <para>
    /// A mirror holds one copy per host and path, so there the scheme is dropped: the Onvif
    /// schemas are reached as http from some documents and https from others, and parsing the
    /// same schema twice would make every type in it conflict with itself.
    /// </para>
    /// </summary>
    public string Combine(string reference, string? baseUri)
    {
        string absolute = baseUri is null
            ? Root(reference)
            : new Uri(new Uri(WithScheme(baseUri)), reference).ToString();

        return _mirrorRoot is null ? absolute : StripScheme(absolute);
    }

    /// <summary>Restores a scheme to an identity the mirror stripped, so it can compose again.</summary>
    private static string WithScheme(string uri) =>
        uri.Contains("://", StringComparison.Ordinal) ? uri : "http://" + uri;

    private static string StripScheme(string uri)
    {
        foreach (string scheme in new[] { "https://", "http://" })
        {
            if (uri.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)) return uri.Substring(scheme.Length);
        }
        return uri;
    }

    /// <summary>
    /// Resolves a reference that has no base to compose against, which is how a run starts and
    /// how an identity is passed back in. Must be idempotent: feeding an identity back in has to
    /// yield the same identity rather than reading it as a path on disk.
    /// </summary>
    private string Root(string reference)
    {
        // A single-letter scheme is a Windows drive, not a scheme; everything else that parses as
        // an absolute URI already is one, including the file URIs this method produces.
        if (Uri.TryCreate(reference, UriKind.Absolute, out var absolute) && absolute.Scheme.Length > 1)
            return absolute.ToString();

        // Under a mirror, identities carry no scheme, so a reference without one is already an
        // identity rather than a path.
        if (_mirrorRoot is not null) return WithScheme(reference);

        return new Uri(Path.GetFullPath(reference)).ToString();
    }

    /// <summary>
    /// The mirror stores one copy per host and path, so the http and https forms of a document
    /// have to agree. The scheme is dropped throughout.
    /// </summary>
    private string ToMirrorPath(string uri)
    {
        string relative = StripScheme(uri);
        if (relative.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            relative = relative.Substring("file://".Length);

        int query = relative.IndexOfAny(['?', '#']);
        if (query >= 0) relative = relative.Substring(0, query);

        return Path.Combine(_mirrorRoot!, relative.Replace('/', Path.DirectorySeparatorChar));
    }
}
