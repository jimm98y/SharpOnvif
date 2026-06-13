using System.Xml.Linq;

namespace WsdlGenerator.Parser;

public class WsdlParser
{
    private static readonly XNamespace Wsdl = "http://schemas.xmlsoap.org/wsdl/";
    private static readonly XNamespace Soap12 = "http://schemas.xmlsoap.org/wsdl/soap12/";
    private static readonly XNamespace Soap11 = "http://schemas.xmlsoap.org/wsdl/soap/";
    private static readonly XNamespace Xs = "http://www.w3.org/2001/XMLSchema";

    public WsdlDefinition Parse(string wsdlPath)
    {
        var doc = XDocument.Load(wsdlPath);
        var root = doc.Root!;

        string targetNs = root.Attribute("targetNamespace")?.Value ?? "";
        var namespaces = ParseNamespaces(root);

        var messages = ParseMessages(root);
        var portTypeOps = ParsePortTypeOperations(root);
        var soapActions = ParseSoapActions(root);
        var operations = BuildOperations(portTypeOps, messages, soapActions);
        var elements = ParseSchemaElements(root);
        string serviceName = DeriveServiceName(root);

        return new WsdlDefinition(targetNs, serviceName, namespaces, operations, elements);
    }

    private static Dictionary<string, string> ParseNamespaces(XElement root)
    {
        var result = new Dictionary<string, string>();
        foreach (var attr in root.Attributes())
        {
            if (!attr.IsNamespaceDeclaration) continue;
            string prefix = attr.Name.LocalName == "xmlns" ? "" : attr.Name.LocalName;
            result[prefix] = attr.Value;
        }
        return result;
    }

    private static string DeriveServiceName(XElement root)
    {
        string name = root.Elements(Wsdl + "binding").FirstOrDefault()?.Attribute("name")?.Value ?? "Service";
        return name.EndsWith("Binding", StringComparison.Ordinal) ? name[..^7] : name;
    }

    // Returns: message local name → element local name
    private static Dictionary<string, string> ParseMessages(XElement root)
    {
        var result = new Dictionary<string, string>();
        foreach (var msg in root.Elements(Wsdl + "message"))
        {
            string msgName = msg.Attribute("name")?.Value ?? "";
            string elementRef = msg.Element(Wsdl + "part")?.Attribute("element")?.Value ?? "";
            string elementLocal = StripPrefix(elementRef);
            if (msgName != "" && elementLocal != "")
                result[msgName] = elementLocal;
        }
        return result;
    }

    // Returns: operationName → (inputMsgLocal, outputMsgLocal)
    private static Dictionary<string, (string Input, string Output)> ParsePortTypeOperations(XElement root)
    {
        var result = new Dictionary<string, (string, string)>();
        var portType = root.Elements(Wsdl + "portType").FirstOrDefault();
        if (portType == null) return result;

        foreach (var op in portType.Elements(Wsdl + "operation"))
        {
            string opName = op.Attribute("name")?.Value ?? "";
            string inputMsg = StripPrefix(op.Element(Wsdl + "input")?.Attribute("message")?.Value ?? "");
            string outputMsg = StripPrefix(op.Element(Wsdl + "output")?.Attribute("message")?.Value ?? "");
            if (opName != "")
                result[opName] = (inputMsg, outputMsg);
        }
        return result;
    }

    // Returns: operationName → soapAction
    private static Dictionary<string, string> ParseSoapActions(XElement root)
    {
        var result = new Dictionary<string, string>();
        var binding = root.Elements(Wsdl + "binding").FirstOrDefault();
        if (binding == null) return result;

        foreach (var op in binding.Elements(Wsdl + "operation"))
        {
            string opName = op.Attribute("name")?.Value ?? "";
            string soapAction =
                op.Element(Soap12 + "operation")?.Attribute("soapAction")?.Value ??
                op.Element(Soap11 + "operation")?.Attribute("soapAction")?.Value ?? "";
            if (opName != "" && soapAction != "")
                result[opName] = soapAction;
        }
        return result;
    }

    private static List<WsdlOperation> BuildOperations(
        Dictionary<string, (string Input, string Output)> portTypeOps,
        Dictionary<string, string> messages,
        Dictionary<string, string> soapActions)
    {
        var ops = new List<WsdlOperation>();
        foreach (var (opName, (inputMsg, outputMsg)) in portTypeOps)
        {
            messages.TryGetValue(inputMsg, out string? inputElem);
            messages.TryGetValue(outputMsg, out string? outputElem);
            soapActions.TryGetValue(opName, out string? soapAction);

            ops.Add(new WsdlOperation(
                opName,
                soapAction ?? "",
                inputElem ?? opName,
                outputElem ?? (opName + "Response")));
        }
        return ops;
    }

    private static Dictionary<string, XsdElement> ParseSchemaElements(XElement root)
    {
        var result = new Dictionary<string, XsdElement>();
        var types = root.Element(Wsdl + "types");
        if (types == null) return result;

        foreach (var schema in types.Elements(Xs + "schema"))
        {
            foreach (var elem in schema.Elements(Xs + "element"))
            {
                string name = elem.Attribute("name")?.Value ?? "";
                if (name == "") continue;
                result[name] = new XsdElement(name, ParseElementFields(elem));
            }
        }
        return result;
    }

    private static List<XsdField> ParseElementFields(XElement elem)
    {
        var complexType = elem.Element(Xs + "complexType");
        if (complexType == null) return [];

        var sequence = complexType.Element(Xs + "sequence");
        if (sequence == null) return [];

        var fields = new List<XsdField>();
        foreach (var field in sequence.Elements(Xs + "element"))
        {
            string fieldName = field.Attribute("name")?.Value ?? "";
            if (fieldName == "") continue;

            // If no type attribute but has inline complexType, treat as XElement
            string fieldType = field.Attribute("type")?.Value
                ?? (field.Element(Xs + "complexType") != null ? "xs:anyType" : "xs:anyType");

            // ref attribute → treat as complex
            if (field.Attribute("ref") != null) fieldType = "xs:anyType";

            string minOccurs = field.Attribute("minOccurs")?.Value ?? "1";
            string maxOccurs = field.Attribute("maxOccurs")?.Value ?? "1";
            bool isOptional = minOccurs == "0";
            bool isMultiple = maxOccurs == "unbounded"
                || (int.TryParse(maxOccurs, out int max) && max > 1);

            fields.Add(new XsdField(fieldName, fieldType, isOptional, isMultiple));
        }
        return fields;
    }

    private static string StripPrefix(string value)
    {
        int colon = value.IndexOf(':');
        return colon >= 0 ? value[(colon + 1)..] : value;
    }
}
