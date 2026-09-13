using WsdlGenerator.Xml;

namespace WsdlGenerator.Binding;

/// <summary>How a generated member maps onto XML.</summary>
internal enum MemberKind
{
    /// <summary>A child element.</summary>
    Element,
    /// <summary>An XML attribute.</summary>
    Attribute,
    /// <summary>An xs:any wildcard, surfaced as XmlElement[] (or XmlNode[] for mixed content).</summary>
    AnyElement,
    /// <summary>Character content of a simple-content or mixed-content type.</summary>
    Text,
    /// <summary>An xs:choice, surfaced as object or object[] with one branch per option.</summary>
    Choice,
    /// <summary>
    /// The parallel discriminator array of a choice that needs one, telling the serializer which
    /// branch each element of the choice member came from.
    /// </summary>
    ChoiceIdentifier,
}

/// <summary>What a member's C# type fundamentally is, which decides how it is read and written.</summary>
internal enum TypeKind
{
    /// <summary>A built-in mapped from an XSD primitive: string, int, DateTime, byte[], ...</summary>
    Primitive,
    /// <summary>A generated enum.</summary>
    Enum,
    /// <summary>A generated class.</summary>
    Class,
    /// <summary>System.Xml.XmlElement.</summary>
    XmlElement,
    /// <summary>System.Xml.XmlNode, used where mixed content means text can appear too.</summary>
    XmlNode,
    /// <summary>System.Object, used for xs:anyType and for choice members.</summary>
    Object,
}

/// <summary>A reference to a C# type, with everything the serializer emitter needs about it.</summary>
internal sealed record CsTypeRef(
    string CsName,
    TypeKind Kind,
    bool IsValueType,
    /// <summary>The XSD primitive local name, for primitives. Drives parse/format selection.</summary>
    string? XsdPrimitive = null,
    /// <summary>Value for XmlElement(DataType=...), when XmlSerializer needs it to round-trip.</summary>
    string? DataType = null,
    /// <summary>
    /// Schema type this refers to, for a generated class. Carried here rather than looked up by
    /// C# name, because a reference may point at the shared assembly's namespace.
    /// </summary>
    QName? XmlTypeName = null)
{
    public override string ToString() => CsName;
}

/// <summary>
/// One branch of an xs:choice. A branch with a null <paramref name="Type"/> is the xs:any
/// wildcard, which carries raw XML rather than a generated type.
/// </summary>
internal sealed record CsChoiceOption(string ElementName, string Namespace, CsTypeRef? Type)
{
    public bool IsWildcard => Type is null;
}

internal sealed class CsMember
{
    public required string Name { get; init; }
    public required CsTypeRef Type { get; init; }
    public required MemberKind Kind { get; init; }

    /// <summary>
    /// Element or attribute name on the wire. Empty namespace means unqualified. Members that do
    /// not correspond to a named node (character content, a choice) leave it empty rather than
    /// default, so the halves are never null.
    /// </summary>
    public QName XmlName { get; init; } = new QName("", "");

    /// <summary>True when the member is a collection, which is emitted as an array.</summary>
    public bool IsArray { get; init; }

    /// <summary>
    /// True when the member needs a companion <c>{Name}Specified</c> flag: an optional element
    /// whose C# type is a value type cannot otherwise express absence.
    /// </summary>
    public bool NeedsSpecified { get; init; }

    public bool IsOptional { get; init; }
    public bool IsNillable { get; init; }

    /// <summary>Declaration order within the sequence, which the wire format depends on.</summary>
    public int Order { get; init; }

    /// <summary>Branches, for <see cref="MemberKind.Choice"/>.</summary>
    public IReadOnlyList<CsChoiceOption> Choices { get; init; } = [];

    /// <summary>Namespace constraint of an xs:any wildcard, for XmlAnyElement(Namespace=...).</summary>
    public string? WildcardNamespace { get; init; }

    /// <summary>
    /// True when this wildcard also receives the character data interleaved with its elements.
    /// A mixed-content type keeps both in one member, which is why its element type widens from
    /// XmlElement to XmlNode - the topic of a notification is character data, and dropping it
    /// leaves the member empty.
    /// </summary>
    public bool CapturesText { get; init; }

