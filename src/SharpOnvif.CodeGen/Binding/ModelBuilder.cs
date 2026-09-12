using System.Text;
using SharpOnvif.CodeGen.Wsdl;
using SharpOnvif.CodeGen.Xml;
using SharpOnvif.CodeGen.Xsd;

namespace SharpOnvif.CodeGen.Binding;

/// <summary>
/// Turns one service's parsed schema and WSDL into the C# model the emitters render.
///
/// Only types reachable from the service's operations are generated, matching svcutil: the shared
/// onvif.xsd declares far more than any one service uses, and generating all of it would bloat
/// every assembly. Reachability also pulls in derived types of anything reachable, because a
/// response may name one through xsi:type.
/// </summary>
internal sealed class ModelBuilder
{
    private readonly XsdSchemaSet _schema;
    private readonly IReadOnlyList<WsdlParser> _wsdls;
    private readonly string _serviceName;

    private readonly Dictionary<QName, CsClass> _classesByName = [];
    private readonly Dictionary<QName, CsEnum> _enumsByName = [];
    private readonly Dictionary<XsdType, CsClass> _classesByType = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<XsdType, CsEnum> _enumsByType = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<string> _takenTypeNames = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CsClass> _wrappers = new(StringComparer.Ordinal);
    private readonly CsModel _model;

    private readonly bool _generateEntireSchema;
    private readonly string _csNamespace;
    private readonly SharedTypeIndex _shared;

    /// <param name="csNamespace">C# namespace this model is generated into.</param>
    /// <param name="shared">
    /// Types already generated into the common assembly. A reference to one of these produces a
    /// qualified reference instead of a second copy of the type.
    /// </param>
    public ModelBuilder(
        string serviceName,
        XsdSchemaSet schema,
        WsdlParser wsdl,
        bool generateEntireSchema = false,
        string csNamespace = "",
        SharedTypeIndex? shared = null)
        : this(serviceName, schema, [wsdl], generateEntireSchema, csNamespace, shared, null, true)
    {
    }

    /// <param name="declareFilter">
    /// Decides which reachable types this model declares. The walk always descends through every
    /// type, because a service's own type is how a shared one is reached; the filter only says
    /// which of them belong here.
    /// </param>
    /// <param name="emitServices">False for the shared model, which has no operations of its own.</param>
    private ModelBuilder(
        string serviceName,
        XsdSchemaSet schema,
        IReadOnlyList<WsdlParser> wsdls,
        bool generateEntireSchema,
        string csNamespace,
        SharedTypeIndex? shared,
        Func<QName, bool>? declareFilter,
        bool emitServices)
    {
        _serviceName = serviceName;
        _schema = schema;
        _wsdls = wsdls;
        _generateEntireSchema = generateEntireSchema;
        _csNamespace = csNamespace;
        _shared = shared ?? SharedTypeIndex.Empty;
        _declareFilter = declareFilter;
        _emitServices = emitServices;
        _model = new CsModel { ServiceName = serviceName, CsNamespace = csNamespace };
    }

    /// <summary>
    /// Builds the model of everything the services share: the Onvif schema, and the OASIS and W3C
    /// schemas the event service pulls in. Generated once into the common assembly.
    /// <para>
    /// Every type in those schemas is generated, not only the ones some operation reaches, so the
    /// shared assembly is a complete rendering of the Onvif data model.
    /// </para>
    /// </summary>
    public static CsModel BuildShared(
        XsdSchemaSet schema,
        IReadOnlyList<WsdlParser> wsdls,
        ISet<string> serviceNamespaces,
        string csNamespace)
    {
        return new ModelBuilder(
            "Shared", schema, wsdls,
            generateEntireSchema: true,
            csNamespace: csNamespace,
            shared: null,
            declareFilter: name => !serviceNamespaces.Contains(name.Namespace),
            emitServices: false).Build();
    }

    private readonly Func<QName, bool>? _declareFilter;
    private readonly bool _emitServices;

