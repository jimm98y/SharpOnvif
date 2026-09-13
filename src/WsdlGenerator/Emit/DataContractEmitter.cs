using WsdlGenerator.Binding;

namespace WsdlGenerator.Emit;

/// <summary>
/// Emits the generated data contracts: the enums and classes, their properties, and the compiled
/// read and write logic that replaces reflection-based XML serialization.
///
/// The XmlSerializer attributes are emitted as well, even though nothing reads them at runtime.
/// They keep the generated surface identical to the bindings this replaces, and they let the test
/// suite serialize the same object both ways and assert the two agree, which is what keeps the
/// hand-written serializers honest.
/// </summary>
internal sealed class DataContractEmitter
{

    private readonly CsModel _model;
    private readonly string _namespace;
    private readonly Dictionary<string, string> _namespaceConstants = new(StringComparer.Ordinal);

    private readonly bool _isShared;
    private readonly string? _sharedNamespace;

    /// <summary>Namespace of the contract base class, the reader and the writer.</summary>
    private readonly string Runtime;

    /// <param name="isShared">
    /// True for the model of the schemas the services share. Its helpers are called from the
    /// other assemblies, so they cannot be internal the way a service's own helpers are.
    /// </param>
    /// <param name="sharedNamespace">
    /// Where the shared types live, so a service's xsi:type factory can defer to theirs. Null for
    /// the shared model itself.
    /// </param>
    /// <param name="runtime">
    /// Root namespace of the runtime the contracts are read and written by.
    /// </param>
    public DataContractEmitter(
        CsModel model, string @namespace, bool isShared, string runtime, string? sharedNamespace = null)
    {
        _model = model;
        _namespace = @namespace;
        _isShared = isShared;
        _sharedNamespace = sharedNamespace;
        Runtime = runtime + ".Xml";
    }

    private string HelperVisibility => _isShared ? "public" : "internal";

    public string Emit()
    {
        AssignNamespaceConstants();

        var writer = new CSharpWriter();
        writer.Lines(GeneratedFile.Header);
        writer.Line();
        writer.Line($"namespace {_namespace}");

        using (writer.Braces())
        {
            EmitNamespaceConstants(writer);

            foreach (var @enum in _model.Enums)
            {
                writer.Line();
                EmitEnum(writer, @enum);
            }

            writer.Line();
            EmitEnumConverters(writer);

            foreach (var @class in _model.Classes.OrderBy(c => c.Name, StringComparer.Ordinal))
            {
                writer.Line();
                EmitClass(writer, @class);
            }

            writer.Line();
            XmlTypeFactoryEmitter.Emit(writer, _model, HelperVisibility, Runtime, _isShared ? null : _sharedNamespace);

            writer.Line();
            SoapActionsEmitter.Emit(writer, _model);
        }

        return writer.ToString();
    }

    // ---------------------------------------------------------------- namespaces

    /// <summary>
    /// Gives every XML namespace in the model a named constant, so the generated code reads as
    /// <c>Ns.Tt</c> rather than repeating a URL thousands of times.
    /// </summary>
    private void AssignNamespaceConstants()
    {
        var namespaces = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var @class in _model.Classes)
        {
            if (@class.XmlName is { } name && name.Namespace.Length > 0) namespaces.Add(name.Namespace);
            if (@class.WrapperElement is { } element && element.Namespace.Length > 0) namespaces.Add(element.Namespace);
            foreach (var member in @class.Members)
            {
                if (member.XmlName.Namespace.Length > 0) namespaces.Add(member.XmlName.Namespace);
                foreach (var option in member.Choices)
                {
                    if (option.Namespace.Length > 0) namespaces.Add(option.Namespace);
                }
            }
        }

