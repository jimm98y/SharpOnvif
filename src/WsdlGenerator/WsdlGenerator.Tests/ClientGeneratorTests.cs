using WsdlGenerator.Generator;
using WsdlGenerator.Parser;
using Xunit;

namespace WsdlGenerator.Tests;

public class ClientGeneratorTests
{
    private static readonly string WsdlPath =
        Path.Combine(AppContext.BaseDirectory, "devicemgmt.wsdl");

    private static (WsdlDefinition Wsdl, string Code) GenerateClient(string ns = "Generated")
    {
        var wsdl = new WsdlParser().Parse(WsdlPath);
        string code = new ClientGenerator().Generate(wsdl, ns);
        return (wsdl, code);
    }

    [Fact]
    public void Generate_ContainsNamespaceDeclaration()
    {
        var (_, code) = GenerateClient("MyApp");
        Assert.Contains("namespace MyApp;", code);
    }

    [Fact]
    public void Generate_ContainsClientClass()
    {
        var (wsdl, code) = GenerateClient();
        Assert.Contains($"public class {wsdl.ServiceName}Client", code);
    }

    [Fact]
    public void Generate_ContainsConstructor()
    {
        var (wsdl, code) = GenerateClient();
        Assert.Contains($"public {wsdl.ServiceName}Client(string endpoint, HttpClient httpClient)", code);
    }

    [Fact]
    public void Generate_ContainsSendAsyncHelper()
    {
        var (_, code) = GenerateClient();
        Assert.Contains("private async Task<XElement?> SendAsync(", code);
        Assert.Contains("_httpClient.PostAsync(", code);
        Assert.Contains("application/soap+xml", code);
    }

    [Fact]
    public void Generate_ContainsTargetNamespaceConstant()
    {
        var (wsdl, code) = GenerateClient();
        Assert.Contains($"TargetNs = \"{wsdl.TargetNamespace}\"", code);
    }

    [Fact]
    public void Generate_ContainsGetDeviceInformationMethod()
    {
        var (_, code) = GenerateClient();
        Assert.Contains("public async Task<XElement?> GetDeviceInformationAsync(", code);
    }

    [Fact]
    public void Generate_GetDeviceInformation_HasNoParams_BesidesCancellationToken()
    {
        var (_, code) = GenerateClient();
        // Should only have CancellationToken since input element has no fields
        Assert.Contains("GetDeviceInformationAsync(CancellationToken ct = default)", code);
    }

    [Fact]
    public void Generate_GetServices_HasBoolIncludeCapabilityParam()
    {
        var (_, code) = GenerateClient();
        Assert.Contains("GetServicesAsync(bool includeCapability,", code);
    }

    [Fact]
    public void Generate_SetHostname_HasStringNameParam()
    {
        var (_, code) = GenerateClient();
        Assert.Contains("SetHostnameAsync(string name,", code);
    }

    [Fact]
    public void Generate_DeleteUsers_HasIEnumerableStringParam()
    {
        var (_, code) = GenerateClient();
        Assert.Contains("DeleteUsersAsync(IEnumerable<string> username", code);
    }

    [Fact]
    public void Generate_ContainsCorrectSoapActions()
    {
        var (_, code) = GenerateClient();
        Assert.Contains("http://www.onvif.org/ver10/device/wsdl/GetDeviceInformation", code);
        Assert.Contains("http://www.onvif.org/ver10/device/wsdl/SystemReboot", code);
        Assert.Contains("http://www.onvif.org/ver10/device/wsdl/GetServices", code);
    }

    [Fact]
    public void Generate_ContainsXElementBodyConstruction()
    {
        var (_, code) = GenerateClient();
        Assert.Contains("new XElement(XName.Get(", code);
    }

    [Fact]
    public void Generate_BoolField_UsesLiteralTrueFalse()
    {
        var (_, code) = GenerateClient();
        // GetServices has bool IncludeCapability
        Assert.Contains("\"true\" : \"false\"", code);
    }

    [Fact]
    public void Generate_ContainsAllOperationMethods()
    {
        var (wsdl, code) = GenerateClient();
        foreach (var op in wsdl.Operations)
        {
            Assert.Contains($"{op.Name}Async(", code);
        }
    }

    [Fact]
    public void Generate_ContainsSoapEnvelopeConstruction()
    {
        var (_, code) = GenerateClient();
        Assert.Contains("SoapNs + \"Envelope\"", code);
        Assert.Contains("SoapNs + \"Body\"", code);
    }

    [Fact]
    public void Generate_OptionalFields_UseNullCheck()
    {
        var (_, code) = GenerateClient();
        // SetSystemDateAndTime has optional fields
        Assert.Contains("SetSystemDateAndTimeAsync(", code);
        // Optional fields should generate null-check code
        Assert.Contains("if (", code);
        Assert.Contains("!= null)", code);
    }

    [Fact]
    public void Generate_MultipleFields_UseForeachLoop()
    {
        var (_, code) = GenerateClient();
        // DeleteUsers, SetScopes etc. have unbounded fields
        Assert.Contains("foreach (var item in ", code);
    }
}
