using WsdlGenerator.Parser;

namespace WsdlGenerator.Generator;

public class ClientGenerator
{
    public string Generate(WsdlDefinition wsdl, string namespaceName = "Generated")
    {
        var w = new CodeWriter();

        WriteHeader(w, wsdl, namespaceName);
        WriteConstructorAndHelpers(w, wsdl);

        foreach (var op in wsdl.Operations)
        {
            wsdl.Elements.TryGetValue(op.InputElementName, out var inputElem);
            wsdl.Elements.TryGetValue(op.OutputElementName, out var outputElem);
            WriteOperation(w, op, inputElem, outputElem, wsdl.TargetNamespace);
        }

        w.CloseBrace(); // class
        return w.ToString();
    }

    private static void WriteHeader(CodeWriter w, WsdlDefinition wsdl, string namespaceName)
    {
        w.Line($"// Generated from WSDL - {wsdl.ServiceName}Service");
        w.Line("using System;");
        w.Line("using System.Collections.Generic;");
        w.Line("using System.Net.Http;");
        w.Line("using System.Text;");
        w.Line("using System.Threading;");
        w.Line("using System.Threading.Tasks;");
        w.Line("using System.Xml.Linq;");
        w.Line();
        w.Line($"namespace {namespaceName};");
        w.Line();
        w.Line($"public class {wsdl.ServiceName}Client");
        w.OpenBrace();
        w.Line("private readonly HttpClient _httpClient;");
        w.Line("private readonly string _endpoint;");
        w.Line($"private const string TargetNs = \"{wsdl.TargetNamespace}\";");
        w.Line("private static readonly XNamespace SoapNs = \"http://www.w3.org/2003/05/soap-envelope\";");
        w.Line();
    }

    private static void WriteConstructorAndHelpers(CodeWriter w, WsdlDefinition wsdl)
    {
        w.Line($"public {wsdl.ServiceName}Client(string endpoint, HttpClient httpClient)");
        w.OpenBrace();
        w.Line("_endpoint = endpoint;");
        w.Line("_httpClient = httpClient;");
        w.CloseBrace();
        w.Line();

        // SendAsync helper
        w.Line("private async Task<XElement?> SendAsync(string soapAction, XElement bodyContent, CancellationToken ct)");
        w.OpenBrace();
        w.Line("var envelope = new XElement(SoapNs + \"Envelope\",");
        w.Line("    new XElement(SoapNs + \"Body\", bodyContent));");
        w.Line("string xml = $\"<?xml version=\\\"1.0\\\" encoding=\\\"utf-8\\\"?>{envelope}\";");
        w.Line("using var content = new StringContent(xml, Encoding.UTF8, \"application/soap+xml\");");
        w.Line("content.Headers.TryAddWithoutValidation(\"SOAPAction\", $\"\\\"{soapAction}\\\"\");");
        w.Line("using var response = await _httpClient.PostAsync(_endpoint, content, ct);");
        w.Line("response.EnsureSuccessStatusCode();");
        w.Line("string responseXml = await response.Content.ReadAsStringAsync(ct);");
        w.Line("var doc = XDocument.Parse(responseXml);");
        w.Line("var body = doc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == \"Body\");");
        w.Line("return body?.Elements().FirstOrDefault();");
        w.CloseBrace();
        w.Line();
    }

    private static void WriteOperation(
        CodeWriter w, WsdlOperation op,
        XsdElement? inputElem, XsdElement? outputElem,
        string targetNs)
    {
        var fields = inputElem?.Fields ?? [];
        var parameters = BuildParameters(fields);

        // Build parameter list string
        var paramParts = parameters.Select(p => $"{p.CsType} {p.ParamName}").ToList();
        paramParts.Add("CancellationToken ct = default");
        string paramStr = string.Join(", ", paramParts);

        w.Line($"public async Task<XElement?> {op.Name}Async({paramStr})");
        w.OpenBrace();
        w.Line($"const string soapAction = \"{op.SoapAction}\";");
        w.Line($"var body = new XElement(XName.Get(\"{op.InputElementName}\", TargetNs));");

        foreach (var (field, param) in fields.Zip(parameters))
        {
            WriteFieldXmlConstruction(w, field, param, targetNs);
        }

        w.Line("return await SendAsync(soapAction, body, ct);");
        w.CloseBrace();
        w.Line();
    }

    private static void WriteFieldXmlConstruction(
        CodeWriter w, XsdField field, ParamInfo param, string targetNs)
    {
        string elemCreate = $"XName.Get(\"{field.Name}\", TargetNs)";

        if (field.IsMultiple)
        {
            w.Line($"foreach (var item in {param.ParamName})");
            if (param.BaseType == "XElement")
                w.Line($"    body.Add(item);");
            else
                w.Line($"    body.Add(new XElement({elemCreate}, item.ToString()));");
        }
        else if (field.IsOptional)
        {
            w.Line($"if ({param.ParamName} != null)");
            if (param.BaseType == "XElement")
                w.Line($"    body.Add({param.ParamName});");
            else if (param.BaseType == "bool")
                w.Line($"    body.Add(new XElement({elemCreate}, {param.ParamName}.Value ? \"true\" : \"false\"));");
            else
                w.Line($"    body.Add(new XElement({elemCreate}, {param.ParamName}));");
        }
        else
        {
            if (param.BaseType == "XElement")
                w.Line($"body.Add({param.ParamName});");
            else if (param.BaseType == "bool")
                w.Line($"body.Add(new XElement({elemCreate}, {param.ParamName} ? \"true\" : \"false\"));");
            else
                w.Line($"body.Add(new XElement({elemCreate}, {param.ParamName}));");
        }
    }

    private sealed record ParamInfo(string CsType, string ParamName, string BaseType);

    private static List<ParamInfo> BuildParameters(IReadOnlyList<XsdField> fields)
    {
        var result = new List<ParamInfo>();
        var usedNames = new HashSet<string>();
        foreach (var f in fields)
        {
            string baseType = TypeMapper.MapXsdToCSharp(f.XsdType);
            string csType = TypeMapper.ToCSharpType(f.XsdType, f.IsOptional, f.IsMultiple);
            string paramName = UniqueParamName(TypeMapper.ToCamelCase(f.Name), usedNames);
            result.Add(new ParamInfo(csType, paramName, baseType));
        }
        return result;
    }

    private static string UniqueParamName(string name, HashSet<string> used)
    {
        string candidate = name;
        int n = 2;
        while (!used.Add(candidate))
            candidate = name + n++;
        return candidate;
    }
}
