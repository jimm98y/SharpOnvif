using WsdlGenerator.Binding;

namespace WsdlGenerator.Emit;

/// <summary>
/// Emits the SOAP action constants for a service. Both the client proxies and the server
/// dispatchers refer to them, so they live alongside the contracts both sides share.
/// </summary>
internal static class SoapActionsEmitter
{
    public static void Emit(CSharpWriter writer, CsModel model)
    {
        writer.Line("/// <summary>SOAP action URIs for this service's operations.</summary>");
        writer.Line("public static class SoapActions");
        using (writer.Braces())
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var operation in model.Services.SelectMany(s => s.Operations))
            {
                if (!seen.Add(operation.Name)) continue;
                writer.Line($"public const string {CsharpNaming.Escape(operation.Name)} = \"{operation.SoapAction}\";");
            }
        }
    }
}
