using WsdlGenerator.Parser;

namespace WsdlGenerator.Generator;

public class ServerGenerator
{
    public string Generate(WsdlDefinition wsdl, string namespaceName = "Generated")
    {
        var w = new CodeWriter();

        WriteHeader(w, wsdl, namespaceName);
        WriteHandlersDict(w, wsdl);
        WriteLifecycle(w, wsdl);
        WriteAcceptLoop(w);
        WriteRequestHandler(w);
        WriteFaultHelper(w);
        WriteAbstractMethods(w, wsdl);

        w.CloseBrace(); // class
        return w.ToString();
    }

    private static void WriteHeader(CodeWriter w, WsdlDefinition wsdl, string namespaceName)
    {
        w.Line($"// Generated from WSDL - {wsdl.ServiceName}Service");
        w.Line("using System;");
        w.Line("using System.Collections.Generic;");
        w.Line("using System.IO;");
        w.Line("using System.Net;");
        w.Line("using System.Text;");
        w.Line("using System.Threading.Tasks;");
        w.Line("using System.Xml.Linq;");
        w.Line();
        w.Line($"namespace {namespaceName};");
        w.Line();
        w.Line($"public abstract class {wsdl.ServiceName}ServiceBase");
        w.OpenBrace();
        w.Line("private HttpListener? _listener;");
        w.Line($"private const string TargetNs = \"{wsdl.TargetNamespace}\";");
        w.Line("private static readonly XNamespace SoapNs = \"http://www.w3.org/2003/05/soap-envelope\";");
        w.Line();
    }

    private static void WriteHandlersDict(CodeWriter w, WsdlDefinition wsdl)
    {
        w.Line($"private Dictionary<string, Func<XElement, Task<XElement?>>> BuildHandlers() => new()");
        w.OpenBrace();
        foreach (var op in wsdl.Operations)
        {
            if (op.SoapAction != "")
                w.Line($"[\"{op.SoapAction}\"] = req => {op.Name}Async(req),");
        }
        w.CloseBrace(";");
        w.Line();
    }

    private static void WriteLifecycle(CodeWriter w, WsdlDefinition wsdl)
    {
        w.Line("public void Start(string listenerPrefix)");
        w.OpenBrace();
        w.Line("_listener = new HttpListener();");
        w.Line("_listener.Prefixes.Add(listenerPrefix);");
        w.Line("_listener.Start();");
        w.Line("_ = AcceptLoopAsync(BuildHandlers());");
        w.CloseBrace();
        w.Line();

        w.Line("public void Stop() => _listener?.Stop();");
        w.Line();
    }

    private static void WriteAcceptLoop(CodeWriter w)
    {
        w.Line("private async Task AcceptLoopAsync(Dictionary<string, Func<XElement, Task<XElement?>>> handlers)");
        w.OpenBrace();
        w.Line("while (_listener?.IsListening == true)");
        w.OpenBrace();
        w.Line("try");
        w.OpenBrace();
        w.Line("var ctx = await _listener.GetContextAsync();");
        w.Line("_ = Task.Run(() => HandleRequestAsync(ctx, handlers));");
        w.CloseBrace();
        w.Line("catch (HttpListenerException) { break; }");
        w.Line("catch (ObjectDisposedException) { break; }");
        w.CloseBrace();
        w.CloseBrace();
        w.Line();
    }

