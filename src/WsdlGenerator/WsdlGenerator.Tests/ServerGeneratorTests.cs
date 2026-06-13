using WsdlGenerator.Generator;
using WsdlGenerator.Parser;
using Xunit;

namespace WsdlGenerator.Tests;

public class ServerGeneratorTests
{
    private static readonly string WsdlPath =
        Path.Combine(AppContext.BaseDirectory, "devicemgmt.wsdl");

    private static (WsdlDefinition Wsdl, string Code) GenerateServer(string ns = "Generated")
    {
        var wsdl = new WsdlParser().Parse(WsdlPath);
        string code = new ServerGenerator().Generate(wsdl, ns);
        return (wsdl, code);
    }

    [Fact]
    public void Generate_ContainsNamespaceDeclaration()
    {
        var (_, code) = GenerateServer("MyApp");
        Assert.Contains("namespace MyApp;", code);
    }

    [Fact]
    public void Generate_ContainsAbstractClass()
    {
        var (wsdl, code) = GenerateServer();
        Assert.Contains($"public abstract class {wsdl.ServiceName}ServiceBase", code);
    }

    [Fact]
    public void Generate_ContainsHttpListener()
    {
        var (_, code) = GenerateServer();
        Assert.Contains("HttpListener", code);
        Assert.Contains("_listener = new HttpListener()", code);
    }

    [Fact]
    public void Generate_ContainsStartMethod()
    {
        var (_, code) = GenerateServer();
        Assert.Contains("public void Start(string listenerPrefix)", code);
        Assert.Contains("_listener.Start()", code);
    }

    [Fact]
    public void Generate_ContainsStopMethod()
    {
        var (_, code) = GenerateServer();
        Assert.Contains("public void Stop()", code);
        Assert.Contains("_listener?.Stop()", code);
    }

    [Fact]
    public void Generate_ContainsSoapActionDispatch()
    {
        var (_, code) = GenerateServer();
        Assert.Contains("SOAPAction", code);
        Assert.Contains("handlers.TryGetValue(soapAction,", code);
    }

    [Fact]
    public void Generate_ContainsFaultGeneration()
    {
        var (_, code) = GenerateServer();
        Assert.Contains("WriteFaultAsync(", code);
        Assert.Contains("s:Fault", code);
        Assert.Contains("s:Receiver", code);
    }

    [Fact]
    public void Generate_ContainsAbstractHandlerMethods()
    {
        var (wsdl, code) = GenerateServer();
        foreach (var op in wsdl.Operations)
        {
            Assert.Contains($"protected abstract Task<XElement?> {op.Name}Async(XElement request);", code);
        }
    }

    [Fact]
    public void Generate_ContainsHandlerDictionary()
    {
        var (_, code) = GenerateServer();
        Assert.Contains("BuildHandlers()", code);
        Assert.Contains("Func<XElement, Task<XElement?>>", code);
    }

    [Fact]
    public void Generate_HandlerDictionary_ContainsSoapActions()
    {
        var (wsdl, code) = GenerateServer();
        foreach (var op in wsdl.Operations.Where(o => !string.IsNullOrEmpty(o.SoapAction)))
        {
            Assert.Contains($"[\"{op.SoapAction}\"]", code);
        }
    }

    [Fact]
    public void Generate_ContainsSoapEnvelopeResponse()
    {
        var (_, code) = GenerateServer();
        Assert.Contains("SoapNs + \"Envelope\"", code);
        Assert.Contains("SoapNs + \"Body\"", code);
        Assert.Contains("application/soap+xml", code);
    }

    [Fact]
    public void Generate_ContainsXDocumentParsing()
    {
        var (_, code) = GenerateServer();
        Assert.Contains("XDocument.Parse(requestXml)", code);
    }

    [Fact]
    public void Generate_ContainsTargetNamespaceConstant()
    {
        var (wsdl, code) = GenerateServer();
        Assert.Contains($"TargetNs = \"{wsdl.TargetNamespace}\"", code);
    }

    [Fact]
    public void Generate_ContainsRequiredUsings()
    {
        var (_, code) = GenerateServer();
        Assert.Contains("using System.Net;", code);
        Assert.Contains("using System.Xml.Linq;", code);
        Assert.Contains("using System.IO;", code);
    }
}