    public CsModel Build()
    {
        // Reserve the names of the wrapper classes first so a schema type never steals one.
        foreach (var portType in _wsdls.SelectMany(w => w.BoundPortTypes))
        {
            foreach (var operation in portType.Operations)
            {
                _takenTypeNames.Add(operation.Name + "Request");
                _takenTypeNames.Add(operation.Name + "Response");
            }
            _takenTypeNames.Add(portType.Name.LocalName);
            _takenTypeNames.Add(portType.Name.LocalName + "Client");
        }

        var reachable = ComputeReachableTypes()
            .Where(t => t.Name is not { } name || _declareFilter is null || _declareFilter(name))
            .ToList();

        foreach (var type in reachable) Declare(type);
        foreach (var type in reachable) Populate(type);

        LinkInheritance();
        if (_emitServices) BuildServices();

        _model.Classes.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        _model.Enums.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return _model;
    }

    // ---------------------------------------------------------------- reachability

    /// <summary>
    /// Collects every named schema type the service's operations can put on the wire, starting
    /// from the message elements and closing over members, base types, and derived types.
    /// </summary>
    private List<XsdType> ComputeReachableTypes()
    {
        var seen = new HashSet<QName>();
        var ordered = new List<XsdType>();
        var queue = new Queue<QName>();

        if (_generateEntireSchema)
        {
            // Seed with everything, so the walk below ends up visiting the whole schema set.
            foreach (var name in _schema.Types.Keys) { seen.Add(name); queue.Enqueue(name); }
        }

        void Enqueue(QName name)
        {
            if (PrimitiveMap.IsXsd(name)) return;

            // Anything the common assembly already carries is referenced, not regenerated, and
            // nothing it refers to needs collecting either.
            if (_shared.Contains(name)) return;

            if (seen.Add(name)) queue.Enqueue(name);
        }

        foreach (var portType in _wsdls.SelectMany(w => w.BoundPortTypes))
        {
            foreach (var operation in portType.Operations)
            {
                foreach (var message in Messages(operation, portType))
                {
                    foreach (var part in message.Parts)
                    {
                        if (part.Element is { } element && _schema.FindElement(element) is { } declaration)
                            EnqueueElement(declaration, Enqueue);
                        else if (part.Type is { } type)
                            Enqueue(type);
                    }
                }
            }
        }

        // Drain, then close over derived types and drain again: a newly reached derived type
        // can itself reference types nothing else mentioned.
        var derivedIndex = BuildDerivedIndex();
        while (queue.Count > 0)
        {
            while (queue.Count > 0)
            {
                var name = queue.Dequeue();
                if (_schema.FindType(name) is not { } type) continue;
                ordered.Add(type);
                EnqueueTypeReferences(type, Enqueue);
            }

            foreach (var name in seen.ToList())
            {
                if (!derivedIndex.TryGetValue(name, out var derived)) continue;
                foreach (var child in derived) Enqueue(child);
            }
        }

        return ordered;
    }

    private IEnumerable<WsdlMessage> Messages(WsdlOperation operation, WsdlPortType portType)
    {
        foreach (var name in new[] { operation.Input, operation.Output })
        {
            if (name is { } message && FindMessage(message) is { } found) yield return found;
        }
        foreach (var fault in operation.Faults)
        {
            if (FindMessage(fault.Message) is { } found) yield return found;
        }
    }

    private WsdlMessage? FindMessage(QName name)
    {
        foreach (var wsdl in _wsdls)
        {
            if (wsdl.Messages.TryGetValue(name, out var message)) return message;
        }
        return null;
    }

    /// <summary>Maps a base type name to the types that extend it.</summary>
    private Dictionary<QName, List<QName>> BuildDerivedIndex()
    {
        var index = new Dictionary<QName, List<QName>>();
        foreach (var (name, type) in _schema.Types)
        {
            if (type is not XsdComplexType complex || complex.BaseType is not { } baseName) continue;
            if (!index.TryGetValue(baseName, out var list)) index[baseName] = list = [];
            list.Add(name);
        }
        return index;
    }

