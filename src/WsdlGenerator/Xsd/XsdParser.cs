using System.Xml.Linq;
using WsdlGenerator.Xml;

namespace WsdlGenerator.Xsd;

/// <summary>
/// Parses the subset of XML Schema that the ONVIF specifications actually use. Constructs that
/// never appear in any mirrored document (substitution groups, xs:group, xs:all, complex content
/// restriction, redefine) are rejected loudly rather than ignored, so a future specification
/// revision that starts using them fails the build instead of silently producing wrong code.
/// </summary>
internal sealed class XsdParser
{
    private static readonly XNamespace Xs = Ns.Xsd;

    private readonly DocumentResolver _resolver;
    private readonly XsdSchemaSet _set;
    private readonly HashSet<string> _parsed = new(StringComparer.Ordinal);
    private readonly Dictionary<QName, XElement> _attributeGroups = [];

    public XsdParser(DocumentResolver resolver, XsdSchemaSet set)
    {
        _resolver = resolver;
        _set = set;
    }

    /// <summary>
    /// Parses an xs:schema element and everything it imports or includes. <paramref name="documentUrl"/>
    /// is the absolute URL the element came from, used to resolve relative schemaLocation values and
    /// to avoid parsing a shared schema such as onvif.xsd once per referencing service.
    /// </summary>
    public void ParseSchema(XElement schema, string documentUrl)
    {
        string targetNamespace = schema.Attribute("targetNamespace")?.Value ?? "";

        // Inline schemas share their document URL with sibling schemas inside the same WSDL,
        // so identity has to include the target namespace as well.
        string key = documentUrl + "#" + targetNamespace;
        if (!_parsed.Add(key)) return;

        foreach (var child in schema.Elements())
        {
            if (child.Name == Xs + "import" || child.Name == Xs + "include")
            {
                ParseReferenced(child, documentUrl, targetNamespace);
            }
            else if (child.Name == Xs + "element")
            {
                _set.AddElement(ParseElement(child, targetNamespace, isGlobal: true));
            }
            else if (child.Name == Xs + "complexType")
            {
                _set.AddType(ParseComplexType(child, targetNamespace, name: RequiredName(child, targetNamespace)));
            }
            else if (child.Name == Xs + "simpleType")
            {
                _set.AddType(ParseSimpleType(child, targetNamespace, name: RequiredName(child, targetNamespace)));
            }
            else if (child.Name == Xs + "attribute")
            {
                _set.AddAttribute(ParseAttribute(child, targetNamespace));
            }
            else if (child.Name == Xs + "annotation")
            {
                // Documentation only.
            }
            else if (child.Name == Xs + "attributeGroup")
            {
                // Only xml.xsd declares one (specialAttrs), and nothing in the mirrored schemas
                // currently references it, but resolving refs is cheap insurance.
                _attributeGroups[RequiredName(child, targetNamespace)] = child;
            }
            else if (child.Name == Xs + "group" || child.Name == Xs + "redefine" || child.Name == Xs + "notation")
            {
                throw new SchemaException(child,
                    $"xs:{child.Name.LocalName} is not modelled by this generator, and no mirrored " +
                    "ONVIF schema used it when the generator was written. Add support before regenerating.");
            }
            else
            {
                throw new SchemaException(child, $"Unexpected schema-level element '{child.Name}'.");
            }
        }
    }

    private void ParseReferenced(XElement reference, string documentUrl, string targetNamespace)
    {
        string? location = reference.Attribute("schemaLocation")?.Value;
        if (location is null)
        {
            // An import with no location just declares that the namespace is expected to be
            // available; the referenced schema arrives through some other document.
            return;
        }

        string url = _resolver.Combine(location, documentUrl);
        var document = _resolver.Load(url);

        // The referenced document may be a bare schema or a WSDL carrying inline schemas.
        foreach (var schema in document.Descendants(Xs + "schema"))
        {
            // xs:include pulls definitions into the including namespace; xs:import keeps its own.
            if (reference.Name == Xs + "include" && schema.Attribute("targetNamespace") is null)
            {
                var chameleon = new XElement(schema);
                chameleon.SetAttributeValue("targetNamespace", targetNamespace);
                ParseSchema(chameleon, url);
            }
            else
            {
                ParseSchema(schema, url);
            }
        }
    }

