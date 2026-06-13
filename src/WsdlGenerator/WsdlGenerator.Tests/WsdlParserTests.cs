using WsdlGenerator.Parser;
using Xunit;

namespace WsdlGenerator.Tests;

public class WsdlParserTests
{
    private static readonly string WsdlPath =
        Path.Combine(AppContext.BaseDirectory, "devicemgmt.wsdl");

    private static WsdlDefinition ParseWsdl() => new WsdlParser().Parse(WsdlPath);

    [Fact]
    public void Parse_ExtractsTargetNamespace()
    {
        var wsdl = ParseWsdl();
        Assert.Equal("http://www.onvif.org/ver10/device/wsdl", wsdl.TargetNamespace);
    }

    [Fact]
    public void Parse_ExtractsServiceName()
    {
        var wsdl = ParseWsdl();
        Assert.Equal("Device", wsdl.ServiceName);
    }

    [Fact]
    public void Parse_ExtractsOperations_CountIsReasonable()
    {
        var wsdl = ParseWsdl();
        Assert.True(wsdl.Operations.Count >= 50,
            $"Expected at least 50 operations, got {wsdl.Operations.Count}");
    }

    [Fact]
    public void Parse_GetServicesOperation_HasCorrectSoapAction()
    {
        var wsdl = ParseWsdl();
        var op = wsdl.Operations.Single(o => o.Name == "GetServices");
        Assert.Equal("http://www.onvif.org/ver10/device/wsdl/GetServices", op.SoapAction);
    }

    [Fact]
    public void Parse_GetServicesOperation_HasCorrectInputElement()
    {
        var wsdl = ParseWsdl();
        var op = wsdl.Operations.Single(o => o.Name == "GetServices");
        Assert.Equal("GetServices", op.InputElementName);
        Assert.Equal("GetServicesResponse", op.OutputElementName);
    }

    [Fact]
    public void Parse_GetDeviceInformationOperation_Exists()
    {
        var wsdl = ParseWsdl();
        var op = wsdl.Operations.Single(o => o.Name == "GetDeviceInformation");
        Assert.Equal("http://www.onvif.org/ver10/device/wsdl/GetDeviceInformation", op.SoapAction);
        Assert.Equal("GetDeviceInformation", op.InputElementName);
        Assert.Equal("GetDeviceInformationResponse", op.OutputElementName);
    }

    [Fact]
    public void Parse_GetDeviceInformation_InputElement_HasNoFields()
    {
        var wsdl = ParseWsdl();
        Assert.True(wsdl.Elements.ContainsKey("GetDeviceInformation"),
            "Expected 'GetDeviceInformation' element in schema");
        var elem = wsdl.Elements["GetDeviceInformation"];
        Assert.Empty(elem.Fields);
    }

    [Fact]
    public void Parse_GetDeviceInformationResponse_HasFiveStringFields()
    {
        var wsdl = ParseWsdl();
        Assert.True(wsdl.Elements.ContainsKey("GetDeviceInformationResponse"));
        var elem = wsdl.Elements["GetDeviceInformationResponse"];
        Assert.Equal(5, elem.Fields.Count);

        var names = elem.Fields.Select(f => f.Name).ToList();
        Assert.Contains("Manufacturer", names);
        Assert.Contains("Model", names);
        Assert.Contains("FirmwareVersion", names);
        Assert.Contains("SerialNumber", names);
        Assert.Contains("HardwareId", names);
    }

    [Fact]
    public void Parse_GetDeviceInformationResponse_AllFieldsAreXsString()
    {
        var wsdl = ParseWsdl();
        var elem = wsdl.Elements["GetDeviceInformationResponse"];
        Assert.All(elem.Fields, f => Assert.Equal("xs:string", f.XsdType));
    }

    [Fact]
    public void Parse_GetDeviceInformationResponse_NoFieldIsOptional()
    {
        var wsdl = ParseWsdl();
        var elem = wsdl.Elements["GetDeviceInformationResponse"];
        Assert.All(elem.Fields, f => Assert.False(f.IsOptional));
        Assert.All(elem.Fields, f => Assert.False(f.IsMultiple));
    }