    /// <summary>
    /// Name of the repeated child element, when the member's schema type is a wrapper whose only
    /// content is one repeating element. Such a wrapper contributes nothing of its own, so the
    /// member becomes a plain array and the wrapper survives only as the enclosing element:
    /// tt:IntItems yields <c>int[] DegreeList</c> over
    /// <c>&lt;DegreeList&gt;&lt;Items&gt;90&lt;/Items&gt;&lt;/DegreeList&gt;</c>.
    /// </summary>
    public QName? ArrayItem { get; init; }

    /// <summary>
    /// Name of the companion discriminator member, for a choice whose branches cannot be told
    /// apart by runtime type alone.
    /// </summary>
    public string? ChoiceIdentifier { get; init; }

    /// <summary>The discriminator enum, for a <see cref="MemberKind.ChoiceIdentifier"/> member.</summary>
    public CsEnum? ChoiceIdentifierEnum { get; init; }

    public string? Documentation { get; init; }

    /// <summary>Backing field name, e.g. "tokenField" for a member named "token".</summary>
    public string FieldName => char.ToLowerInvariant(Name[0]) + Name.Substring(1) + "Field";

    public override string ToString() => $"{Type} {Name}";
}

internal sealed class CsEnumMember
{
    public required string Name { get; init; }
    /// <summary>Set when the C# name had to be mangled and XmlEnum must carry the wire value.</summary>
    public string? XmlValue { get; init; }
    public string? Documentation { get; init; }
}

internal sealed class CsEnum
{
    public required string Name { get; init; }
    public required QName XmlName { get; init; }
    public IReadOnlyList<CsEnumMember> Members { get; init; } = [];
    public string? Documentation { get; init; }

    public override string ToString() => Name;
}

internal sealed class CsClass
{
    public required string Name { get; init; }

    /// <summary>
    /// C# namespace the class is generated into. Types from the schemas services share are
    /// generated once into the common assembly, so a service's class may extend one from a
    /// different namespace and has to name it in full.
    /// </summary>
    public string? CsNamespace { get; init; }

    /// <summary>The name to use when referring to this class from <paramref name="from"/>.</summary>
    public string NameFrom(string from) =>
        CsNamespace is null || CsNamespace == from ? Name : CsNamespace + "." + Name;

    /// <summary>Schema type name, used for xsi:type. Null for message wrappers.</summary>
    public QName? XmlName { get; init; }

    public CsClass? BaseClass { get; set; }
    public List<CsMember> Members { get; } = [];

    /// <summary>Classes that extend this one, in generation order. Drives xsi:type dispatch.</summary>
    public List<CsClass> DerivedClasses { get; } = [];

    /// <summary>True for the generated request/response wrappers of an operation.</summary>
    public bool IsMessageWrapper { get; init; }

    /// <summary>Element name this wrapper serialises as, for message wrappers.</summary>
    public QName? WrapperElement { get; init; }

    public string? Documentation { get; init; }

    /// <summary>This class's members plus every inherited one, base-first (schema sequence order).</summary>
    public IEnumerable<CsMember> AllMembers =>
        BaseClass is null ? Members : BaseClass.AllMembers.Concat(Members);

    public override string ToString() => Name;
}

internal sealed class CsOperation
{
    public required string Name { get; init; }
    public required string SoapAction { get; init; }

    public required CsClass Request { get; init; }
    public required CsClass Response { get; init; }

    /// <summary>True for a one-way operation with no wsdl:output.</summary>
    public bool IsOneWay { get; init; }

    public string? Documentation { get; init; }

    public IReadOnlyList<CsMember> Input => Request.Members;
    public IReadOnlyList<CsMember> Output => Response.Members;

    public override string ToString() => Name;
}

internal sealed class CsService
{
    public required string Name { get; init; }
    public required QName XmlName { get; init; }
    public IReadOnlyList<CsOperation> Operations { get; init; } = [];
    public string? Documentation { get; init; }

    public override string ToString() => Name;
}

/// <summary>Everything generated into one pair of client/server assemblies.</summary>
internal sealed class CsModel
{
    public required string ServiceName { get; init; }

    /// <summary>C# namespace this model is generated into.</summary>
    public string CsNamespace { get; init; } = "";
    public List<CsEnum> Enums { get; } = [];
    public List<CsClass> Classes { get; } = [];
    public List<CsService> Services { get; } = [];
}
