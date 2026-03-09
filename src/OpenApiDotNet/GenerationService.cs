using System.Text.Json;
using BinkyLabs.OpenApi.Overlays;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using OpenApiDotNet.IO;

namespace OpenApiDotNet;

internal class GenerationService
{
    private static readonly OpenApiReaderSettings s_openApiReaderSettings = CreateOpenApiReaderSettings();
    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new() { WriteIndented = true };

    internal static ILogger<GenerationService> Logger { get; set; } = LoggerFactory.Create(static builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    }).CreateLogger<GenerationService>();

    private static OpenApiReaderSettings CreateOpenApiReaderSettings()
    {
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();
        return settings;
    }

    public async Task<List<string>> GenerateAsync(IFileInfo openApiFile, IWritableFileProvider outputProvider, string namespaceName, string? namespacePrefix, string? clientName, IFileInfo[] overlayFiles, Dictionary<string, string>? typeMappings)
    {
        var displayPath = openApiFile.PhysicalPath ?? openApiFile.Name;
        Logger.LogInformation("Reading OpenAPI specification from: {FilePath}", displayPath);

        var openApiDocument = overlayFiles.Length > 0
             ? await ApplyOverlaysAsync(openApiFile, overlayFiles)
             : await LoadOpenApiDocumentAsync(openApiFile);

        Logger.LogInformation("Title: {Title}", openApiDocument.Info.Title);
        Logger.LogInformation("Version: {Version}", openApiDocument.Info.Version);
        Logger.LogInformation("Output: {OutputPath}", outputProvider.Root);
        Logger.LogInformation("Namespace: {Namespace}", namespaceName);
        if (namespacePrefix != null)
            Logger.LogInformation("Namespace prefix: {NamespacePrefix}", namespacePrefix);
        if (clientName != null)
            Logger.LogInformation("Client name: {ClientName}", clientName);

        Logger.LogInformation("Generating client code...");

        var generator = new OpenApiGenerator(openApiDocument, namespaceName, outputProvider, namespacePrefix, clientName, new TypeMappingConfig(typeMappings));
        var generatedFiles = generator.Generate();

        SaveConfig(openApiFile, outputProvider, namespaceName, namespacePrefix, clientName, overlayFiles, typeMappings, generatedFiles);

        Logger.LogInformation("✓ Client generation complete!");
        return generatedFiles;
    }

    public async Task UpdateAsync(FileInfo configFile)
    {
        var json = File.ReadAllText(configFile.FullName);
        var config = JsonSerializer.Deserialize<GenerationConfig>(json, s_jsonSerializerOptions);

        if (config is null)
        {
            Logger.LogError("Failed to read configuration file.");
            return;
        }

        var baseDirectory = Path.GetDirectoryName(configFile.FullName) ?? ".";
        var openApiFilePath = Path.IsPathRooted(config.OpenApiFile)
            ? config.OpenApiFile
            : Path.GetFullPath(Path.Combine(baseDirectory, config.OpenApiFile));
        var outputDirectoryPath = Path.IsPathRooted(config.OutputDirectory)
            ? config.OutputDirectory
            : Path.GetFullPath(Path.Combine(baseDirectory, config.OutputDirectory));

        var overlayFileInfos = config.OverlayFiles
            .Select(o => Path.IsPathRooted(o) ? o : Path.GetFullPath(Path.Combine(baseDirectory, o)))
            .Select(o => (IFileInfo)new PhysicalWritableFileInfo(new FileInfo(o)))
            .ToArray();

        var previousFiles = config.GeneratedFiles;

        Logger.LogInformation("Updating from configuration: {ConfigFile}", configFile.FullName);

        Directory.CreateDirectory(outputDirectoryPath);
        using var outputProvider = new PhysicalWritableFileProvider(outputDirectoryPath);
        var openApiFileInfo = new PhysicalWritableFileInfo(new FileInfo(openApiFilePath));
        var currentFiles = await GenerateAsync(openApiFileInfo, outputProvider, config.Namespace, config.NamespacePrefix, config.ClientName, overlayFileInfos, config.TypeMappings);

        CleanupRemovedFiles(outputProvider, previousFiles, currentFiles);
    }

    public async Task ConvertAsync(IFileInfo inputFile, IWritableFileInfo outputFile, string version, string format)
    {
        var displayPath = inputFile.PhysicalPath ?? inputFile.Name;
        Logger.LogInformation("Reading OpenAPI specification from: {FilePath}", displayPath);

        var openApiDocument = await LoadOpenApiDocumentAsync(inputFile);

        var specVersion = version switch
        {
            "2.0" => OpenApiSpecVersion.OpenApi2_0,
            "3.0" => OpenApiSpecVersion.OpenApi3_0,
            "3.1" => OpenApiSpecVersion.OpenApi3_1,
            "3.2" => OpenApiSpecVersion.OpenApi3_2,
            _ => throw new ArgumentException($"Unsupported OpenAPI version: {version}")
        };

        await using var outStream = outputFile.CreateWriteStream();
        await openApiDocument.SerializeAsync(outStream, specVersion, format, default);

        Logger.LogInformation("✓ Converted to OpenAPI {Version} ({Format}): {OutputFile}", version, format, outputFile.PhysicalPath ?? outputFile.Name);
    }

    private void SaveConfig(IFileInfo openApiFile, IWritableFileProvider outputProvider, string namespaceName, string? namespacePrefix, string? clientName, IFileInfo[] overlayFiles, Dictionary<string, string>? typeMappings, List<string> generatedFiles)
    {
        var root = outputProvider.Root;
        var config = new GenerationConfig
        {
            OpenApiFile = openApiFile.PhysicalPath is not null ? Path.GetRelativePath(root, openApiFile.PhysicalPath) : openApiFile.Name,
            OutputDirectory = ".",
            Namespace = namespaceName,
            NamespacePrefix = namespacePrefix,
            ClientName = clientName,
            OverlayFiles = overlayFiles.Select(f => f.PhysicalPath is not null ? Path.GetRelativePath(root, f.PhysicalPath) : f.Name).ToList(),
            TypeMappings = typeMappings,
            GeneratedFiles = generatedFiles
        };

        var json = JsonSerializer.Serialize(config, s_jsonSerializerOptions);
        outputProvider.GetFileInfo(GenerationConfig.FileName).WriteAllText(json);
        Logger.LogInformation("  Saved configuration: {FileName}", GenerationConfig.FileName);
    }

    internal void CleanupRemovedFiles(IWritableFileProvider outputProvider, List<string>? previousFiles, List<string>? currentFiles)
    {
        if (previousFiles is null or [])
            return;

        var currentSet = new HashSet<string>(currentFiles ?? [], StringComparer.OrdinalIgnoreCase);
        var removedFiles = previousFiles.Where(f => !currentSet.Contains(f)).ToList();

        if (removedFiles.Count == 0)
            return;

        Logger.LogInformation("Cleaning up removed files:");
        foreach (var relativePath in removedFiles)
        {
            var fileInfo = outputProvider.GetFileInfo(relativePath);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
                Logger.LogInformation("  Deleted: {RelativePath}", relativePath);

                // Remove empty parent directories up to the output directory
                var directory = Path.GetDirectoryName(relativePath);
                while (!string.IsNullOrEmpty(directory))
                {
                    var dirContents = outputProvider.GetDirectoryContents(directory);
                    if (dirContents.Exists && !dirContents.Any())
                    {
                        outputProvider.GetFileInfo(directory).Delete();
                        Logger.LogInformation("  Removed empty directory: {Directory}", directory);
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

    private async Task<OpenApiDocument> ApplyOverlaysAsync(IFileInfo openApiFile, IFileInfo[] overlayFiles)
    {
        var readerSettings = new OverlayReaderSettings() { OpenApiSettings = s_openApiReaderSettings };

        var overlayDocument = new OverlayDocument();

        foreach (var overlayFile in overlayFiles)
        {
            var overlayDisplayPath = overlayFile.PhysicalPath ?? overlayFile.Name;
            using var overlayStream = overlayFile.CreateReadStream();
            var (overlay, diagnostic) = await OverlayDocument.LoadFromStreamAsync(overlayStream, DetectFormat(overlayFile.Name), readerSettings);
            if (diagnostic?.Errors.Count > 0)
            {
                Logger.LogError("Errors found in overlay '{OverlayFile}':", overlayDisplayPath);
                foreach (var error in diagnostic.Errors)
                    Logger.LogError("  - {ErrorMessage}", error.Message);
            }

            if (diagnostic?.Warnings.Count > 0)
            {
                Logger.LogWarning("Warnings found in overlay '{OverlayFile}':", overlayDisplayPath);
                foreach (var warning in diagnostic.Warnings)
                    Logger.LogWarning("  - {WarningMessage}", warning.Message);
            }

            if (overlay != null)
                overlayDocument = overlayDocument.CombineWith(overlay);
        }

        // OpenAPI Overlay spec only supports v3+.
        // If the input is a Swagger 2.0 spec, convert it to OpenAPI 3.1 in-memory before applying overlays.
        bool isV2 = await IsSwaggerV2Async(openApiFile);
        if (isV2)
            Logger.LogInformation("OpenAPI v2 (Swagger) spec detected. Converting to OpenAPI 3.1 before applying overlays...");

        // When isV2 is true, ownership of the MemoryStream is transferred here and disposed by the using statement.
        using var openApiStream = isV2
            ? await ConvertV2ToV3StreamAsync(openApiFile)
            : openApiFile.CreateReadStream();
        // Converted v2 streams are always serialized as JSON; detect format only for original v3 files.
        var openApiFormat = isV2 ? "json" : DetectFormat(openApiFile.Name);

        var (result, overlayDiagnostic) = await overlayDocument.ApplyToDocumentStreamAndLoadAsync(openApiStream, new Uri(openApiFile.PhysicalPath ?? "file:///document"), openApiFormat, readerSettings);

        if (overlayDiagnostic?.Errors.Count > 0)
        {
            Logger.LogError("Errors found after applying overlays:");
            foreach (var error in overlayDiagnostic.Errors)
                Logger.LogError("  - {ErrorMessage}", error.Message);
        }

        if (overlayDiagnostic?.Warnings.Count > 0)
        {
            Logger.LogWarning("Warnings:");
            foreach (var warning in overlayDiagnostic.Warnings)
                Logger.LogWarning("  - {WarningMessage}", warning.Message);
        }

        return result ?? throw new InvalidOperationException("Can not load document after applying overlays");
    }

    private async Task<MemoryStream> ConvertV2ToV3StreamAsync(IFileInfo openApiFile)
    {
        var document = await LoadOpenApiDocumentAsync(openApiFile);
        var memoryStream = new MemoryStream();
        await document.SerializeAsync(memoryStream, OpenApiSpecVersion.OpenApi3_1, "json", default);
        memoryStream.Position = 0;
        return memoryStream;
    }

    private static async Task<bool> IsSwaggerV2Async(IFileInfo file)
    {
        using var stream = file.CreateReadStream();
        using var reader = new StreamReader(stream, leaveOpen: false);
        char[] buffer = new char[512];
        int read = await reader.ReadBlockAsync(buffer, 0, buffer.Length);
        var beginning = new string(buffer, 0, read);
        // JSON Swagger 2.0 specs have a top-level "swagger": "2.0" field.
        // YAML Swagger 2.0 specs have a top-level `swagger: "2.0"` or `swagger: '2.0'` field.
        return beginning.Contains("\"swagger\"", StringComparison.OrdinalIgnoreCase)
            && beginning.Contains("\"2.0\"", StringComparison.OrdinalIgnoreCase)
            || beginning.Contains("swagger:", StringComparison.OrdinalIgnoreCase)
            && (beginning.Contains("\"2.0\"", StringComparison.OrdinalIgnoreCase)
                || beginning.Contains("'2.0'", StringComparison.OrdinalIgnoreCase)
                || System.Text.RegularExpressions.Regex.IsMatch(beginning, @"swagger\s*:\s*2\.0", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
    }

    private async Task<OpenApiDocument> LoadOpenApiDocumentAsync(IFileInfo openApiFile)
    {
        using var stream = openApiFile.CreateReadStream();
        var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream, DetectFormat(openApiFile.Name), s_openApiReaderSettings);

        if (diagnostic?.Errors.Count > 0)
        {
            Logger.LogError("Errors found in OpenAPI document:");
            foreach (var error in diagnostic.Errors)
                Logger.LogError("  - {ErrorMessage}", error.Message);
        }

        if (diagnostic?.Warnings.Count > 0)
        {
            Logger.LogWarning("Warnings:");
            foreach (var warning in diagnostic.Warnings)
                Logger.LogWarning("  - {WarningMessage}", warning.Message);
        }

        return document ?? throw new InvalidOperationException($"Can not load document from {openApiFile.PhysicalPath ?? openApiFile.Name}");
    }

    private static string? DetectFormat(string fileName) =>
        fileName.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            ? "yaml"
            : null;
}
