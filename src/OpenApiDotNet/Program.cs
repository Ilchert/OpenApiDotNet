using System.CommandLine;
using System.CommandLine.Completions;
using Microsoft.OpenApi.Readers;
using OpenApiDotNet;

var openApiFileArgument = new Argument<FileInfo>("openapi-file")
{
    Description = "Path to the OpenAPI specification file (JSON or YAML)"
};
openApiFileArgument.CompletionSources.Add(ctx =>
{
    var pattern = ctx.WordToComplete;
    var directory = Path.GetDirectoryName(pattern);
    if (string.IsNullOrEmpty(directory))
        directory = ".";

    if (!Directory.Exists(directory))
        return [];

    return Directory.EnumerateFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
        .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                  || f.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
                  || f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase))
        .Select(f => new CompletionItem(f));
});

var outputOption = new Option<DirectoryInfo>("--output")
{
    Description = "Directory where generated code will be placed",
    DefaultValueFactory = _ => new DirectoryInfo("./Generated")
};
outputOption.Aliases.Add("-o");

var namespaceOption = new Option<string>("--namespace")
{
    Description = "Namespace for generated code",
    DefaultValueFactory = _ => "GeneratedClient"
};
namespaceOption.Aliases.Add("-n");

var rootCommand = new RootCommand("OpenAPI Client Generator — generates strongly-typed C# HTTP clients from OpenAPI specifications")
{
    openApiFileArgument,
    outputOption,
    namespaceOption,
};

rootCommand.SetAction(parseResult =>
{
    var openApiFile = parseResult.GetValue(openApiFileArgument)!;
    var outputDirectory = parseResult.GetValue(outputOption)!;
    var namespaceName = parseResult.GetValue(namespaceOption)!;
    Generate(openApiFile, outputDirectory, namespaceName);
});

return rootCommand.Parse(args).Invoke();

static void Generate(FileInfo openApiFile, DirectoryInfo outputDirectory, string namespaceName)
{
    if (!openApiFile.Exists)
    {
        Console.Error.WriteLine($"Error: File '{openApiFile.FullName}' not found.");
        return;
    }

    try
    {
        Console.WriteLine($"Reading OpenAPI specification from: {openApiFile.FullName}");
        Console.WriteLine();

        using var stream = openApiFile.OpenRead();
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
        Console.WriteLine($"Output: {outputDirectory.FullName}");
        Console.WriteLine($"Namespace: {namespaceName}");
        Console.WriteLine();

        Console.WriteLine("Generating client code...");
        Console.WriteLine();

        var generator = new ClientGenerator(openApiDocument, namespaceName, outputDirectory.FullName);
        generator.Generate();

        Console.WriteLine();
        Console.WriteLine("✓ Client generation complete!");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Console.Error.WriteLine(ex.StackTrace);
    }
}