    private void EnqueueElement(XsdElement element, Action<QName> enqueue)
    {
        var resolved = _schema.ResolveElement(element);
        if (resolved.TypeName is { } named) enqueue(named);
        else if (resolved.InlineType is { } inline) EnqueueTypeReferences(inline, enqueue);
    }

    private void EnqueueTypeReferences(XsdType type, Action<QName> enqueue)
    {
        switch (type)
        {
            case XsdSimpleType simple:
                if (simple.BaseType is { } simpleBase) enqueue(simpleBase);
                if (simple.ItemType is { } item) enqueue(item);
                foreach (var member in simple.MemberTypes) enqueue(member);
                break;

            case XsdComplexType complex:
                if (complex.BaseType is { } complexBase) enqueue(complexBase);
                if (complex.SimpleContentType is { } text) enqueue(text);
                foreach (var attribute in complex.Attributes)
                {
                    var resolved = _schema.ResolveAttribute(attribute);
                    if (resolved.TypeName is { } attributeType) enqueue(attributeType);
                }
                if (complex.Particle is { } particle) EnqueueParticle(particle, enqueue);
                break;
        }
    }

    private void EnqueueParticle(XsdParticle particle, Action<QName> enqueue)
    {
        switch (particle)
        {
            case XsdSequence sequence:
                foreach (var item in sequence.Items) EnqueueParticle(item, enqueue);
                break;
            case XsdChoice choice:
                foreach (var item in choice.Items) EnqueueParticle(item, enqueue);
                break;
            case XsdElementParticle element:
                EnqueueElement(element.Element, enqueue);
                break;
        }
    }

    // ---------------------------------------------------------------- declaration

    /// <summary>Creates the C# declaration for a schema type without filling in its members.</summary>
    private void Declare(XsdType type)
    {
        if (type.Name is not { } name) return;
        if (_classesByName.ContainsKey(name) || _enumsByName.ContainsKey(name)) return;

        if (type is XsdSimpleType simple)
        {
            if (!simple.IsEnumeration) return;   // Non-enum simple types collapse into their base.

            string enumName = CsharpNaming.Unique(CsharpNaming.TypeName(name.LocalName), _takenTypeNames);
            var @enum = new CsEnum
            {
                Name = enumName,
                XmlName = name,
                Members = BuildEnumMembers(simple),
                Documentation = simple.Documentation,
            };
            _enumsByName[name] = @enum;
            _enumsByType[type] = @enum;
            _model.Enums.Add(@enum);
            return;
        }

        var complex = (XsdComplexType)type;
        string className = CsharpNaming.Unique(CsharpNaming.TypeName(name.LocalName), _takenTypeNames);
        var @class = new CsClass
        {
            Name = className,
            CsNamespace = _csNamespace,
            XmlName = name,
            Documentation = complex.Documentation,
        };
        _classesByName[name] = @class;
        _classesByType[type] = @class;
        _model.Classes.Add(@class);
    }

    private static List<CsEnumMember> BuildEnumMembers(XsdSimpleType simple)
    {
        var taken = new HashSet<string>(StringComparer.Ordinal);
        var members = new List<CsEnumMember>();

        foreach (var value in simple.Enumerations)
        {
            // xsd.exe strips characters that cannot appear in an identifier rather than
            // substituting them, so "Ad-hoc" becomes "Adhoc" and "Very Good" becomes "VeryGood".
            var stripped = new StringBuilder(value.Value.Length);
            foreach (char c in value.Value)
            {
                if (char.IsLetterOrDigit(c) || c == '_') stripped.Append(c);
            }
            if (stripped.Length == 0) stripped.Append("Item");
            if (char.IsDigit(stripped[0])) stripped.Insert(0, 'i');

            string name = CsharpNaming.Unique(stripped.ToString(), taken);
            members.Add(new CsEnumMember
            {
                Name = name,
                XmlValue = name == value.Value ? null : value.Value,
                Documentation = value.Documentation,
            });
        }

        return members;
    }

    // ---------------------------------------------------------------- population

    private void Populate(XsdType type)
    {
        if (type is not XsdComplexType complex) return;
        if (!_classesByType.TryGetValue(type, out var @class)) return;
        PopulateClass(@class, complex);
    }

