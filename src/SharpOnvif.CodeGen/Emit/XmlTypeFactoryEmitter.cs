using SharpOnvif.CodeGen.Binding;

namespace SharpOnvif.CodeGen.Emit;

/// <summary>
/// Emits the factory that turns an xsi:type name into an instance.
///
/// Each generated assembly needs its own, because each carries a separate copy of the shared
/// schema types: a ConfigurationEntity from the Media assembly is a different CLR type from the
/// one in PTZ, and only the assembly's own factory can construct the right one.
/// </summary>
internal static class XmlTypeFactoryEmitter
{
    public static void Emit(CSharpWriter writer, CsModel model)
    {
        writer.Line("/// <summary>Constructs a contract named by an xsi:type attribute.</summary>");
        writer.Line("internal static class XmlTypeFactory");
        using (writer.Braces())
        {
            writer.Line("public static SharpOnvifCommon.Xml.OnvifObject Create(string ns, string name)");
            using (writer.Braces())
            {
                // Only types that can actually be named by xsi:type are worth listing: a type
                // nothing extends is always constructed from its declared type instead.
                var polymorphic = model.Classes
                    .Where(c => c.XmlName is not null && (c.BaseClass is not null || c.DerivedClasses.Count > 0))
                    .OrderBy(c => c.XmlName!.Value.LocalName, StringComparer.Ordinal)
                    .ToList();

                if (polymorphic.Count == 0)
                {
                    writer.Line("return null;");
                    return;
                }

                writer.Line("switch (name)");
                using (writer.Braces())
                {
                    foreach (var group in polymorphic.GroupBy(c => c.XmlName!.Value.LocalName, StringComparer.Ordinal))
                    {
                        writer.Line($"case \"{group.Key}\":");
                        writer.Indent();
                        foreach (var @class in group)
                        {
                            writer.Line($"if (ns == \"{@class.XmlName!.Value.Namespace}\") return new {@class.Name}();");
                        }
                        writer.Line("break;");
                        writer.Outdent();
                    }
                }
                writer.Line("return null;");
            }
        }
    }
}
