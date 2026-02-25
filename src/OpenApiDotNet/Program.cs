using System.CommandLine;
using System.CommandLine.Completions;
using System.Text.Json;
using BinkyLabs.OpenApi.Overlays;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
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

var namespacePrefixOption = new Option<string?>("--namespace-prefix")
{
    Description = "Strip this dotted prefix from schema names when generating namespaces (e.g. 'Commerce' turns 'Commerce.Order' into 'Order')"
};
namespacePrefixOption.Aliases.Add("-p");

var overlayOption = new Option<FileInfo[]>("--overlay")
{
    Description = "Path to overlay file(s) to apply before generation. Can be specified multiple times.",
    Arity = ArgumentArity.ZeroOrMore
};
overlayOption.CompletionSources.Add(ctx =>
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

var rootCommand = new RootCommand
{
    openApiFileArgument,
    outputOption,
    namespaceOption,
    namespacePrefixOption,
    overlayOption,
};

rootCommand.SetAction(async parseResult =>
{
    var openApiFile = parseResult.GetValue(openApiFileArgument)!;
    var outputDirectory = parseResult.GetValue(outputOption)!;
    var namespaceName = parseResult.GetValue(namespaceOption)!;
    var namespacePrefix = parseResult.GetValue(namespacePrefixOption);
    var overlayFiles = parseResult.GetValue(overlayOption) ?? [];
    await Generate(openApiFile, outputDirectory, namespaceName, namespacePrefix, overlayFiles, null);
});

var updateConfigArgument = new Argument<FileInfo>("config-file")
{
    Description = $"Path to the {GenerationConfig.FileName} configuration file"
};
updateConfigArgument.DefaultValueFactory = _ => new FileInfo(GenerationConfig.FileName);

var updateCommand = new Command("update", "Re-generate client code using a previously saved configuration file")
{
    updateConfigArgument
};

updateCommand.SetAction(parseResult =>
{
    var configFile = parseResult.GetValue(updateConfigArgument)!;
    Update(configFile);
});

rootCommand.Subcommands.Add(updateCommand);

var convertOutputArgument = new Argument<FileInfo>("output-file")
{
    Description = "Path for the converted output file"
};

var versionOption = new Option<string>("--version")
{
    Description = "Target OpenAPI specification version"
};
versionOption.Aliases.Add("-v");
versionOption.DefaultValueFactory = _ => "3.2";
versionOption.AcceptOnlyFromAmong("2.0", "3.0", "3.1", "3.2");

var formatOption = new Option<string>("--format")
{
    Description = "Output format"
};
formatOption.Aliases.Add("-f");
formatOption.DefaultValueFactory = _ => "json";
formatOption.AcceptOnlyFromAmong("json", "yaml");

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

static async Task Generate(FileInfo openApiFile, DirectoryInfo outputDirectory, string namespaceName, string? namespacePrefix, FileInfo[] overlayFiles, Dictionary<string, string>? typeMappings)
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

        OpenApiDocument openApiDocument;
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();

        if (overlayFiles.Length > 0)
        {
            foreach (var overlayFile in overlayFiles)
            {
                if (!overlayFile.Exists)
                {
                    Console.Error.WriteLine($"Error: Overlay file '{overlayFile.FullName}' not found.");
                    return;
                }
                Console.WriteLine($"Applying overlay: {overlayFile.FullName}");
            }

            var (firstOverlay, _) = await OverlayDocument.LoadFromUrlAsync(overlayFiles[0].FullName);
            for (int i = 1; i < overlayFiles.Length; i++)
            {
                var (nextOverlay, _) = await OverlayDocument.LoadFromUrlAsync(overlayFiles[i].FullName);
                firstOverlay = firstOverlay.CombineWith(nextOverlay);
            }

            var (result, overlayDiagnostic) = await firstOverlay.ApplyToDocumentAndLoadAsync(openApiFile.FullName, readerSettings: new OverlayReaderSettings() { OpenApiSettings = settings });
            openApiDocument = result;

            if (overlayDiagnostic?.Errors.Count > 0)
            {
                Console.Error.WriteLine("Errors found after applying overlays:");
                foreach (var error in overlayDiagnostic.Errors)
                {
                    Console.Error.WriteLine($"  - {error.Message}");
                }
            }

            if (overlayDiagnostic?.Warnings.Count > 0)
            {
                Console.WriteLine("Warnings:");
                foreach (var warning in overlayDiagnostic.Warnings)
                {
                    Console.WriteLine($"  - {warning.Message}");
                }
                Console.WriteLine();
            }
        }
        else
        {
            using var stream = openApiFile.OpenRead();

            var (result, diagnostic) = await OpenApiDocument.LoadAsync(stream, settings: settings);
            openApiDocument = result;

            if (diagnostic?.Errors.Count > 0)
            {
                Console.Error.WriteLine("Errors found in OpenAPI document:");
                foreach (var error in diagnostic.Errors)
                {
                    Console.Error.WriteLine($"  - {error.Message}");
                }
            }

            if (diagnostic?.Warnings.Count > 0)
            {
                Console.WriteLine("Warnings:");
                foreach (var warning in diagnostic.Warnings)
                {
                    Console.WriteLine($"  - {warning.Message}");
                }
                Console.WriteLine();
            }
        }

        Console.WriteLine($"Title: {openApiDocument.Info.Title}");
        Console.WriteLine($"Version: {openApiDocument.Info.Version}");
        Console.WriteLine($"Output: {outputDirectory.FullName}");
        Console.WriteLine($"Namespace: {namespaceName}");
        if (namespacePrefix != null)
            Console.WriteLine($"Namespace prefix: {namespacePrefix}");
        Console.WriteLine();

        Console.WriteLine("Generating client code...");
        Console.WriteLine();

        var generator = new ClientGenerator(openApiDocument, namespaceName, outputDirectory.FullName, namespacePrefix, new TypeMappingConfig(typeMappings));
        generator.Generate();

        SaveConfig(openApiFile, outputDirectory, namespaceName, namespacePrefix, overlayFiles, typeMappings);

        Console.WriteLine();
        Console.WriteLine("✓ Client generation complete!");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Console.Error.WriteLine(ex.StackTrace);
    }
}