        var taken = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ns in namespaces)
            _namespaceConstants[ns] = CsharpNaming.Unique(NamespaceConstantName(ns), taken);
    }

    /// <summary>Derives a readable constant name from a namespace URI.</summary>
    private static string NamespaceConstantName(string ns)
    {
        var segments = ns
            .Replace("http://", "").Replace("https://", "")
            .Split(['/', '.', ':', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s is not ("www" or "org" or "com" or "net" or "wsdl" or "schema" or "xsd"))
            .ToList();

        if (segments.Count == 0) segments.Add("Ns");

        // The last two segments are the distinguishing ones: ".../ver10/device/wsdl" -> Ver10Device.
        var chosen = segments.Skip(Math.Max(0, segments.Count - 2));
        string name = string.Concat(chosen.Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1)));
        return CsharpNaming.Identifier(name);
    }

    private void EmitNamespaceConstants(CSharpWriter writer)
    {
        writer.Line("/// <summary>XML namespaces used by this service's contracts.</summary>");
        writer.Line("internal static class Ns");
        using (writer.Braces())
        {
            foreach (var (uri, name) in _namespaceConstants.OrderBy(kv => kv.Value, StringComparer.Ordinal))
                writer.Line($"public const string {name} = \"{uri}\";");
        }
    }

    private string NsRef(string ns) =>
        ns.Length == 0 ? "null" : _namespaceConstants.TryGetValue(ns, out var name) ? "Ns." + name : Quote(ns);

    private static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    // ---------------------------------------------------------------- enums

    private static void EmitEnum(CSharpWriter writer, CsEnum @enum)
    {
        writer.Doc(@enum.Documentation);
        if (@enum.XmlName.Namespace.Length > 0)
            writer.Line($"[System.Xml.Serialization.XmlTypeAttribute(Namespace=\"{@enum.XmlName.Namespace}\")]");

        writer.Line($"public enum {@enum.Name}");
        using (writer.Braces())
        {
            foreach (var member in @enum.Members)
            {
                writer.Doc(member.Documentation);
                if (member.XmlValue is not null)
                    writer.Line($"[System.Xml.Serialization.XmlEnumAttribute({Quote(member.XmlValue)})]");
                writer.Line($"{CsharpNaming.Escape(member.Name)},");
                writer.Line();
            }
        }
    }

    /// <summary>
    /// Emits the enum conversions the serializers call. An unrecognised value parses to the
    /// enum's first member rather than throwing: devices do send values outside the published
    /// enumeration, and losing one field is better than failing the whole response.
    /// </summary>
    private void EmitEnumConverters(CSharpWriter writer)
    {
        writer.Line("/// <summary>Conversions between the generated enums and their XML lexical forms.</summary>");
        writer.Line($"{HelperVisibility} static class EnumXml");
        using (writer.Braces())
        {
            bool first = true;
            foreach (var @enum in _model.Enums)
            {
                if (!first) writer.Line();
                first = false;

                writer.Line($"public static string ToXml({@enum.Name} value)");
                using (writer.Braces())
                {
                    writer.Line("switch (value)");
                    using (writer.Braces())
                    {
                        foreach (var member in @enum.Members)
                        {
                            writer.Line($"case {@enum.Name}.{CsharpNaming.Escape(member.Name)}:");
                            writer.Line($"    return {Quote(member.XmlValue ?? member.Name)};");
                        }
                        writer.Line("default:");
                        writer.Line("    return null;");
                    }
                }

                writer.Line();
                writer.Line($"public static {@enum.Name} Parse{@enum.Name}(string text)");
                using (writer.Braces())
                {
                    writer.Line("switch (text)");
                    using (writer.Braces())
                    {
                        foreach (var member in @enum.Members)
                        {
                            writer.Line($"case {Quote(member.XmlValue ?? member.Name)}:");
                            writer.Line($"    return {@enum.Name}.{CsharpNaming.Escape(member.Name)};");
                        }
                        writer.Line("default:");
                        writer.Line($"    return default({@enum.Name});");
                    }
                }
            }
        }
    }

    // ---------------------------------------------------------------- classes

    private void EmitClass(CSharpWriter writer, CsClass @class)
    {
        writer.Doc(@class.Documentation);

        foreach (var derived in @class.DerivedClasses)
            writer.Line($"[System.Xml.Serialization.XmlIncludeAttribute(typeof({derived.NameFrom(_namespace)}))]");

        if (@class.XmlName is { } xmlName && xmlName.Namespace.Length > 0)
            writer.Line($"[System.Xml.Serialization.XmlTypeAttribute(Namespace=\"{xmlName.Namespace}\")]");

        if (@class.WrapperElement is { } wrapper)
            writer.Line($"[System.Xml.Serialization.XmlRootAttribute(\"{wrapper.LocalName}\", Namespace=\"{wrapper.Namespace}\")]");

        string baseType = @class.BaseClass?.NameFrom(_namespace) ?? $"{Runtime}.XmlContract";
        writer.Line($"public partial class {@class.Name} : {baseType}");

        using (writer.Braces())
        {
            EmitFields(writer, @class);
            EmitProperties(writer, @class);
            EmitConstructors(writer, @class);
            EmitSerialization(writer, @class);
        }
    }

    private static void EmitFields(CSharpWriter writer, CsClass @class)
    {
        foreach (var member in @class.Members)
        {
            writer.Line($"private {MemberType(member)} {member.FieldName};");
            if (member.NeedsSpecified) writer.Line($"private bool {member.FieldName}Specified;");
            writer.Line();
        }
    }

    private static string MemberType(CsMember member) =>
        member.IsArray ? member.Type.CsName + "[]" : member.Type.CsName;

    private void EmitProperties(CSharpWriter writer, CsClass @class)
    {
        foreach (var member in @class.Members)
        {
            writer.Doc(member.Documentation);
            EmitMemberAttributes(writer, member);

            writer.Line($"public {MemberType(member)} {CsharpNaming.Escape(member.Name)}");
            using (writer.Braces())
            {
                writer.Line($"get {{ return this.{member.FieldName}; }}");
                writer.Line($"set {{ this.{member.FieldName} = value; }}");
            }
            writer.Line();

            if (!member.NeedsSpecified) continue;

            writer.Line("/// <summary>");
            writer.Line($"/// Whether <see cref=\"{CsharpNaming.Escape(member.Name)}\"/> was present. The value type cannot");
            writer.Line("/// otherwise distinguish an absent optional element from a zero one.");
            writer.Line("/// </summary>");
            writer.Line("[System.Xml.Serialization.XmlIgnoreAttribute()]");
            writer.Line($"public bool {CsharpNaming.Escape(member.Name)}Specified");
            using (writer.Braces())
            {
                writer.Line($"get {{ return this.{member.FieldName}Specified; }}");
                writer.Line($"set {{ this.{member.FieldName}Specified = value; }}");
            }
            writer.Line();
        }
    }

    private void EmitMemberAttributes(CSharpWriter writer, CsMember member)
    {
        string dataType = member.Type.DataType is { } dt ? $", DataType=\"{dt}\"" : "";

        switch (member.Kind)
        {
            case MemberKind.Attribute:
                var attributeArguments = new List<string>();
                if (member.Name != member.XmlName.LocalName) attributeArguments.Add($"\"{member.XmlName.LocalName}\"");
                if (member.XmlName.Namespace.Length > 0)
                {
                    attributeArguments.Add("Form=System.Xml.Schema.XmlSchemaForm.Qualified");
                    attributeArguments.Add($"Namespace=\"{member.XmlName.Namespace}\"");
                }
                if (member.Type.DataType is { } attributeDataType) attributeArguments.Add($"DataType=\"{attributeDataType}\"");
                writer.Line($"[System.Xml.Serialization.XmlAttributeAttribute({string.Join(", ", attributeArguments)})]");
                break;

            case MemberKind.Text:
                writer.Line($"[System.Xml.Serialization.XmlTextAttribute({dataType.TrimStart(',', ' ')})]");
                break;

            case MemberKind.AnyElement:
                if (member.CapturesText) writer.Line("[System.Xml.Serialization.XmlTextAttribute()]");
                string wildcardNs = member.WildcardNamespace is { } wns ? $"Namespace=\"{wns}\", " : "";
                writer.Line($"[System.Xml.Serialization.XmlAnyElementAttribute({wildcardNs}Order={member.Order})]");
                break;

            case MemberKind.Choice:
                foreach (var option in member.Choices.OrderBy(o => o.ElementName, StringComparer.Ordinal))
                {
                    if (option.IsWildcard)
                        writer.Line($"[System.Xml.Serialization.XmlAnyElementAttribute(Order={member.Order})]");
                    else
                        writer.Line($"[System.Xml.Serialization.XmlElementAttribute(\"{option.ElementName}\", typeof({option.Type!.CsName}), Order={member.Order})]");
                }
                if (member.ChoiceIdentifier is { } identifier)
                    writer.Line($"[System.Xml.Serialization.XmlChoiceIdentifierAttribute(\"{identifier}\")]");
                break;

            case MemberKind.ChoiceIdentifier:
                writer.Line($"[System.Xml.Serialization.XmlElementAttribute(\"{member.Name}\", Order={member.Order})]");
                writer.Line("[System.Xml.Serialization.XmlIgnoreAttribute()]");
                break;

            case MemberKind.Element when member.ArrayItem is { } wrapped:
                writer.Line($"[System.Xml.Serialization.XmlArrayAttribute(Order={member.Order})]");
                writer.Line($"[System.Xml.Serialization.XmlArrayItemAttribute(\"{wrapped.LocalName}\", IsNullable=false)]");
                break;

            default:
                // The name is spelled out when the member had to be renamed to be a legal
                // identifier, and for arrays, where it names the repeated element.
                string elementName = member.IsArray || member.Name != member.XmlName.LocalName
                    ? $"\"{member.XmlName.LocalName}\", "
                    : "";
                string nillable = member.IsNillable ? ", IsNullable=true" : "";
                writer.Line($"[System.Xml.Serialization.XmlElementAttribute({elementName}Order={member.Order}{dataType}{nillable})]");
                break;
        }
    }

    private static void EmitConstructors(CSharpWriter writer, CsClass @class)
    {
        if (!@class.IsMessageWrapper) return;

        writer.Line($"public {@class.Name}()");
        using (writer.Braces()) { }
        writer.Line();

        var settable = @class.Members.Where(m => m.Kind != MemberKind.ChoiceIdentifier).ToList();
        if (settable.Count == 0) return;

        // The all-members constructor is what call sites written against the previous bindings
        // use to build a request in one expression.
        string parameters = string.Join(", ",
            settable.Select(m => $"{MemberType(m)} {CsharpNaming.Escape(LowerFirst(m.Name))}"));

        writer.Line($"public {@class.Name}({parameters})");
        using (writer.Braces())
        {
            foreach (var member in settable)
            {
                writer.Line($"this.{member.FieldName} = {CsharpNaming.Escape(LowerFirst(member.Name))};");
                if (member.NeedsSpecified) writer.Line($"this.{member.FieldName}Specified = true;");
            }
        }
        writer.Line();
    }

    private static string LowerFirst(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name.Substring(1);

    // ---------------------------------------------------------------- serialization

    private void EmitSerialization(CSharpWriter writer, CsClass @class)
    {
        bool hasBase = @class.BaseClass is not null;

        if (@class.XmlName is { } typeName)
        {
            writer.Line($"protected override string XmlTypeName {{ get {{ return \"{typeName.LocalName}\"; }} }}");
            writer.Line();
            writer.Line($"protected override string XmlTypeNamespace {{ get {{ return {NsRef(typeName.Namespace)}; }} }}");
            writer.Line();
        }

        EmitWriteAttributes(writer, @class, hasBase);
        EmitWriteContent(writer, @class, hasBase);
        EmitReadAttribute(writer, @class, hasBase);
        EmitReadElement(writer, @class, hasBase);
        EmitReadText(writer, @class, hasBase);
    }

    private void EmitWriteAttributes(CSharpWriter writer, CsClass @class, bool hasBase)
    {
        var attributes = @class.Members.Where(m => m.Kind == MemberKind.Attribute).ToList();
        if (attributes.Count == 0 && !hasBase) return;
        if (attributes.Count == 0) return;

        writer.Line($"protected override void WriteXmlAttributes({Runtime}.IXmlWriter writer)");
        using (writer.Braces())
        {
            if (hasBase) writer.Line("base.WriteXmlAttributes(writer);");

            foreach (var member in attributes)
            {
                string value = ToXmlExpression(member, $"this.{member.FieldName}");
                string call = $"writer.WriteAttributeString({NsRef(member.XmlName.Namespace)}, \"{member.XmlName.LocalName}\", {value});";

                if (member.NeedsSpecified)
                {
                    writer.Line($"if (this.{member.FieldName}Specified)");
                    using (writer.Braces()) writer.Line(call);
                }
                else
                {
                    writer.Line(call);
                }
            }
        }
        writer.Line();
    }

    private void EmitWriteContent(CSharpWriter writer, CsClass @class, bool hasBase)
    {
        var content = @class.Members
            .Where(m => m.Kind is MemberKind.Element or MemberKind.AnyElement or MemberKind.Text or MemberKind.Choice)
            .OrderBy(m => m.Order)
            .ToList();

        if (content.Count == 0) return;

        writer.Line($"protected override void WriteXmlContent({Runtime}.IXmlWriter writer)");
        using (writer.Braces())
        {
            if (hasBase) writer.Line("base.WriteXmlContent(writer);");

            foreach (var member in content) EmitWriteMember(writer, member);
        }
        writer.Line();
    }

    private void EmitWriteMember(CSharpWriter writer, CsMember member)
    {
        string field = $"this.{member.FieldName}";

        switch (member.Kind)
        {
            case MemberKind.AnyElement:
                writer.Line($"writer.WriteAny({field});");
                return;

            case MemberKind.Text:
                if (member.IsArray)
                {
                    writer.Line($"if ({field} != null)");
                    using (writer.Braces())
                    {
                        writer.Line($"for (int i = 0; i < {field}.Length; i++)");
                        using (writer.Braces()) writer.Line($"writer.WriteText({field}[i]);");
                    }
                }
                else
                {
                    writer.Line($"writer.WriteText({ToXmlExpression(member, field)});");
                }
                return;

            case MemberKind.Choice:
                EmitWriteChoice(writer, member);
                return;
        }

        if (member.ArrayItem is { } item)
        {
            writer.Line($"if ({field} != null)");
            using (writer.Braces())
            {
                writer.Line($"writer.WriteStartElement({NsRef(member.XmlName.Namespace)}, \"{member.XmlName.LocalName}\");");
                writer.Line($"for (int i = 0; i < {field}.Length; i++)");
                using (writer.Braces())
                {
                    var itemMember = new CsMember
                    {
                        Name = member.Name,
                        Type = member.Type,
                        Kind = MemberKind.Element,
                        XmlName = item,
                    };
                    EmitWriteSingleElement(writer, itemMember, $"{field}[i]");
                }
                writer.Line("writer.WriteEndElement();");
            }
            return;
        }

        if (member.IsArray)
        {
            writer.Line($"if ({field} != null)");
            using (writer.Braces())
            {
                writer.Line($"for (int i = 0; i < {field}.Length; i++)");
                using (writer.Braces()) EmitWriteSingleElement(writer, member, $"{field}[i]");
            }
            return;
        }

        if (member.NeedsSpecified)
        {
            writer.Line($"if (this.{member.FieldName}Specified)");
            using (writer.Braces()) EmitWriteSingleElement(writer, member, field);
            return;
        }

        EmitWriteSingleElement(writer, member, field);
    }

    private void EmitWriteSingleElement(CSharpWriter writer, CsMember member, string value)
    {
        string ns = NsRef(member.XmlName.Namespace);
        string name = $"\"{member.XmlName.LocalName}\"";

        if (member.Type.Kind == TypeKind.Class)
        {
            writer.Line($"writer.WriteElement({ns}, {name}, {value}, {DeclaredTypeArguments(member.Type)});");
            return;
        }

        if (member.Type.Kind is TypeKind.XmlElement or TypeKind.XmlNode)
        {
            // The member holds the payload of a wrapper whose only content is one xs:any, so the
            // wrapper element is written here and the payload verbatim inside it.
            writer.Line($"if ({value} != null)");
            using (writer.Braces())
            {
                writer.Line($"writer.WriteStartElement({ns}, {name});");
                writer.Line($"writer.WriteAnyElement({value});");
                writer.Line("writer.WriteEndElement();");
            }
            return;
        }

        if (member.Type.XsdPrimitive == "QName")
        {
            writer.Line($"writer.WriteElementQualifiedName({ns}, {name}, {value});");
            return;
        }

        writer.Line($"writer.WriteElementString({ns}, {name}, {ToXmlExpression(member, value)});");
    }

    /// <summary>
    /// The declared type of a member, passed so the writer can tell whether the value's runtime
    /// type is a schema extension that needs an xsi:type hint.
    /// </summary>
    private string DeclaredTypeArguments(CsTypeRef type)
    {
        if (type.XmlTypeName is not { } name) return "null, null";
        return $"{NsRef(name.Namespace)}, \"{name.LocalName}\"";
    }

    private void EmitWriteChoice(CSharpWriter writer, CsMember member)
    {
        string field = $"this.{member.FieldName}";

        if (member.IsArray)
        {
            writer.Line($"if ({field} != null)");
            using (writer.Braces())
            {
                writer.Line($"for (int i = 0; i < {field}.Length; i++)");
                using (writer.Braces()) EmitWriteChoiceValue(writer, member, $"{field}[i]");
            }
            return;
        }

        EmitWriteChoiceValue(writer, member, field);
    }

    private void EmitWriteChoiceValue(CSharpWriter writer, CsMember member, string value)
    {
        // Branches are told apart by the runtime type of the value, which is what the choice
        // member's declared element types differ by.
        bool first = true;
        foreach (var option in member.Choices)
        {
            if (option.IsWildcard)
            {
                writer.Line($"{(first ? "if" : "else if")} ({value} is System.Xml.XmlElement)");
                using (writer.Braces())
                    writer.Line($"((System.Xml.XmlElement){value}).WriteTo(writer.Xml);");
                first = false;
                continue;
            }

            string cast = $"(({option.Type!.CsName}){value})";
            writer.Line($"{(first ? "if" : "else if")} ({value} is {option.Type.CsName})");
            using (writer.Braces())
            {
                if (option.Type.Kind == TypeKind.Class)
                {
                    writer.Line($"writer.WriteElement({NsRef(option.Namespace)}, \"{option.ElementName}\", {cast}, {DeclaredTypeArguments(option.Type)});");
                }
                else
                {
                    writer.Line($"writer.WriteElementString({NsRef(option.Namespace)}, \"{option.ElementName}\", {ScalarToXml(option.Type, cast)});");
                }
            }
            first = false;
        }
    }

    private void EmitReadAttribute(CSharpWriter writer, CsClass @class, bool hasBase)
    {
        var attributes = @class.Members.Where(m => m.Kind == MemberKind.Attribute).ToList();
        if (attributes.Count == 0) return;

        writer.Line($"protected override bool ReadXmlAttribute({Runtime}.IXmlReader reader)");
        using (writer.Braces())
        {
            writer.Line("switch (reader.LocalName)");
            using (writer.Braces())
            {
                foreach (var member in attributes)
                {
                    writer.Line($"case \"{member.XmlName.LocalName}\":");
                    writer.Indent();
                    if (member.XmlName.Namespace.Length > 0)
                        writer.Line($"if (reader.NamespaceUri != {NsRef(member.XmlName.Namespace)}) break;");
                    writer.Line($"this.{member.FieldName} = {FromXmlExpression(member, "reader.AttributeValue")};");
                    if (member.NeedsSpecified) writer.Line($"this.{member.FieldName}Specified = true;");
                    writer.Line("return true;");
                    writer.Outdent();
                }
            }
            writer.Line(hasBase ? "return base.ReadXmlAttribute(reader);" : "return false;");
        }
        writer.Line();
    }

    private void EmitReadElement(CSharpWriter writer, CsClass @class, bool hasBase)
    {
        var elements = @class.Members.Where(m => m.Kind == MemberKind.Element).ToList();
        var choices = @class.Members.Where(m => m.Kind == MemberKind.Choice).ToList();
        var wildcard = @class.Members.FirstOrDefault(m => m.Kind == MemberKind.AnyElement);

        if (elements.Count == 0 && choices.Count == 0 && wildcard is null) return;

        writer.Line($"protected override bool ReadXmlElement({Runtime}.IXmlReader reader)");
        using (writer.Braces())
        {
            if (elements.Count > 0 || choices.Count > 0)
            {
                writer.Line("switch (reader.LocalName)");
                using (writer.Braces())
                {
                    foreach (var member in elements)
                    {
                        writer.Line($"case \"{member.XmlName.LocalName}\":");
                        writer.Indent();
                        if (member.XmlName.Namespace.Length > 0)
                            writer.Line($"if (reader.NamespaceUri != {NsRef(member.XmlName.Namespace)}) break;");
                        EmitReadElementBody(writer, member);
                        writer.Line("return true;");
                        writer.Outdent();
                    }

                    foreach (var member in choices)
                    {
                        foreach (var option in member.Choices.Where(o => !o.IsWildcard))
                        {
                            writer.Line($"case \"{option.ElementName}\":");
                            writer.Indent();
                            if (option.Namespace.Length > 0)
                                writer.Line($"if (reader.NamespaceUri != {NsRef(option.Namespace)}) break;");
                            EmitReadChoiceBranch(writer, @class, member, option);
                            writer.Line("return true;");
                            writer.Outdent();
                        }
                    }
                }
            }

            if (hasBase) writer.Line("if (base.ReadXmlElement(reader)) return true;");

            // Anything the schema does not name belongs to the wildcard, if the type has one.
            var choiceWildcard = choices.FirstOrDefault(c => c.Choices.Any(o => o.IsWildcard));
            if (wildcard is not null)
            {
                string read = wildcard.Type.Kind == TypeKind.XmlNode ? "ReadAnyNode" : "ReadAnyElement";
                writer.Line($"reader.Append(ref this.{wildcard.FieldName}, reader.{read}());");
                writer.Line("return true;");
            }
            else if (choiceWildcard is not null)
            {
                EmitReadChoiceWildcard(writer, @class, choiceWildcard);
                writer.Line("return true;");
            }
            else
            {
                writer.Line("return false;");
            }
        }
        writer.Line();
    }

    private void EmitReadElementBody(CSharpWriter writer, CsMember member)
    {
        string field = $"this.{member.FieldName}";

        if (member.IsNillable)
        {
            writer.Line("if (reader.IsNil())");
            using (writer.Braces())
            {
                writer.Line("reader.Skip();");
                writer.Line("return true;");
            }
        }

        string value = member.Type.Kind switch
        {
            TypeKind.Class => $"reader.ReadElementObject<{member.Type.CsName}>(() => new {member.Type.CsName}())",
            TypeKind.XmlElement or TypeKind.XmlNode => "reader.ReadWrappedElement()",
            _ => member.Type.XsdPrimitive == "QName"
                ? "reader.ReadElementQualifiedName()"
                : FromXmlExpression(member, "reader.ReadElementText()"),
        };

        if (member.ArrayItem is { } item)
        {
            writer.Line($"reader.ReadWrappedArray({NsRef(item.Namespace)}, \"{item.LocalName}\", () =>");
            using (writer.Braces())
            {
                writer.Line($"reader.Append(ref {field}, {value});");
            }
            writer.Line(");");
        }
        else if (member.IsArray)
        {
            writer.Line($"reader.Append(ref {field}, {value});");
        }
        else
        {
            writer.Line($"{field} = {value};");
            if (member.NeedsSpecified) writer.Line($"this.{member.FieldName}Specified = true;");
        }
    }

    private void EmitReadChoiceBranch(CSharpWriter writer, CsClass @class, CsMember member, CsChoiceOption option)
    {
        string value = option.Type!.Kind == TypeKind.Class
            ? $"reader.ReadElementObject<{option.Type.CsName}>(() => new {option.Type.CsName}())"
            : ScalarFromXml(option.Type, "reader.ReadElementText()");

        if (member.IsArray)
        {
            writer.Line($"reader.Append(ref this.{member.FieldName}, (object){value});");
            if (member.ChoiceIdentifier is { } identifier)
            {
                var discriminator = FindDiscriminator(@class, identifier);
                if (discriminator is not null)
                {
                    string branch = DiscriminatorMember(discriminator.ChoiceIdentifierEnum, option.ElementName);
                    writer.Line($"reader.Append(ref this.{discriminator.FieldName}, {branch});");
                }
            }
        }
        else
        {
            writer.Line($"this.{member.FieldName} = {value};");
        }
    }

    private void EmitReadChoiceWildcard(CSharpWriter writer, CsClass @class, CsMember member)
    {
        writer.Line($"reader.Append(ref this.{member.FieldName}, (object)reader.ReadAnyElement());");
        if (member.ChoiceIdentifier is not { } identifier) return;

        var discriminator = FindDiscriminator(@class, identifier);
        if (discriminator is null) return;

        string branch = DiscriminatorMember(discriminator.ChoiceIdentifierEnum, "##any:");
        writer.Line($"reader.Append(ref this.{discriminator.FieldName}, {branch});");
    }

    private static CsMember? FindDiscriminator(CsClass @class, string identifier) =>
        @class.Members.FirstOrDefault(m => m.Kind == MemberKind.ChoiceIdentifier && m.Name == identifier);

    private static string DiscriminatorMember(CsEnum? discriminator, string elementName)
    {
        if (discriminator is null) return "default";
        var match = discriminator.Members.FirstOrDefault(m => (m.XmlValue ?? m.Name) == elementName)
                    ?? discriminator.Members.First();
        return $"{discriminator.Name}.{CsharpNaming.Escape(match.Name)}";
    }

    private void EmitReadText(CSharpWriter writer, CsClass @class, bool hasBase)
    {
        var text = @class.Members.FirstOrDefault(m => m.Kind == MemberKind.Text);
        var mixedWildcard = @class.Members.FirstOrDefault(m => m.Kind == MemberKind.AnyElement && m.CapturesText);

        if (text is null && mixedWildcard is null) return;

        writer.Line($"protected override void ReadXmlText({Runtime}.IXmlReader reader, string text)");
        using (writer.Braces())
        {
            if (mixedWildcard is not null)
            {
                // The character data belongs in the wildcard, interleaved with its elements.
                writer.Line($"reader.Append(ref this.{mixedWildcard.FieldName}, reader.CreateTextNode(text));");
            }
            else if (text!.IsArray)
            {
                writer.Line($"reader.Append(ref this.{text.FieldName}, text);");
            }
            else
            {
                writer.Line($"this.{text.FieldName} = {ScalarFromXml(text.Type, "text")};");
            }
        }
        writer.Line();
    }

    // ---------------------------------------------------------------- scalar conversions

    private string ToXmlExpression(CsMember member, string value) => ScalarToXml(member.Type, value);

    private string FromXmlExpression(CsMember member, string text) => ScalarFromXml(member.Type, text);

    /// <summary>The EnumXml class that declares the conversions for a type, qualified if needed.</summary>
    private static string EnumHelper(CsTypeRef type)
    {
        int lastDot = type.CsName.LastIndexOf('.');
        return lastDot < 0 ? "EnumXml" : type.CsName.Substring(0, lastDot) + ".EnumXml";
    }

    /// <summary>The enum's own name, without the namespace it may be qualified with.</summary>
    private static string SimpleName(CsTypeRef type)
    {
        int lastDot = type.CsName.LastIndexOf('.');
        return lastDot < 0 ? type.CsName : type.CsName.Substring(lastDot + 1);
    }

    /// <summary>Renders a CLR value as its XML lexical form.</summary>
    private string ScalarToXml(CsTypeRef type, string value)
    {
        if (type.Kind == TypeKind.Enum) return $"{EnumHelper(type)}.ToXml({value})";
        if (type.Kind == TypeKind.Object) return $"System.Convert.ToString({value}, System.Globalization.CultureInfo.InvariantCulture)";

        // An xs:list member is an array of scalars sharing one element.
        if (type.CsName.EndsWith("[]", StringComparison.Ordinal) && type.CsName != "byte[]")
        {
            string item = type.CsName.Substring(0, type.CsName.Length - 2);
            var itemType = type with { CsName = item };
            string project = item == "string"
                ? value
                : "System.Array.ConvertAll(" + value + ", x => " + ScalarToXml(itemType, "x") + ")";
            return $"writer.JoinList({project})";
        }

        return type.XsdPrimitive switch
        {
            "string" or "anyURI" or "token" or "duration" or "NCName" or "Name" or "NMTOKEN" or "NMTOKENS"
                or "ID" or "IDREF" or "IDREFS" or "ENTITY" or "ENTITIES" or "NOTATION" or "language"
                or "normalizedString" or "integer" or "nonNegativeInteger" or "positiveInteger"
                or "negativeInteger" or "nonPositiveInteger" or "gYear" or "gMonth" or "gDay"
                or "gYearMonth" or "gMonthDay" or "anySimpleType" => value,
            "date" => $"writer.ToDateString({value})",
            "time" => $"writer.ToTimeString({value})",
            "hexBinary" => $"writer.ToHexString({value})",
            "QName" => $"writer.QualifiedNameToString({value})",
            _ => $"writer.ToXml({value})",
        };
    }

    /// <summary>Parses an XML lexical form into a CLR value.</summary>
    private string ScalarFromXml(CsTypeRef type, string text)
    {
        if (type.Kind == TypeKind.Enum) return $"{EnumHelper(type)}.Parse{SimpleName(type)}({text})";
        if (type.Kind == TypeKind.Object) return text;

        if (type.CsName.EndsWith("[]", StringComparison.Ordinal) && type.CsName != "byte[]")
        {
            string item = type.CsName.Substring(0, type.CsName.Length - 2);
            var itemType = type with { CsName = item };
            string split = $"reader.SplitList({text})";
            return item == "string"
                ? split
                : "System.Array.ConvertAll(" + split + ", x => " + ScalarFromXml(itemType, "x") + ")";
        }

        return type.XsdPrimitive switch
        {
            "boolean" => $"reader.ToBoolean({text})",
            "byte" => $"reader.ToSByte({text})",
            "unsignedByte" => $"reader.ToByte({text})",
            "short" => $"reader.ToInt16({text})",
            "unsignedShort" => $"reader.ToUInt16({text})",
            "int" => $"reader.ToInt32({text})",
            "unsignedInt" => $"reader.ToUInt32({text})",
            "long" => $"reader.ToInt64({text})",
            "unsignedLong" => $"reader.ToUInt64({text})",
            "decimal" => $"reader.ToDecimal({text})",
            "float" => $"reader.ToSingle({text})",
            "double" => $"reader.ToDouble({text})",
            "dateTime" or "date" or "time" => $"reader.ToDateTime({text})",
            "base64Binary" => $"reader.ToByteArray({text})",
            "hexBinary" => $"reader.FromHexString({text})",
            "QName" => $"reader.ToQualifiedName({text})",
            _ => text,
        };
    }
}