    private void PopulateClass(CsClass @class, XsdComplexType complex, string? typeNamePrefix = null)
    {
        // Anonymous member types are named after their owner. For a message wrapper that owner is
        // the SOAP body element rather than the generated Request/Response class, so that
        // <xs:element name="anyParameters"> inside <xs:element name="UploadCRL"> yields
        // UploadCRLAnyParameters and not UploadCRLRequestAnyParameters.
        string owner = typeNamePrefix ?? @class.Name;
        int order = 0;

        if (complex.Content == ContentKind.Simple)
        {
            // <xs:simpleContent><xs:extension base="xs:string"> becomes a [XmlText] member
            // named Value, alongside the extension's attributes.
            var text = complex.SimpleContentType is { } baseName
                ? ResolveTypeReference(baseName, owner, "Value")
                : new CsTypeRef("string", TypeKind.Primitive, false, "string");

            @class.Members.Add(new CsMember
            {
                Name = "Value",
                Type = text,
                Kind = MemberKind.Text,
                Order = order++,
            });
        }
        else if (complex.Particle is { } particle)
        {
            AddParticleMembers(@class, particle, ref order, complex, owner);
        }

        if (complex.IsMixed && !@class.Members.Any(m => m.Kind is MemberKind.AnyElement or MemberKind.Text))
        {
            // Mixed content with no wildcard: the interleaved character data is all that can
            // appear, and xsd.exe surfaces it as a string array.
            @class.Members.Add(new CsMember
            {
                Name = "Text",
                Type = new CsTypeRef("string", TypeKind.Primitive, false, "string"),
                Kind = MemberKind.Text,
                IsArray = true,
                Order = order++,
            });
        }

        foreach (var attribute in complex.Attributes)
        {
            var resolved = _schema.ResolveAttribute(attribute);
            if (resolved.Use == AttributeUse.Prohibited) continue;

            var type = resolved.TypeName is { } named
                ? ResolveTypeReference(named, owner, resolved.Name.LocalName)
                : resolved.InlineType is { } inline
                    ? ResolveInlineType(inline, owner, resolved.Name.LocalName)
                    : new CsTypeRef("string", TypeKind.Primitive, false, "string");

            bool optional = resolved.Use != AttributeUse.Required;
            @class.Members.Add(new CsMember
            {
                Name = MemberName(@class, resolved.Name.LocalName),
                Type = type,
                Kind = MemberKind.Attribute,
                XmlName = resolved.Name,
                IsOptional = optional,
                NeedsSpecified = optional && type.IsValueType,
                Documentation = resolved.Documentation,
            });
        }
    }

    private void AddParticleMembers(
        CsClass @class, XsdParticle particle, ref int order, XsdComplexType declaring, string owner)
    {
        switch (particle)
        {
            case XsdSequence sequence:
                foreach (var item in sequence.Items) AddParticleMembers(@class, item, ref order, declaring, owner);
                break;

            case XsdChoice choice:
                AddChoiceMembers(@class, choice, ref order, owner);
                break;

            case XsdAnyParticle any:
                @class.Members.Add(new CsMember
                {
                    // Mixed content means character data can be interleaved with the wildcard
                    // elements, so the member has to widen from XmlElement to XmlNode to keep it.
                    Name = MemberName(@class, "Any"),
                    Type = declaring.IsMixed
                        ? new CsTypeRef("System.Xml.XmlNode", TypeKind.XmlNode, false)
                        : new CsTypeRef("System.Xml.XmlElement", TypeKind.XmlElement, false),
                    Kind = MemberKind.AnyElement,
                    IsArray = true,
                    WildcardNamespace = any.ResolvedNamespace is "##any" or "##other" or null
                        ? null
                        : any.ResolvedNamespace,
                    Order = order++,
                });
                break;

            case XsdElementParticle element:
                @class.Members.Add(BuildElementMember(@class, element, order++, owner));
                break;
        }
    }