    private static QName RequiredName(XElement element, string targetNamespace)
    {
        string name = element.Attribute("name")?.Value
            ?? throw new SchemaException(element, $"Top-level xs:{element.Name.LocalName} must have a name.");
        return new QName(targetNamespace, name);
    }

    private XsdElement ParseElement(XElement element, string targetNamespace, bool isGlobal)
    {
        if (element.Attribute("substitutionGroup") is not null)
            throw new SchemaException(element, "Substitution groups are not modelled by this generator.");
        if (element.Attribute("abstract")?.Value == "true")
            throw new SchemaException(element, "Abstract element declarations are not modelled by this generator.");

        var reference = element.Attribute("ref")?.Value;
        if (reference is not null)
        {
            var target = QName.Parse(reference, element);
            return new XsdElement
            {
                Name = target,
                Ref = target,
                Occurs = ParseOccurs(element),
                Documentation = Documentation(element),
                IsGlobal = false,
            };
        }

        string local = element.Attribute("name")?.Value
            ?? throw new SchemaException(element, "xs:element must have either name or ref.");

        // Only globally declared elements are namespace-qualified unless the schema sets
        // elementFormDefault="qualified", which every ONVIF schema does.
        var name = new QName(isGlobal || IsElementFormQualified(element) ? targetNamespace : "", local);

        var typeAttribute = element.Attribute("type")?.Value;
        XsdType? inline = null;
        if (typeAttribute is null)
        {
            var complex = element.Element(Xs + "complexType");
            var simple = element.Element(Xs + "simpleType");
            if (complex is not null) inline = ParseComplexType(complex, targetNamespace, name: null, anonymousFor: local);
            else if (simple is not null) inline = ParseSimpleType(simple, targetNamespace, name: null, anonymousFor: local);
            // An element with neither type nor inline definition is xs:anyType.
        }

        return new XsdElement
        {
            Name = name,
            TypeName = typeAttribute is null ? null : QName.Parse(typeAttribute, element),
            InlineType = inline,
            Occurs = ParseOccurs(element),
            IsNillable = element.Attribute("nillable")?.Value == "true",
            Default = element.Attribute("default")?.Value,
            Fixed = element.Attribute("fixed")?.Value,
            Documentation = Documentation(element),
            IsGlobal = isGlobal,
        };
    }

    private static bool IsElementFormQualified(XElement element)
    {
        var schema = element.AncestorsAndSelf(Xs + "schema").FirstOrDefault();
        return schema?.Attribute("elementFormDefault")?.Value == "qualified";
    }

    private XsdAttribute ParseAttribute(XElement attribute, string targetNamespace)
    {
        var reference = attribute.Attribute("ref")?.Value;
        if (reference is not null)
        {
            var target = QName.Parse(reference, attribute);
            return new XsdAttribute
            {
                Name = target,
                Ref = target,
                Use = ParseUse(attribute),
                Documentation = Documentation(attribute),
            };
        }

        string local = attribute.Attribute("name")?.Value
            ?? throw new SchemaException(attribute, "xs:attribute must have either name or ref.");

        // Attributes are unqualified unless attributeFormDefault says otherwise, which no
        // ONVIF schema sets, so locally declared attributes carry no namespace.
        var schema = attribute.AncestorsAndSelf(Xs + "schema").FirstOrDefault();
        bool qualified = schema?.Attribute("attributeFormDefault")?.Value == "qualified";
        bool isGlobal = attribute.Parent?.Name == Xs + "schema";

        var inlineSimple = attribute.Element(Xs + "simpleType");

        return new XsdAttribute
        {
            Name = new QName(isGlobal || qualified ? targetNamespace : "", local),
            TypeName = attribute.Attribute("type") is { } t ? QName.Parse(t.Value, attribute) : null,
            InlineType = inlineSimple is null ? null : ParseSimpleType(inlineSimple, targetNamespace, null, local),
            Use = ParseUse(attribute),
            Default = attribute.Attribute("default")?.Value,
            Fixed = attribute.Attribute("fixed")?.Value,
            Documentation = Documentation(attribute),
        };
    }

    private static AttributeUse ParseUse(XElement attribute) => attribute.Attribute("use")?.Value switch
    {
        "required" => AttributeUse.Required,
        "prohibited" => AttributeUse.Prohibited,
        _ => AttributeUse.Optional,
    };