static void Update(FileInfo configFile)
{
    if (!configFile.Exists)
    {
        Console.Error.WriteLine($"Error: Configuration file '{configFile.FullName}' not found.");
        Console.Error.WriteLine($"Run the generate command first to create a {GenerationConfig.FileName} file.");
        return;
    }

    try
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

        Console.WriteLine($"Updating from configuration: {configFile.FullName}");
        Generate(new FileInfo(openApiFilePath), new DirectoryInfo(outputDirectoryPath), config.Namespace, config.NamespacePrefix, overlayFiles, config.TypeMappings);
    }
    catch (JsonException ex)
    {
        Console.Error.WriteLine($"Error: Invalid configuration file — {ex.Message}");
    }
}

static void SaveConfig(FileInfo openApiFile, DirectoryInfo outputDirectory, string namespaceName, string? namespacePrefix, FileInfo[] overlayFiles, Dictionary<string, string>? typeMappings)
{
    var config = new GenerationConfig
    {
        OpenApiFile = Path.GetRelativePath(outputDirectory.FullName, openApiFile.FullName),
        OutputDirectory = ".",
        Namespace = namespaceName,
        NamespacePrefix = namespacePrefix,
        OverlayFiles = overlayFiles.Select(f => Path.GetRelativePath(outputDirectory.FullName, f.FullName)).ToList(),
        TypeMappings = typeMappings
    };

    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    var json = JsonSerializer.Serialize(config, jsonOptions);
    var configPath = Path.Combine(outputDirectory.FullName, GenerationConfig.FileName);
    File.WriteAllText(configPath, json);
    Console.WriteLine($"  Saved configuration: {GenerationConfig.FileName}");
}

static async Task Convert(FileInfo inputFile, FileInfo outputFile, string version, string format)
{
    if (!inputFile.Exists)
    {
        Console.Error.WriteLine($"Error: File '{inputFile.FullName}' not found.");
        return;
    }

    try
    {
        Console.WriteLine($"Reading OpenAPI specification from: {inputFile.FullName}");

        using var stream = inputFile.OpenRead();
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();
        var (openApiDocument, diagnostic) = await OpenApiDocument.LoadAsync(stream, settings: settings);

        if (diagnostic?.Errors.Count > 0)
        {
            Console.Error.WriteLine("Errors found in OpenAPI document:");
            foreach (var error in diagnostic.Errors)
            {
                Console.Error.WriteLine($"  - {error.Message}");
            }
        }

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

        using var fileStream = File.Create(outputFile.FullName);
        using var textWriter = new StreamWriter(fileStream);

        IOpenApiWriter writer = format switch
        {
            "yaml" => new OpenApiYamlWriter(textWriter),
            _ => new OpenApiJsonWriter(textWriter),
        };

        openApiDocument.SerializeAs(specVersion, writer);

        Console.WriteLine($"✓ Converted to OpenAPI {version} ({format}): {outputFile.FullName}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        Console.Error.WriteLine(ex.StackTrace);
    }
}