    private CsMember BuildElementMember(CsClass @class, XsdElementParticle particle, int order, string owner)
    {
        var element = _schema.ResolveElement(particle.Element);
        // An element ref keeps the referenced declaration's type but the referring particle's
        // occurrence constraints.
        var occurs = particle.Occurs;

        // A wrapper type holding one repeating element collapses into an array member.
        QName? arrayItem = null;
        if (!occurs.IsRepeating && element.TypeName is { } wrapperName && ArrayWrapperItem(wrapperName) is { } item)
        {
            arrayItem = item.Name;
            return new CsMember
            {
                Name = MemberName(@class, element.Name.LocalName),
                Type = item.TypeName is { } itemType
                    ? ResolveTypeReference(itemType, owner, item.Name.LocalName)
                    : new CsTypeRef("string", TypeKind.Primitive, false, "string"),
                Kind = MemberKind.Element,
                XmlName = element.Name,
                IsArray = true,
                IsOptional = occurs.IsOptional,
                ArrayItem = arrayItem,
                Order = order,
                Documentation = element.Documentation,
            };
        }

        var type = element.TypeName is { } named
            ? ResolveTypeReference(named, owner, element.Name.LocalName)
            : element.InlineType is { } inline
                ? ResolveInlineType(inline, owner, element.Name.LocalName)
                : new CsTypeRef("object", TypeKind.Object, false, "anyType");

        bool array = occurs.IsRepeating;

        return new CsMember
        {
            Name = MemberName(@class, element.Name.LocalName),
            Type = type,
            Kind = MemberKind.Element,
            XmlName = element.Name,
            IsArray = array,
            IsOptional = occurs.IsOptional,
            IsNillable = element.IsNillable,
            // A missing optional value type is indistinguishable from a default one, so
            // XmlSerializer pairs it with a {Name}Specified flag. Arrays and reference types
            // express absence with null and need no flag.
            NeedsSpecified = occurs.IsOptional && type.IsValueType && !array,
            Order = order,
            Documentation = element.Documentation,
        };
    }

    private void AddChoiceMembers(CsClass @class, XsdChoice choice, ref int order, string owner)
    {
        var options = new List<CsChoiceOption>();
        bool repeating = choice.Occurs.IsRepeating;
        bool hasWildcard = false;

        foreach (var item in choice.Items)
        {
            if (item is XsdAnyParticle wildcard)
            {
                // A wildcard branch means arbitrary elements can appear alongside the named
                // ones, so the member cannot be discriminated by runtime type.
                hasWildcard = true;
                if (wildcard.Occurs.IsRepeating) repeating = true;
                options.Add(new CsChoiceOption("##any:", wildcard.ResolvedNamespace ?? "", null));
                continue;
            }

            if (item is not XsdElementParticle particle)
                throw new SchemaException($"Only element and xs:any branches are supported inside xs:choice ({@class.Name}).");

            if (particle.Occurs.IsRepeating) repeating = true;

            var element = _schema.ResolveElement(particle.Element);
            var type = element.TypeName is { } named
                ? ResolveTypeReference(named, owner, element.Name.LocalName)
                : element.InlineType is { } inline
                    ? ResolveInlineType(inline, owner, element.Name.LocalName)
                    : new CsTypeRef("object", TypeKind.Object, false, "anyType");

            options.Add(new CsChoiceOption(element.Name.LocalName, element.Name.Namespace, type));
        }

        // Runtime type is enough to tell branches apart unless a wildcard is involved or two
        // branches share a C# type; otherwise a parallel discriminator array is needed.
        var namedTypes = options.Where(o => !o.IsWildcard).Select(o => o.Type!.CsName).ToList();
        bool needsDiscriminator = hasWildcard || namedTypes.Count != namedTypes.Distinct(StringComparer.Ordinal).Count();

        // xsd.exe collapses a choice into a single loosely typed member carrying one
        // XmlElement attribute per branch: "Item" when it occurs once, "Items" when repeated.
        string memberName = MemberName(@class, repeating ? "Items" : "Item");
        string? discriminatorName = null;

        if (needsDiscriminator)
        {
            discriminatorName = memberName + "ElementName";
        }

        @class.Members.Add(new CsMember
        {
            Name = memberName,
            Type = new CsTypeRef("object", TypeKind.Object, false),
            Kind = MemberKind.Choice,
            IsArray = repeating,
            Choices = options,
            ChoiceIdentifier = discriminatorName,
            Order = order++,
        });

        if (discriminatorName is null) return;

        // The discriminator's enum is named after the choice member: Items yields ItemsChoiceType.
        string enumName = CsharpNaming.Unique(memberName + "ChoiceType", _takenTypeNames);
        var taken = new HashSet<string>(StringComparer.Ordinal);
        var discriminator = new CsEnum
        {
            Name = enumName,
            XmlName = new QName("", enumName),
            Members = options.Select(option =>
            {
                // The wildcard branch has no legal identifier, so it is mangled and the raw
                // "##any:" marker is carried on the XmlEnum attribute.
                string identifier = option.IsWildcard ? "Item" : CsharpNaming.Identifier(option.ElementName);
                identifier = CsharpNaming.Unique(identifier, taken);
                return new CsEnumMember
                {
                    Name = identifier,
                    XmlValue = identifier == option.ElementName ? null : option.ElementName,
                };
            }).ToList(),
        };
        _model.Enums.Add(discriminator);

        @class.Members.Add(new CsMember
        {
            Name = discriminatorName,
            Type = new CsTypeRef(enumName, TypeKind.Enum, true),
            Kind = MemberKind.ChoiceIdentifier,
            IsArray = repeating,
            ChoiceIdentifierEnum = discriminator,
            Order = order++,
        });
    }