    private XsdSimpleType ParseSimpleType(XElement simple, string targetNamespace, QName? name, string? anonymousFor = null)
    {
        var restriction = simple.Element(Xs + "restriction");
        var list = simple.Element(Xs + "list");
        var union = simple.Element(Xs + "union");

        if (restriction is not null)
        {
            var enumerations = restriction.Elements(Xs + "enumeration")
                .Select(e => new XsdEnumValue(
                    e.Attribute("value")?.Value ?? throw new SchemaException(e, "xs:enumeration requires a value."),
                    Documentation(e)))
                .ToList();

            return new XsdSimpleType
            {
                Name = name,
                AnonymousFor = anonymousFor,
                BaseType = restriction.Attribute("base") is { } b ? QName.Parse(b.Value, restriction) : null,
                Enumerations = enumerations,
                Documentation = Documentation(simple),
            };
        }

        if (list is not null)
        {
            return new XsdSimpleType
            {
                Name = name,
                AnonymousFor = anonymousFor,
                Variety = SimpleTypeVariety.List,
                ItemType = list.Attribute("itemType") is { } i ? QName.Parse(i.Value, list) : null,
                Documentation = Documentation(simple),
            };
        }

        if (union is not null)
        {
            var members = (union.Attribute("memberTypes")?.Value ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(m => QName.Parse(m, union))
                .ToList();

            return new XsdSimpleType
            {
                Name = name,
                AnonymousFor = anonymousFor,
                Variety = SimpleTypeVariety.Union,
                MemberTypes = members,
                Documentation = Documentation(simple),
            };
        }

        throw new SchemaException(simple, "xs:simpleType must contain a restriction, list, or union.");
    }

    private XsdComplexType ParseComplexType(XElement complex, string targetNamespace, QName? name, string? anonymousFor = null)
    {
        bool isAbstract = complex.Attribute("abstract")?.Value == "true";
        var simpleContent = complex.Element(Xs + "simpleContent");
        if (simpleContent is not null)
            return ParseSimpleContent(simpleContent, complex, targetNamespace, name, anonymousFor);

        var complexContent = complex.Element(Xs + "complexContent");
        XElement body = complex;
        QName? baseType = null;
        bool isMixed = complex.Attribute("mixed")?.Value == "true";

        if (complexContent is not null)
        {
            // mixed may be declared on either the complexType or the complexContent wrapper.
            isMixed |= complexContent.Attribute("mixed")?.Value == "true";

            var extension = complexContent.Element(Xs + "extension");
            var restriction = complexContent.Element(Xs + "restriction");
            if (restriction is not null)
                throw new SchemaException(restriction, "Complex content restriction is not modelled by this generator.");
            if (extension is null)
                throw new SchemaException(complexContent, "xs:complexContent requires an extension.");

            baseType = QName.Parse(
                extension.Attribute("base")?.Value ?? throw new SchemaException(extension, "xs:extension requires a base."),
                extension);

            // Extending xs:anyType adds nothing, and carrying it as a base would send the
            // emitter looking for a class that does not exist.
            if (baseType == new QName(Ns.Xsd, "anyType")) baseType = null;

            body = extension;
        }

        var particle = ParseContentModel(body, targetNamespace);
        var attributes = CollectAttributes(body, targetNamespace);

        return new XsdComplexType
        {
            Name = name,
            AnonymousFor = anonymousFor,
            BaseType = baseType,
            Content = particle is null && !isMixed && attributes.Count > 0 ? ContentKind.Empty : ContentKind.Elements,
            Particle = particle,
            Attributes = attributes,
            AllowsAnyAttribute = body.Element(Xs + "anyAttribute") is not null,
            IsMixed = isMixed,
            IsAbstract = isAbstract,
            Documentation = Documentation(complex),
        };
    }

    /// <summary>
    /// Reads the attributes declared directly under <paramref name="body"/>, expanding any
    /// xs:attributeGroup references into their members.
    /// </summary>
    private List<XsdAttribute> CollectAttributes(XElement body, string targetNamespace)
    {
        var attributes = new List<XsdAttribute>();

        foreach (var attribute in body.Elements(Xs + "attribute"))
            attributes.Add(ParseAttribute(attribute, targetNamespace));

        foreach (var groupRef in body.Elements(Xs + "attributeGroup"))
        {
            string? reference = groupRef.Attribute("ref")?.Value;
            if (reference is null) continue;

            var target = QName.Parse(reference, groupRef);
            if (!_attributeGroups.TryGetValue(target, out var group))
                throw new SchemaException(groupRef, $"Unresolved attribute group ref {target}.");

            string groupNamespace = group.AncestorsAndSelf(Xs + "schema").FirstOrDefault()
                ?.Attribute("targetNamespace")?.Value ?? "";
            attributes.AddRange(CollectAttributes(group, groupNamespace));
        }

        return attributes;
    }

    private XsdComplexType ParseSimpleContent(
        XElement simpleContent, XElement complex, string targetNamespace, QName? name, string? anonymousFor)
    {
        var extension = simpleContent.Element(Xs + "extension");
        var restriction = simpleContent.Element(Xs + "restriction");
        var body = extension ?? restriction
            ?? throw new SchemaException(simpleContent, "xs:simpleContent requires an extension or restriction.");

        var baseName = QName.Parse(
            body.Attribute("base")?.Value ?? throw new SchemaException(body, "Simple content requires a base."),
            body);

        return new XsdComplexType
        {
            Name = name,
            AnonymousFor = anonymousFor,
            Content = ContentKind.Simple,
            SimpleContentType = baseName,
            Attributes = CollectAttributes(body, targetNamespace),
            AllowsAnyAttribute = body.Element(Xs + "anyAttribute") is not null,
            Documentation = Documentation(complex),
        };
    }

    /// <summary>Reads the sequence/choice/any content model directly under <paramref name="parent"/>.</summary>
    private XsdParticle? ParseContentModel(XElement parent, string targetNamespace)
    {
        var sequence = parent.Element(Xs + "sequence");
        if (sequence is not null) return ParseSequence(sequence, targetNamespace);

        var choice = parent.Element(Xs + "choice");
        if (choice is not null) return ParseChoice(choice, targetNamespace);

        if (parent.Element(Xs + "all") is { } all)
            throw new SchemaException(all, "xs:all is not modelled by this generator.");
        if (parent.Element(Xs + "group") is { } group)
            throw new SchemaException(group, "xs:group is not modelled by this generator.");

        return null;
    }

    private XsdSequence ParseSequence(XElement sequence, string targetNamespace) => new()
    {
        Occurs = ParseOccurs(sequence),
        Items = ParseParticles(sequence, targetNamespace),
    };

    private XsdChoice ParseChoice(XElement choice, string targetNamespace) => new()
    {
        Occurs = ParseOccurs(choice),
        Items = ParseParticles(choice, targetNamespace),
    };

    private List<XsdParticle> ParseParticles(XElement container, string targetNamespace)
    {
        var items = new List<XsdParticle>();
        foreach (var child in container.Elements())
        {
            if (child.Name == Xs + "element")
            {
                var element = ParseElement(child, targetNamespace, isGlobal: false);
                items.Add(new XsdElementParticle { Element = element, Occurs = element.Occurs });
            }
            else if (child.Name == Xs + "any")
            {
                string? constraint = child.Attribute("namespace")?.Value;
                items.Add(new XsdAnyParticle
                {
                    Occurs = ParseOccurs(child),
                    Namespace = constraint,
                    ResolvedNamespace = constraint == "##targetNamespace" ? targetNamespace : constraint,
                });
            }
            else if (child.Name == Xs + "sequence")
            {
                items.Add(ParseSequence(child, targetNamespace));
            }
            else if (child.Name == Xs + "choice")
            {
                items.Add(ParseChoice(child, targetNamespace));
            }
            else if (child.Name == Xs + "annotation")
            {
                // Documentation only.
            }
            else
            {
                throw new SchemaException(child, $"Unexpected particle '{child.Name}'.");
            }
        }
        return items;
    }

    private static Occurs ParseOccurs(XElement element)
    {
        int min = element.Attribute("minOccurs") is { } lo ? int.Parse(lo.Value) : 1;
        string? hi = element.Attribute("maxOccurs")?.Value;
        int max = hi is null ? 1 : hi == "unbounded" ? Occurs.Unbounded : int.Parse(hi);
        return new Occurs(min, max);
    }

    private static string? Documentation(XElement element)
    {
        var text = element.Element(Xs + "annotation")?.Element(Xs + "documentation")?.Value;
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Schema documentation is indented for readability inside the XML; collapse it so it
        // can be re-wrapped into a C# doc comment.
        var lines = text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0);
        return string.Join(" ", lines);
    }
}
