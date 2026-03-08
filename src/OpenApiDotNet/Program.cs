using System.Buffers;
using System.CommandLine;
using System.CommandLine.Completions;
using System.Text.Json;
using BinkyLabs.OpenApi.Overlays;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using OpenApiDotNet;
using OpenApiDotNet.IO;

// default generate command
var openApiFileArgument = new Argument<FileInfo>("openapi-file")
{
    Description = "Path to the OpenAPI specification file (JSON or YAML)",
    CompletionSources = { JsonYamlCompletion }
}.AcceptExistingOnly();

var outputOption = new Option<DirectoryInfo>("--output", "-o")
{
    Description = "Directory where generated code will be placed",
    DefaultValueFactory = _ => new DirectoryInfo("./Generated"),
}.AcceptLegalFilePathsOnly();

var namespaceOption = new Option<string>("--namespace", "-n")
{
    Description = "Namespace for generated code",
    DefaultValueFactory = _ => "GeneratedClient"
};

var namespacePrefixOption = new Option<string?>("--namespace-prefix", "-p")
{
    Description = "Strip this dotted prefix from schema names when generating namespaces (e.g. 'Commerce' turns 'Commerce.Order' into 'Order')"
};

var clientNameOption = new Option<string?>("--client-name", "-c")
{
    Description = "Custom name for the generated client class (default: derived from API title)"
};

var overlayOption = new Option<FileInfo[]>("--overlay")
{
    Description = "Path to overlay file(s) to apply before generation. Can be specified multiple times.",
    Arity = ArgumentArity.ZeroOrMore,
    CompletionSources = { JsonYamlCompletion }
}.AcceptExistingOnly();

var rootCommand = new RootCommand
{
    openApiFileArgument,
    outputOption,
    namespaceOption,
    namespacePrefixOption,
    clientNameOption,
    overlayOption,
};

rootCommand.SetAction(async parseResult =>
{
    var openApiFile = parseResult.GetValue(openApiFileArgument)!;
    var outputDirectory = parseResult.GetValue(outputOption)!;
    var namespaceName = parseResult.GetValue(namespaceOption)!;
    var namespacePrefix = parseResult.GetValue(namespacePrefixOption);
    var clientName = parseResult.GetValue(clientNameOption);
    var overlayFiles = parseResult.GetValue(overlayOption) ?? [];
    await Generate(openApiFile, outputDirectory, namespaceName, namespacePrefix, clientName, overlayFiles, null);
});


// update comand config
var updateConfigArgument = new Argument<FileInfo>("config-file")
{
    Description = $"Path to the {GenerationConfig.FileName} configuration file",
    DefaultValueFactory = _ => new FileInfo(GenerationConfig.FileName)
}.AcceptExistingOnly();

var updateCommand = new Command("update", "Re-generate client code using a previously saved configuration file")
{
    updateConfigArgument
};

updateCommand.SetAction(async parseResult =>
{
    var configFile = parseResult.GetValue(updateConfigArgument)!;
    await Update(configFile);
});

rootCommand.Subcommands.Add(updateCommand);

var convertOutputArgument = new Argument<FileInfo>("output-file")
{
    Description = "Path for the converted output file"
}.AcceptLegalFileNamesOnly();

var versionOption = new Option<string>("--version", "-v")
{
    Description = "Target OpenAPI specification version",
    DefaultValueFactory = _ => "3.2"
}.AcceptOnlyFromAmong("2.0", "3.0", "3.1", "3.2");

var formatOption = new Option<string>("--format", "-f")
{
    Description = "Output format",
    DefaultValueFactory = _ => "json"
}.AcceptOnlyFromAmong("json", "yaml");

var convertCommand = new Command("convert", "Convert an OpenAPI specification to a specific version and format")
{
    openApiFileArgument,
    convertOutputArgument,
    versionOption,
    formatOption,
};

convertCommand.SetAction(async parseResult =>
{
    var inputFile = parseResult.GetValue(openApiFileArgument)!;
    var outputFile = parseResult.GetValue(convertOutputArgument)!;
    var version = parseResult.GetValue(versionOption)!;
    var format = parseResult.GetValue(formatOption)!;
    await Convert(inputFile, outputFile, version, format);
});

rootCommand.Subcommands.Add(convertCommand);

return rootCommand.Parse(args).Invoke();

static IEnumerable<string> JsonYamlCompletion(CompletionContext ctx)
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
                  || f.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));
}