    /// <summary>
    /// Picks a member name that does not collide with the class name or an existing member,
    /// which C# forbids even though XML allows it.
    /// </summary>
    private static string MemberName(CsClass owner, string localName)
    {
        string candidate = CsharpNaming.Identifier(localName);
        if (candidate == owner.Name) candidate += "1";

        while (owner.Members.Any(m => m.Name == candidate)) candidate += "1";
        return candidate;
    }

    // ---------------------------------------------------------------- type references

    private CsTypeRef ResolveTypeReference(QName name, string ownerName, string memberName, int depth = 0)
    {
        if (PrimitiveMap.Lookup(name) is { } primitive) return primitive;

        if (_shared.TryResolve(name, out var sharedType)) return sharedType;

        if (_classesByName.TryGetValue(name, out var @class))
            return new CsTypeRef(@class.Name, TypeKind.Class, false, XmlTypeName: @class.XmlName);

        if (_enumsByName.TryGetValue(name, out var @enum))
            return new CsTypeRef(@enum.Name, TypeKind.Enum, true);

        if (depth > 16) throw new SchemaException($"Type reference chain too deep at {name}.");

        // A simple type that is not an enumeration contributes no C# type of its own: it
        // collapses onto whatever its restriction bottoms out in, carrying the DataType with it.
        if (_schema.FindType(name) is XsdSimpleType simple)
        {
            if (simple.Variety == SimpleTypeVariety.List)
            {
                var item = simple.ItemType is { } itemType
                    ? ResolveTypeReference(itemType, ownerName, memberName, depth + 1)
                    : new CsTypeRef("string", TypeKind.Primitive, false, "string");
                return item with { CsName = item.CsName + "[]", IsValueType = false };
            }

            if (simple.Variety == SimpleTypeVariety.Union)
            {
                // A union has no single CLR type; XmlSerializer represents it as its lexical form.
                return new CsTypeRef("string", TypeKind.Primitive, false, "string");
            }

            if (simple.BaseType is { } baseName)
                return ResolveTypeReference(baseName, ownerName, memberName, depth + 1);

            return new CsTypeRef("string", TypeKind.Primitive, false, "string");
        }

        if (_schema.FindType(name) is XsdComplexType)
        {
            // Reachable but not declared: it was pruned, which should not happen.
            throw new SchemaException($"Complex type {name} is referenced but was not declared.");
        }

        // Unknown namespace (for example a type from a schema that was imported without a
        // location). xs:anyType is the only safe representation.
        return new CsTypeRef("object", TypeKind.Object, false, "anyType");
    }

