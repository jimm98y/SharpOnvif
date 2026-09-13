using WsdlGenerator.Xml;

namespace WsdlGenerator.Xsd;

/// <summary>How often a particle may appear. <see cref="Max"/> is -1 for unbounded.</summary>
internal readonly record struct Occurs(int Min, int Max)
{
    public const int Unbounded = -1;
    public static readonly Occurs One = new(1, 1);

    public bool IsOptional => Min == 0;
    public bool IsRepeating => Max is Unbounded or > 1;
}

internal abstract class XsdType
{
    /// <summary>Null for types declared inline on an element.</summary>
    public QName? Name { get; init; }
    public string? Documentation { get; init; }

    /// <summary>Set for anonymous types: the element that declared them, used to derive a C# name.</summary>
    public string? AnonymousFor { get; init; }

    public override string ToString() => Name?.ToString() ?? $"(anonymous for {AnonymousFor})";
}

internal enum SimpleTypeVariety { Atomic, List, Union }

internal sealed class XsdSimpleType : XsdType
{
    public SimpleTypeVariety Variety { get; init; } = SimpleTypeVariety.Atomic;

    /// <summary>The restriction base, or null when this restricts an anonymous type.</summary>
    public QName? BaseType { get; init; }

    /// <summary>Non-empty when the restriction enumerates its legal values, which becomes a C# enum.</summary>
    public IReadOnlyList<XsdEnumValue> Enumerations { get; init; } = [];

    /// <summary>Item type for <see cref="SimpleTypeVariety.List"/>, which maps to a C# array.</summary>
    public QName? ItemType { get; init; }

    /// <summary>Member types for <see cref="SimpleTypeVariety.Union"/>.</summary>
    public IReadOnlyList<QName> MemberTypes { get; init; } = [];

    public bool IsEnumeration => Enumerations.Count > 0;
}

internal sealed record XsdEnumValue(string Value, string? Documentation);

internal enum ContentKind
{
    /// <summary>No child elements and no text: attributes only.</summary>
    Empty,
    /// <summary>Child elements, optionally with attributes.</summary>
    Elements,
    /// <summary>Text content of a simple type, plus attributes.</summary>
    Simple,
}

internal sealed class XsdComplexType : XsdType
{
    /// <summary>Base type of a complexContent or simpleContent extension, else null.</summary>
    public QName? BaseType { get; init; }

    public ContentKind Content { get; init; } = ContentKind.Elements;

    /// <summary>For <see cref="ContentKind.Simple"/>, the type of the element's text.</summary>
    public QName? SimpleContentType { get; init; }

    /// <summary>Content model contributed by this type, excluding anything inherited.</summary>
    public XsdParticle? Particle { get; init; }

    public IReadOnlyList<XsdAttribute> Attributes { get; init; } = [];

    /// <summary>
    /// True when the type declares xs:anyAttribute. Recorded for fidelity but not emitted:
    /// svcutil drops these, and matching it keeps the generated surface source-compatible.
    /// </summary>
    public bool AllowsAnyAttribute { get; init; }

    /// <summary>
    /// Mixed content, meaning character data may appear between child elements. Only the OASIS
    /// WS-Notification topic and query expression types use this. A mixed type's wildcard member
    /// widens from XmlElement[] to XmlNode[] so the interleaved text survives, which is what
    /// callers read when they pull a topic string out of a notification.
    /// </summary>
    public bool IsMixed { get; init; }

    /// <summary>
    /// Declared abstract. Emitted as an ordinary class anyway: the schema only forbids using it
    /// as an instance type, and generating it abstract would break deserialisation of derived types.
    /// </summary>
    public bool IsAbstract { get; init; }
}

internal abstract class XsdParticle
{
    public Occurs Occurs { get; init; } = Occurs.One;
}

internal sealed class XsdSequence : XsdParticle
{
    public IReadOnlyList<XsdParticle> Items { get; init; } = [];
}

internal sealed class XsdChoice : XsdParticle
{
    public IReadOnlyList<XsdParticle> Items { get; init; } = [];
}

internal sealed class XsdElementParticle : XsdParticle
{
    public required XsdElement Element { get; init; }
}

/// <summary>An xs:any wildcard, which becomes an <c>XmlElement[]</c> member.</summary>
internal sealed class XsdAnyParticle : XsdParticle
{
    /// <summary>The raw namespace constraint: "##any", "##targetNamespace", "##other", or a list.</summary>
    public string? Namespace { get; init; }

    /// <summary>Resolved target namespace, so "##targetNamespace" can be emitted as a literal.</summary>
    public string? ResolvedNamespace { get; init; }
}

internal sealed class XsdElement
{
    public required QName Name { get; init; }

    /// <summary>Named type, or null when <see cref="InlineType"/> carries an anonymous one.</summary>
    public QName? TypeName { get; init; }
    public XsdType? InlineType { get; init; }

    /// <summary>Set when this is an xs:element ref="..." to a global element.</summary>
    public QName? Ref { get; init; }

    public Occurs Occurs { get; init; } = Occurs.One;
    public bool IsNillable { get; init; }
    public string? Default { get; init; }
    public string? Fixed { get; init; }
    public string? Documentation { get; init; }

    /// <summary>True for a global (top-level) element declaration, which can be a message part.</summary>
    public bool IsGlobal { get; init; }

    public override string ToString() => Name.ToString();
}

internal enum AttributeUse { Optional, Required, Prohibited }

internal sealed class XsdAttribute
{
    public required QName Name { get; init; }
    public QName? TypeName { get; init; }
    public XsdSimpleType? InlineType { get; init; }
    public QName? Ref { get; init; }
    public AttributeUse Use { get; init; } = AttributeUse.Optional;
    public string? Default { get; init; }
    public string? Fixed { get; init; }
    public string? Documentation { get; init; }

    public override string ToString() => Name.ToString();
}
