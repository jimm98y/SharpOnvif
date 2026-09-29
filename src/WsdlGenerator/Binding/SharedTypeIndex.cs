using WsdlGenerator.Xml;

namespace WsdlGenerator.Binding;

/// <summary>
/// The types generated once into the common assembly, looked up by their schema name.
///
/// Services of one family share a large body of schema - their own common one, plus the OASIS and W3C
/// schemas the event service pulls in. Generating those per service produced the same few hundred
/// types twenty-five times over. They are generated once instead, and a service that refers to one
/// gets a reference into the shared namespace.
/// </summary>
internal sealed class SharedTypeIndex
{
    public static readonly SharedTypeIndex Empty = new(new Dictionary<QName, CsTypeRef>(), new Dictionary<QName, CsClass>());

    private readonly Dictionary<QName, CsTypeRef> _types;
    private readonly Dictionary<QName, CsClass> _classes;

    public SharedTypeIndex(Dictionary<QName, CsTypeRef> types, Dictionary<QName, CsClass> classes)
    {
        _types = types;
        _classes = classes;
    }

    public bool Contains(QName name) => _types.ContainsKey(name);

    public bool TryResolve(QName name, out CsTypeRef type) => _types.TryGetValue(name, out type!);

    /// <summary>
    /// The shared class itself, for a service type that extends it. The class has to be linked,
    /// not merely named, or the derived class loses every attribute and element it inherits.
    /// </summary>
    public bool TryResolveClass(QName name, out CsClass @class) => _classes.TryGetValue(name, out @class!);

    /// <summary>Builds the index from a model that has already been named.</summary>
    public static SharedTypeIndex From(CsModel model)
    {
        var types = new Dictionary<QName, CsTypeRef>();
        var classes = new Dictionary<QName, CsClass>();

        foreach (var @class in model.Classes)
        {
            if (@class.XmlName is not { } name) continue;
            classes[name] = @class;
            types[name] = new CsTypeRef(
                @class.NameFrom(from: ""), TypeKind.Class, false, XmlTypeName: name);
        }

        foreach (var @enum in model.Enums)
        {
            // Only enums that came from a named schema type are shareable; the ones invented for
            // an inline type or a choice discriminator belong to the type that declared them.
            if (@enum.XmlName.Namespace.Length == 0) continue;
            types[@enum.XmlName] = new CsTypeRef(
                model.CsNamespace + "." + @enum.Name, TypeKind.Enum, true, IsQNameEnum: @enum.IsQName);
        }

        return new SharedTypeIndex(types, classes);
    }
}