    private CsTypeRef ResolveInlineType(XsdType inline, string ownerName, string memberName)
    {
        // The member component is capitalised when it becomes part of a type name, even though
        // the member itself keeps the schema's casing.
        string typeSuffix = memberName.Length == 0
            ? memberName
            : char.ToUpperInvariant(memberName[0]) + memberName.Substring(1);

        if (inline is XsdSimpleType simple)
        {
            if (simple.IsEnumeration)
            {
                if (_enumsByType.TryGetValue(inline, out var existing))
                    return new CsTypeRef(existing.Name, TypeKind.Enum, true);

                // xsd.exe names an inline type after the member that declares it, prefixed by
                // the owning type: element ErrorCode inside BaseFaultType becomes BaseFaultTypeErrorCode.
                string name = CsharpNaming.Unique(
                    CsharpNaming.TypeName(ownerName + typeSuffix), _takenTypeNames);
                var @enum = new CsEnum
                {
                    Name = name,
                    XmlName = new QName("", name),
                    Members = BuildEnumMembers(simple),
                    Documentation = simple.Documentation,
                };
                _enumsByType[inline] = @enum;
                _model.Enums.Add(@enum);
                return new CsTypeRef(name, TypeKind.Enum, true);
            }

            if (simple.Variety == SimpleTypeVariety.List)
            {
                var item = simple.ItemType is { } itemType
                    ? ResolveTypeReference(itemType, ownerName, memberName)
                    : new CsTypeRef("string", TypeKind.Primitive, false, "string");
                return item with { CsName = item.CsName + "[]", IsValueType = false };
            }

            return simple.BaseType is { } baseName
                ? ResolveTypeReference(baseName, ownerName, memberName)
                : new CsTypeRef("string", TypeKind.Primitive, false, "string");
        }

        var complex = (XsdComplexType)inline;

        // A wrapper whose entire content is one xs:any carries nothing of its own, so the member
        // becomes the wildcard element directly rather than a class with a single Any property.
        // wsnt:NotificationMessageHolderType/Message is the important case: event handling code
        // reads message.Message.InnerXml, and a generated wrapper class would break that.
        if (IsSingleWildcardWrapper(complex))
            return new CsTypeRef("System.Xml.XmlElement", TypeKind.XmlElement, false);

        if (_classesByType.TryGetValue(inline, out var declared))
            return new CsTypeRef(declared.Name, TypeKind.Class, false, XmlTypeName: declared.XmlName);

        string className = CsharpNaming.Unique(
            CsharpNaming.TypeName(ownerName + typeSuffix), _takenTypeNames);
        var @class = new CsClass
        {
            Name = className,
            CsNamespace = _csNamespace,
            XmlName = null,
            Documentation = complex.Documentation,
        };
        _classesByType[inline] = @class;
        _model.Classes.Add(@class);
        PopulateClass(@class, complex);

        if (complex.BaseType is { } inlineBase && _classesByName.TryGetValue(inlineBase, out var parent))
        {
            @class.BaseClass = parent;
            parent.DerivedClasses.Add(@class);
        }

        return new CsTypeRef(className, TypeKind.Class, false);
    }

    /// <summary>
    /// The repeated element of a wrapper type whose only content is one repeating element, or
    /// null when the type is not such a wrapper.
    /// </summary>
    private XsdElement? ArrayWrapperItem(QName typeName)
    {
        if (_schema.FindType(typeName) is not XsdComplexType complex) return null;

        // A wrapper that can carry attributes is not purely a container, so it keeps its class.
        // This is what separates tt:IntItems, which collapses to int[], from
        // tt:AudioEncoderConfigurationOptions, which declares xs:anyAttribute and does not.
        if (complex.BaseType is not null || complex.Attributes.Count > 0) return null;
        if (complex.AllowsAnyAttribute || complex.IsMixed) return null;
        if (complex.Particle is not XsdSequence sequence || sequence.Items.Count != 1) return null;
        if (sequence.Items[0] is not XsdElementParticle particle || !particle.Occurs.IsRepeating) return null;

        var element = _schema.ResolveElement(particle.Element);
        return element.InlineType is null ? element : null;
    }