static async Task Generate(FileInfo openApiFile, DirectoryInfo outputDirectory, string namespaceName, string? namespacePrefix, string? clientName, FileInfo[] overlayFiles, Dictionary<string, string>? typeMappings)
{
    Console.WriteLine($"Reading OpenAPI specification from: {openApiFile.FullName}");
    Console.WriteLine();

    var settings = new OpenApiReaderSettings();
    settings.AddYamlReader();

    var openApiDocument = overlayFiles.Length > 0
         ? await ApplyOverlays(openApiFile, overlayFiles, settings)
         : await OpenApiDocument(openApiFile, settings);

    Console.WriteLine($"Title: {openApiDocument.Info.Title}");
    Console.WriteLine($"Version: {openApiDocument.Info.Version}");
    Console.WriteLine($"Output: {outputDirectory.FullName}");
    Console.WriteLine($"Namespace: {namespaceName}");
    if (namespacePrefix != null)
        Console.WriteLine($"Namespace prefix: {namespacePrefix}");
    if (clientName != null)
        Console.WriteLine($"Client name: {clientName}");
    Console.WriteLine();

    Console.WriteLine("Generating client code...");
    Console.WriteLine();

    Directory.CreateDirectory(outputDirectory.FullName);
    using var outputProvider = new PhysicalWritableFileProvider(outputDirectory.FullName);
    var generator = new OpenApiGenerator(openApiDocument, namespaceName, outputProvider, namespacePrefix, clientName, new TypeMappingConfig(typeMappings));
    var generatedFiles = generator.Generate();

    SaveConfig(openApiFile, outputDirectory, namespaceName, namespacePrefix, clientName, overlayFiles, typeMappings, generatedFiles);

    Console.WriteLine();
    Console.WriteLine("✓ Client generation complete!");
}

static async Task Update(FileInfo configFile)
{
    var json = File.ReadAllText(configFile.FullName);
    var config = JsonSerializer.Deserialize<GenerationConfig>(json);

    if (config is null)
    {
        Console.Error.WriteLine("Error: Failed to read configuration file.");
        return;
    }

    var baseDirectory = Path.GetDirectoryName(configFile.FullName) ?? ".";
    var openApiFilePath = Path.IsPathRooted(config.OpenApiFile)
        ? config.OpenApiFile
        : Path.GetFullPath(Path.Combine(baseDirectory, config.OpenApiFile));
    var outputDirectoryPath = Path.IsPathRooted(config.OutputDirectory)
        ? config.OutputDirectory
        : Path.GetFullPath(Path.Combine(baseDirectory, config.OutputDirectory));

    var overlayFiles = config.OverlayFiles
        .Select(o => Path.IsPathRooted(o) ? o : Path.GetFullPath(Path.Combine(baseDirectory, o)))
        .Select(o => new FileInfo(o))
        .ToArray();

    var previousFiles = config.GeneratedFiles;

    Console.WriteLine($"Updating from configuration: {configFile.FullName}");
    await Generate(new FileInfo(openApiFilePath), new DirectoryInfo(outputDirectoryPath), config.Namespace, config.NamespacePrefix, config.ClientName, overlayFiles, config.TypeMappings);

    // Read the updated config to get the new list of generated files
    var updatedJson = File.ReadAllText(configFile.FullName);
    var updatedConfig = JsonSerializer.Deserialize<GenerationConfig>(updatedJson);
    var currentFiles = updatedConfig?.GeneratedFiles;

    CleanupRemovedFiles(outputDirectoryPath, previousFiles, currentFiles);
}

static void SaveConfig(FileInfo openApiFile, DirectoryInfo outputDirectory, string namespaceName, string? namespacePrefix, string? clientName, FileInfo[] overlayFiles, Dictionary<string, string>? typeMappings, List<string> generatedFiles)
{
    var config = new GenerationConfig
    {
        OpenApiFile = Path.GetRelativePath(outputDirectory.FullName, openApiFile.FullName),
        OutputDirectory = ".",
        Namespace = namespaceName,
        NamespacePrefix = namespacePrefix,
        ClientName = clientName,
        OverlayFiles = overlayFiles.Select(f => Path.GetRelativePath(outputDirectory.FullName, f.FullName)).ToList(),
        TypeMappings = typeMappings,
        GeneratedFiles = generatedFiles
    };

    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    var json = JsonSerializer.Serialize(config, jsonOptions);
    var configPath = Path.Combine(outputDirectory.FullName, GenerationConfig.FileName);
    File.WriteAllText(configPath, json);
    Console.WriteLine($"  Saved configuration: {GenerationConfig.FileName}");
}

