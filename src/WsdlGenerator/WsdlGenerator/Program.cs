using WsdlGenerator.Generator;
using WsdlGenerator.Parser;

if (args.Length == 0)
{
    Console.WriteLine("Usage: WsdlGenerator <wsdl-file> [--output-dir <dir>] [--namespace <ns>]");
    Console.WriteLine();
    Console.WriteLine("  <wsdl-file>         Path to the .wsdl file");
    Console.WriteLine("  --output-dir <dir>  Output directory (default: current directory)");
    Console.WriteLine("  --namespace <ns>    C# namespace for generated code (default: Generated)");
    return 1;
}

string wsdlPath = args[0];
string outputDir = ".";
string namespaceName = "Generated";

for (int i = 1; i < args.Length - 1; i++)
{
    if (args[i] == "--output-dir") outputDir = args[++i];
    else if (args[i] == "--namespace") namespaceName = args[++i];
}

if (!File.Exists(wsdlPath))
{
    Console.Error.WriteLine($"Error: File not found: {wsdlPath}");
    return 1;
}

try
{
    Console.WriteLine($"Parsing {wsdlPath}...");
    var parser = new WsdlParser();
    var wsdl = parser.Parse(wsdlPath);

    Console.WriteLine($"  Service: {wsdl.ServiceName}");
    Console.WriteLine($"  Operations: {wsdl.Operations.Count}");
    Console.WriteLine($"  Schema elements: {wsdl.Elements.Count}");

    Directory.CreateDirectory(outputDir);

    string clientFile = Path.Combine(outputDir, $"{wsdl.ServiceName}Client.cs");
    string serverFile = Path.Combine(outputDir, $"{wsdl.ServiceName}ServiceBase.cs");

    Console.WriteLine($"Generating {clientFile}...");
    var clientGen = new ClientGenerator();
    File.WriteAllText(clientFile, clientGen.Generate(wsdl, namespaceName));

    Console.WriteLine($"Generating {serverFile}...");
    var serverGen = new ServerGenerator();
    File.WriteAllText(serverFile, serverGen.Generate(wsdl, namespaceName));

    Console.WriteLine("Done.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