    private static void WriteRequestHandler(CodeWriter w)
    {
        w.Line("private async Task HandleRequestAsync(");
        w.Line("    HttpListenerContext ctx,");
        w.Line("    Dictionary<string, Func<XElement, Task<XElement?>>> handlers)");
        w.OpenBrace();
        w.Line("try");
        w.OpenBrace();
        w.Line("string soapAction = ctx.Request.Headers[\"SOAPAction\"]?.Trim('\"') ?? \"\";");
        w.Line("using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);");
        w.Line("string requestXml = await reader.ReadToEndAsync();");
        w.Line();
        w.Line("XElement requestBodyElem;");
        w.Line("try");
        w.OpenBrace();
        w.Line("var doc = XDocument.Parse(requestXml);");
        w.Line("var soapBody = doc.Root?.Elements()");
        w.Line("    .FirstOrDefault(e => e.Name.LocalName == \"Body\");");
        w.Line("requestBodyElem = soapBody?.Elements().FirstOrDefault() ?? new XElement(\"Empty\");");
        w.CloseBrace();
        w.Line("catch");
        w.OpenBrace();
        w.Line("await WriteFaultAsync(ctx, \"Could not parse SOAP envelope\");");
        w.Line("return;");
        w.CloseBrace();
        w.Line();
        w.Line("if (!handlers.TryGetValue(soapAction, out var handler))");
        w.OpenBrace();
        w.Line("await WriteFaultAsync(ctx, $\"Unknown SOAPAction: {soapAction}\");");
        w.Line("return;");
        w.CloseBrace();
        w.Line();
        w.Line("XElement? responseBody = await handler(requestBodyElem);");
        w.Line();
        w.Line("var envelope = new XElement(SoapNs + \"Envelope\",");
        w.Line("    new XElement(SoapNs + \"Body\",");
        w.Line("        responseBody != null ? (object)responseBody : \"\"));");
        w.Line("string responseXml = $\"<?xml version=\\\"1.0\\\" encoding=\\\"utf-8\\\"?>{envelope}\";");
        w.Line("await WriteResponseAsync(ctx, responseXml, 200);");
        w.CloseBrace();
        w.Line("catch (Exception ex)");
        w.OpenBrace();
        w.Line("await WriteFaultAsync(ctx, ex.Message);");
        w.CloseBrace();
        w.Line("finally");
        w.OpenBrace();
        w.Line("ctx.Response.Close();");
        w.CloseBrace();
        w.CloseBrace();
        w.Line();
    }

    private static void WriteFaultHelper(CodeWriter w)
    {
        w.Line("private static async Task WriteFaultAsync(HttpListenerContext ctx, string message)");
        w.OpenBrace();
        w.Line("string escaped = System.Security.SecurityElement.Escape(message) ?? message;");
        w.Line("string fault = $\"\"\"");
        w.Line("    <?xml version=\"1.0\" encoding=\"utf-8\"?>");
        w.Line("    <s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">");
        w.Line("      <s:Body>");
        w.Line("        <s:Fault>");
        w.Line("          <s:Code><s:Value>s:Receiver</s:Value></s:Code>");
        w.Line("          <s:Reason><s:Text>{escaped}</s:Text></s:Reason>");
        w.Line("        </s:Fault>");
        w.Line("      </s:Body>");
        w.Line("    </s:Envelope>");
        w.Line("    \"\"\";");
        w.Line("await WriteResponseAsync(ctx, fault.Trim(), 500);");
        w.CloseBrace();
        w.Line();

        w.Line("private static async Task WriteResponseAsync(HttpListenerContext ctx, string xml, int statusCode)");
        w.OpenBrace();
        w.Line("byte[] bytes = Encoding.UTF8.GetBytes(xml);");
        w.Line("ctx.Response.StatusCode = statusCode;");
        w.Line("ctx.Response.ContentType = \"application/soap+xml; charset=utf-8\";");
        w.Line("ctx.Response.ContentLength64 = bytes.Length;");
        w.Line("await ctx.Response.OutputStream.WriteAsync(bytes);");
        w.CloseBrace();
        w.Line();
    }

    private static void WriteAbstractMethods(CodeWriter w, WsdlDefinition wsdl)
    {
        w.Line("// Implement these methods to handle incoming SOAP requests.");
        w.Line("// The 'request' parameter is the inner body element (the operation element).");
        w.Line("// Return the response body element, or null for empty responses.");
        w.Line();
        foreach (var op in wsdl.Operations)
        {
            w.Line($"protected abstract Task<XElement?> {op.Name}Async(XElement request);");
        }
    }
}