static void CleanupRemovedFiles(string outputDirectory, List<string>? previousFiles, List<string>? currentFiles)
{
    if (previousFiles is null or [])
        return;

    var currentSet = new HashSet<string>(currentFiles ?? [], StringComparer.OrdinalIgnoreCase);
    var removedFiles = previousFiles.Where(f => !currentSet.Contains(f)).ToList();

    if (removedFiles.Count == 0)
        return;

    Directory.CreateDirectory(outputDirectory);
    using var outputProvider = new PhysicalWritableFileProvider(outputDirectory);

    Console.WriteLine();
    Console.WriteLine("Cleaning up removed files:");
    foreach (var relativePath in removedFiles)
    {
        var fileInfo = outputProvider.GetFileInfo(relativePath);
        if (fileInfo.Exists)
        {
            fileInfo.Delete();
            Console.WriteLine($"  Deleted: {relativePath}");

            // Remove empty parent directories up to the output directory
            var directory = Path.GetDirectoryName(relativePath);
            while (!string.IsNullOrEmpty(directory))
            {
                var dirContents = outputProvider.GetDirectoryContents(directory);
                if (dirContents.Exists && !dirContents.Any())
                {
                    outputProvider.GetFileInfo(directory).Delete();
                    Console.WriteLine($"  Removed empty directory: {directory}");
                    directory = Path.GetDirectoryName(directory);
                }
                else
                {
                    break;
                }
            }
        }
    }
}

static async Task Convert(FileInfo inputFile, FileInfo outputFile, string version, string format)
{
    Console.WriteLine($"Reading OpenAPI specification from: {inputFile.FullName}");

    var settings = new OpenApiReaderSettings();
    settings.AddYamlReader();

    var openApiDocument = await OpenApiDocument(inputFile, settings);

    var specVersion = version switch
    {
        "2.0" => OpenApiSpecVersion.OpenApi2_0,
        "3.0" => OpenApiSpecVersion.OpenApi3_0,
        "3.1" => OpenApiSpecVersion.OpenApi3_1,
        "3.2" => OpenApiSpecVersion.OpenApi3_2,
        _ => throw new ArgumentException($"Unsupported OpenAPI version: {version}")
    };

    var outputDirectory = Path.GetDirectoryName(outputFile.FullName);
    if (!string.IsNullOrEmpty(outputDirectory))
        Directory.CreateDirectory(outputDirectory);

    await using var outStream = outputFile.Create();
    await openApiDocument.SerializeAsync(outStream, specVersion, format, default);

    Console.WriteLine($"✓ Converted to OpenAPI {version} ({format}): {outputFile.FullName}");
}

static async Task<OpenApiDocument> ApplyOverlays(FileInfo openApiFile, FileInfo[] overlayFiles, OpenApiReaderSettings settings)
{
    var readerSettings = new OverlayReaderSettings() { OpenApiSettings = settings };

    var overlayDocument = new OverlayDocument();

    foreach (var overlayFile in overlayFiles)
    {
        var (overlay, diagnostic) = await OverlayDocument.LoadFromUrlAsync(overlayFile.FullName, readerSettings);
        if (diagnostic?.Errors.Count > 0)
        {
            Console.Error.WriteLine($"Errors found in overlay '{overlayFile.FullName}':");
            foreach (var error in diagnostic.Errors)
                Console.Error.WriteLine($"  - {error.Message}");
        }

        if (diagnostic?.Warnings.Count > 0)
        {
            Console.WriteLine($"Warnings found in overlay '{overlayFile.FullName}':");
            foreach (var warning in diagnostic.Warnings)
                Console.WriteLine($"  - {warning.Message}");
        }

        Console.WriteLine();
        if (overlay != null)
            overlayDocument = overlayDocument.CombineWith(overlay);
    }

    var (result, overlayDiagnostic) = await overlayDocument.ApplyToDocumentAndLoadAsync(openApiFile.FullName, readerSettings: new OverlayReaderSettings() { OpenApiSettings = settings });

    if (overlayDiagnostic?.Errors.Count > 0)
    {
        Console.Error.WriteLine("Errors found after applying overlays:");
        foreach (var error in overlayDiagnostic.Errors)
            Console.Error.WriteLine($"  - {error.Message}");
    }

    if (overlayDiagnostic?.Warnings.Count > 0)
    {
        Console.WriteLine("Warnings:");
        foreach (var warning in overlayDiagnostic.Warnings)
            Console.WriteLine($"  - {warning.Message}");
        Console.WriteLine();
    }

    return result ?? throw new InvalidOperationException("Can not load document after applying overlays");
}

static async Task<OpenApiDocument> OpenApiDocument(FileInfo openApiFile, OpenApiReaderSettings settings)
{
    var (document, diagnostic) = await Microsoft.OpenApi.OpenApiDocument.LoadAsync(openApiFile.FullName, settings: settings);

    if (diagnostic?.Errors.Count > 0)
    {
        Console.Error.WriteLine("Errors found in OpenAPI document:");
        foreach (var error in diagnostic.Errors)
            Console.Error.WriteLine($"  - {error.Message}");
    }

    if (diagnostic?.Warnings.Count > 0)
    {
        Console.WriteLine("Warnings:");
        foreach (var warning in diagnostic.Warnings)
            Console.WriteLine($"  - {warning.Message}");
        Console.WriteLine();
    }
    return document ?? throw new InvalidOperationException($"Can not load document from file {openApiFile.FullName}");

}
