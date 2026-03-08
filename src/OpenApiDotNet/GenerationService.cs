using System.Text.Json;
using BinkyLabs.OpenApi.Overlays;
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

    public async Task GenerateAsync(FileInfo openApiFile, DirectoryInfo outputDirectory, string namespaceName, string? namespacePrefix, string? clientName, FileInfo[] overlayFiles, Dictionary<string, string>? typeMappings)
    {
        Logger.LogInformation("Reading OpenAPI specification from: {FilePath}", openApiFile.FullName);

        var openApiDocument = overlayFiles.Length > 0
             ? await ApplyOverlaysAsync(openApiFile, overlayFiles)
             : await LoadOpenApiDocumentAsync(openApiFile);

        Logger.LogInformation("Title: {Title}", openApiDocument.Info.Title);
        Logger.LogInformation("Version: {Version}", openApiDocument.Info.Version);
        Logger.LogInformation("Output: {OutputDirectory}", outputDirectory.FullName);
        Logger.LogInformation("Namespace: {Namespace}", namespaceName);
        if (namespacePrefix != null)
            Logger.LogInformation("Namespace prefix: {NamespacePrefix}", namespacePrefix);
        if (clientName != null)
            Logger.LogInformation("Client name: {ClientName}", clientName);

        Logger.LogInformation("Generating client code...");

        Directory.CreateDirectory(outputDirectory.FullName);
        using var outputProvider = new PhysicalWritableFileProvider(outputDirectory.FullName);
        var generator = new OpenApiGenerator(openApiDocument, namespaceName, outputProvider, namespacePrefix, clientName, new TypeMappingConfig(typeMappings));
        var generatedFiles = generator.Generate();

        SaveConfig(openApiFile, outputDirectory, namespaceName, namespacePrefix, clientName, overlayFiles, typeMappings, generatedFiles);

        Logger.LogInformation("✓ Client generation complete!");
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

        var overlayFiles = config.OverlayFiles
            .Select(o => Path.IsPathRooted(o) ? o : Path.GetFullPath(Path.Combine(baseDirectory, o)))
            .Select(o => new FileInfo(o))
            .ToArray();

        var previousFiles = config.GeneratedFiles;

        Logger.LogInformation("Updating from configuration: {ConfigFile}", configFile.FullName);
        await GenerateAsync(new FileInfo(openApiFilePath), new DirectoryInfo(outputDirectoryPath), config.Namespace, config.NamespacePrefix, config.ClientName, overlayFiles, config.TypeMappings);

        // Read the updated config to get the new list of generated files
        var updatedJson = File.ReadAllText(configFile.FullName);
        var updatedConfig = JsonSerializer.Deserialize<GenerationConfig>(updatedJson, s_jsonSerializerOptions);
        var currentFiles = updatedConfig?.GeneratedFiles;

        CleanupRemovedFiles(outputDirectoryPath, previousFiles, currentFiles);
    }

    public async Task ConvertAsync(FileInfo inputFile, FileInfo outputFile, string version, string format)
    {
        Logger.LogInformation("Reading OpenAPI specification from: {FilePath}", inputFile.FullName);

        var openApiDocument = await LoadOpenApiDocumentAsync(inputFile);

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

        Logger.LogInformation("✓ Converted to OpenAPI {Version} ({Format}): {OutputFile}", version, format, outputFile.FullName);
    }

    private void SaveConfig(FileInfo openApiFile, DirectoryInfo outputDirectory, string namespaceName, string? namespacePrefix, string? clientName, FileInfo[] overlayFiles, Dictionary<string, string>? typeMappings, List<string> generatedFiles)
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

        var json = JsonSerializer.Serialize(config, s_jsonSerializerOptions);
        var configPath = Path.Combine(outputDirectory.FullName, GenerationConfig.FileName);
        File.WriteAllText(configPath, json);
        Logger.LogInformation("  Saved configuration: {FileName}", GenerationConfig.FileName);
    }

    private void CleanupRemovedFiles(string outputDirectory, List<string>? previousFiles, List<string>? currentFiles)
    {
        if (previousFiles is null or [])
            return;

        var currentSet = new HashSet<string>(currentFiles ?? [], StringComparer.OrdinalIgnoreCase);
        var removedFiles = previousFiles.Where(f => !currentSet.Contains(f)).ToList();

        if (removedFiles.Count == 0)
            return;

        Directory.CreateDirectory(outputDirectory);
        using var outputProvider = new PhysicalWritableFileProvider(outputDirectory);

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

    private async Task<OpenApiDocument> ApplyOverlaysAsync(FileInfo openApiFile, FileInfo[] overlayFiles)
    {
        var readerSettings = new OverlayReaderSettings() { OpenApiSettings = s_openApiReaderSettings };

        var overlayDocument = new OverlayDocument();

        foreach (var overlayFile in overlayFiles)
        {
            var (overlay, diagnostic) = await OverlayDocument.LoadFromUrlAsync(overlayFile.FullName, readerSettings);
            if (diagnostic?.Errors.Count > 0)
            {
                Logger.LogError("Errors found in overlay '{OverlayFile}':", overlayFile.FullName);
                foreach (var error in diagnostic.Errors)
                    Logger.LogError("  - {ErrorMessage}", error.Message);
            }

            if (diagnostic?.Warnings.Count > 0)
            {
                Logger.LogWarning("Warnings found in overlay '{OverlayFile}':", overlayFile.FullName);
                foreach (var warning in diagnostic.Warnings)
                    Logger.LogWarning("  - {WarningMessage}", warning.Message);
            }

            if (overlay != null)
                overlayDocument = overlayDocument.CombineWith(overlay);
        }

        var (result, overlayDiagnostic) = await overlayDocument.ApplyToDocumentAndLoadAsync(openApiFile.FullName, readerSettings: readerSettings);

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

    private async Task<OpenApiDocument> LoadOpenApiDocumentAsync(FileInfo openApiFile)
    {
        var (document, diagnostic) = await OpenApiDocument.LoadAsync(openApiFile.FullName, settings: s_openApiReaderSettings);

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

        return document ?? throw new InvalidOperationException($"Can not load document from file {openApiFile.FullName}");
    }
}
