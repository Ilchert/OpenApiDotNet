using Microsoft.OpenApi.Readers;
using OpenApiDotNet;

if (args.Length == 0)
{
    Console.WriteLine("OpenAPI Client Generator");
    Console.WriteLine("========================");
    Console.WriteLine();
    Console.WriteLine("Usage: OpenApiDotNet <openapi-file-path> [output-directory] [namespace]");
    Console.WriteLine();
    Console.WriteLine("Arguments:");
    Console.WriteLine("  openapi-file-path   Path to the OpenAPI specification file (JSON or YAML)");
    Console.WriteLine("  output-directory    Directory where generated code will be placed (default: ./Generated)");
    Console.WriteLine("  namespace           Namespace for generated code (default: GeneratedClient)");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  OpenApiDotNet api.yaml");
    Console.WriteLine("  OpenApiDotNet swagger.json ./src/Client MyApi.Client");
    return;
}

var openApiFilePath = args[0];
var outputDirectory = args.Length > 1 ? args[1] : "./Generated";
var namespaceName = args.Length > 2 ? args[2] : "GeneratedClient";

if (!File.Exists(openApiFilePath))
{
    Console.Error.WriteLine($"Error: File '{openApiFilePath}' not found.");
    return;
}

try
{
    Console.WriteLine($"Reading OpenAPI specification from: {openApiFilePath}");
    Console.WriteLine();

    using var stream = File.OpenRead(openApiFilePath);
    var reader = new OpenApiStreamReader();
    var openApiDocument = reader.Read(stream, out var diagnostic);

    if (diagnostic.Errors.Count > 0)
    {
        Console.Error.WriteLine("Errors found in OpenAPI document:");
        foreach (var error in diagnostic.Errors)
        {
            Console.Error.WriteLine($"  - {error.Message}");
        }
        return;
    }

    if (diagnostic.Warnings.Count > 0)
    {
        Console.WriteLine("Warnings:");
        foreach (var warning in diagnostic.Warnings)
        {
            Console.WriteLine($"  - {warning.Message}");
        }
        Console.WriteLine();
    }

    Console.WriteLine($"Title: {openApiDocument.Info.Title}");
    Console.WriteLine($"Version: {openApiDocument.Info.Version}");
    Console.WriteLine($"Output: {Path.GetFullPath(outputDirectory)}");
    Console.WriteLine($"Namespace: {namespaceName}");
    Console.WriteLine();

    Console.WriteLine("Generating client code...");
    Console.WriteLine();

    var generator = new ClientGenerator(openApiDocument, namespaceName, outputDirectory);
    generator.Generate();

    Console.WriteLine();
    Console.WriteLine("✓ Client generation complete!");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
}