    /// <summary>
    /// True for a complex type whose only content is a single, non-repeating xs:any wildcard and
    /// which declares no attributes or base type.
    /// </summary>
    private static bool IsSingleWildcardWrapper(XsdComplexType complex)
    {
        if (complex.BaseType is not null || complex.Attributes.Count > 0) return false;
        if (complex.AllowsAnyAttribute || complex.IsMixed) return false;
        if (complex.Particle is not XsdSequence sequence || sequence.Items.Count != 1) return false;

        return sequence.Items[0] is XsdAnyParticle wildcard && !wildcard.Occurs.IsRepeating;
    }

    // ---------------------------------------------------------------- inheritance

    private void LinkInheritance()
    {
        foreach (var (name, @class) in _classesByName)
        {
            if (_schema.FindType(name) is not XsdComplexType complex) continue;
            if (complex.BaseType is not { } baseName) continue;
            if (!_classesByName.TryGetValue(baseName, out var parent)) continue;

            @class.BaseClass = parent;
            if (!parent.DerivedClasses.Contains(@class)) parent.DerivedClasses.Add(@class);
        }

        foreach (var @class in _model.Classes)
            @class.DerivedClasses.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
    }

    // ---------------------------------------------------------------- services

    private void BuildServices()
    {
        foreach (var portType in _wsdls.SelectMany(w => w.BoundPortTypes).OrderBy(p => p.Name.LocalName, StringComparer.Ordinal))
        {
            var operations = new List<CsOperation>();

            foreach (var operation in portType.Operations)
            {
                var request = BuildWrapper(operation, operation.Input, operation.Name + "Request");
                var response = BuildWrapper(operation, operation.Output, operation.Name + "Response");

                operations.Add(new CsOperation
                {
                    Name = operation.Name,
                    SoapAction = operation.SoapAction
                        ?? throw new SchemaException($"Operation {portType.Name}/{operation.Name} has no SOAP action."),
                    Request = request,
                    Response = response,
                    IsOneWay = operation.Output is null,
                    Documentation = operation.Documentation,
                });
            }

            _model.Services.Add(new CsService
            {
                Name = portType.Name.LocalName,
                XmlName = portType.Name,
                Operations = operations,
                Documentation = portType.Documentation,
            });
        }
    }

    /// <summary>
    /// Builds the request or response wrapper class for an operation by flattening the SOAP body
    /// element. The wrapper is always generated, even when empty, so both call styles exist.
    /// </summary>
    private CsClass BuildWrapper(WsdlOperation operation, QName? messageName, string className)
    {
        QName? bodyElement = null;
        XsdComplexType? body = null;

        if (messageName is { } name && FindMessage(name) is { } message)
        {
            var part = message.Parts.FirstOrDefault();
            if (part?.Element is { } elementName && _schema.FindElement(elementName) is { } element)
            {
                bodyElement = element.Name;
                body = element.TypeName is { } typeName
                    ? _schema.FindType(typeName) as XsdComplexType
                    : element.InlineType as XsdComplexType;
            }
        }

        // Several portTypes can declare the same operation: the WS-Notification subscription
        // manager and its pausable variant both have Renew and Unsubscribe, and Notify appears on
        // both the consumer and the pull point. They share one SOAP body, so they share one
        // wrapper rather than generating the class twice.
        if (_wrappers.TryGetValue(className, out var existing))
        {
            if (existing.WrapperElement == bodyElement) return existing;

            // Same operation name, different body: give this one a distinct class.
            className = CsharpNaming.Unique(className, _takenTypeNames);
        }

        var @class = new CsClass
        {
            Name = className,
            CsNamespace = _csNamespace,
            XmlName = null,
            IsMessageWrapper = true,
            WrapperElement = bodyElement,
            Documentation = operation.Documentation,
        };

        if (body is not null) PopulateClass(@class, body, bodyElement?.LocalName);

        _wrappers[className] = @class;
        _model.Classes.Add(@class);
        return @class;
    }
}