    [Fact]
    public void Parse_GetServices_InputElement_HasBooleanField()
    {
        var wsdl = ParseWsdl();
        Assert.True(wsdl.Elements.ContainsKey("GetServices"));
        var elem = wsdl.Elements["GetServices"];
        Assert.Single(elem.Fields);
        var field = elem.Fields[0];
        Assert.Equal("IncludeCapability", field.Name);
        Assert.Equal("xs:boolean", field.XsdType);
        Assert.False(field.IsOptional);
    }

    [Fact]
    public void Parse_GetServicesResponse_HasMultipleServiceField()
    {
        var wsdl = ParseWsdl();
        Assert.True(wsdl.Elements.ContainsKey("GetServicesResponse"));
        var elem = wsdl.Elements["GetServicesResponse"];
        Assert.Single(elem.Fields);
        var field = elem.Fields[0];
        Assert.Equal("Service", field.Name);
        Assert.True(field.IsMultiple);
    }

    [Fact]
    public void Parse_DeleteUsers_HasMultipleStringUsernameField()
    {
        var wsdl = ParseWsdl();
        Assert.True(wsdl.Elements.ContainsKey("DeleteUsers"));
        var elem = wsdl.Elements["DeleteUsers"];
        Assert.Single(elem.Fields);
        var field = elem.Fields[0];
        Assert.Equal("Username", field.Name);
        Assert.Equal("xs:string", field.XsdType);
        Assert.True(field.IsMultiple);
    }

    [Fact]
    public void Parse_SetSystemDateAndTime_HasOptionalFields()
    {
        var wsdl = ParseWsdl();
        Assert.True(wsdl.Elements.ContainsKey("SetSystemDateAndTime"));
        var elem = wsdl.Elements["SetSystemDateAndTime"];
        Assert.True(elem.Fields.Count >= 2);
        // TimeZone and UTCDateTime are optional (minOccurs="0")
        var optionalFields = elem.Fields.Where(f => f.IsOptional).ToList();
        Assert.True(optionalFields.Count >= 2,
            $"Expected at least 2 optional fields, got {optionalFields.Count}");
    }

    [Fact]
    public void Parse_SetHostname_HasStringField()
    {
        var wsdl = ParseWsdl();
        Assert.True(wsdl.Elements.ContainsKey("SetHostname"));
        var elem = wsdl.Elements["SetHostname"];
        Assert.Single(elem.Fields);
        Assert.Equal("Name", elem.Fields[0].Name);
        // xs:token maps to string
        Assert.Equal("xs:token", elem.Fields[0].XsdType);
    }

    [Fact]
    public void Parse_SystemReboot_InputHasNoFields()
    {
        var wsdl = ParseWsdl();
        Assert.True(wsdl.Elements.ContainsKey("SystemReboot"));
        Assert.Empty(wsdl.Elements["SystemReboot"].Fields);
    }

    [Fact]
    public void Parse_AllOperations_HaveSoapActions()
    {
        var wsdl = ParseWsdl();
        var missing = wsdl.Operations.Where(o => string.IsNullOrEmpty(o.SoapAction)).ToList();
        Assert.Empty(missing);
    }

    [Fact]
    public void Parse_SoapActions_AllStartWithExpectedPrefix()
    {
        var wsdl = ParseWsdl();
        Assert.All(wsdl.Operations, op =>
            Assert.StartsWith("http://www.onvif.org/ver10/device/wsdl/", op.SoapAction));
    }

    [Fact]
    public void Parse_Namespaces_ContainsTdsPrefix()
    {
        var wsdl = ParseWsdl();
        Assert.True(wsdl.Namespaces.ContainsKey("tds"));
        Assert.Equal("http://www.onvif.org/ver10/device/wsdl", wsdl.Namespaces["tds"]);
    }

    [Fact]
    public void Parse_Elements_ContainsAllKnownElements()
    {
        var wsdl = ParseWsdl();
        string[] expected =
        [
            "GetServices", "GetServicesResponse",
            "GetDeviceInformation", "GetDeviceInformationResponse",
            "SystemReboot", "SystemRebootResponse",
            "GetScopes", "GetScopesResponse",
            "SetHostname", "SetHostnameResponse",
        ];
        foreach (var name in expected)
            Assert.True(wsdl.Elements.ContainsKey(name), $"Missing element: {name}");
    }
}
