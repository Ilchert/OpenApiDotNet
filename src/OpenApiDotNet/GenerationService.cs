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

    public async Task<List<string>> GenerateAsync(IFileInfo openApiFile, IWritableFileProvider outputProvider, GenerationConfig config, IFileInfo[] overlayFiles)
    {
        var displayPath = openApiFile.PhysicalPath ?? openApiFile.Name;
        Logger.LogInformation("Reading OpenAPI specification from: {FilePath}", displayPath);

        var openApiDocument = overlayFiles.Length > 0
             ? await ApplyOverlaysAsync(openApiFile, overlayFiles)
             : await LoadOpenApiDocumentAsync(openApiFile);

        Logger.LogInformation("Title: {Title}", openApiDocument.Info.Title);
        Logger.LogInformation("Version: {Version}", openApiDocument.Info.Version);
        Logger.LogInformation("Output: {OutputPath}", outputProvider.Root);
        Logger.LogInformation("Namespace: {Namespace}", config.Namespace);
        Logger.LogInformation("Namespace prefix: {NamespacePrefix}", config.NamespacePrefix);
        Logger.LogInformation("Client name: {ClientName}", config.ClientName);

        Logger.LogInformation("Generating client code...");

        var typeMappingConfig = new TypeMappingConfig(config.TypeMappings);
        var generator = new OpenApiGenerator(openApiDocument, config.Namespace, outputProvider, config.NamespacePrefix, config.ClientName, typeMappingConfig);
        var generatedFiles = generator.Generate();

        SaveConfig(openApiFile, outputProvider, config, overlayFiles, generatedFiles);

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
        var currentFiles = await GenerateAsync(openApiFileInfo, outputProvider, config, overlayFileInfos);

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

    private void SaveConfig(IFileInfo openApiFile, IWritableFileProvider outputProvider, GenerationConfig config, IFileInfo[] overlayFiles, List<string> generatedFiles)
    {
        var root = outputProvider.Root;

        var savedConfig = new GenerationConfig
        {
            OpenApiFile = openApiFile.PhysicalPath is not null ? Path.GetRelativePath(root, openApiFile.PhysicalPath) : openApiFile.Name,
            OutputDirectory = ".",
            Namespace = config.Namespace,
            NamespacePrefix = config.NamespacePrefix,
            ClientName = config.ClientName,
            OverlayFiles = overlayFiles.Select(f => f.PhysicalPath is not null ? Path.GetRelativePath(root, f.PhysicalPath) : f.Name).ToList(),
            TypeMappings = config.TypeMappings,
            GeneratedFiles = generatedFiles
        };

        var json = JsonSerializer.Serialize(savedConfig, s_jsonSerializerOptions);
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
        using var openApiStream = openApiFile.CreateReadStream();

        var overlayStreams = overlayFiles.Select(f =>
        {
            var stream = f.CreateReadStream();
            return (Stream: stream, Path: (string?)(f.PhysicalPath ?? f.Name));
        }).ToList();

        try
        {
            return await OpenApiDocumentLoader.LoadWithOverlaysAsync(
                openApiStream,
                openApiFile.PhysicalPath ?? openApiFile.Name,
                overlayStreams,
                s_openApiReaderSettings,
                onError: msg => Logger.LogError("{Message}", msg),
                onWarning: msg => Logger.LogWarning("{Message}", msg));
        }
        finally
        {
            foreach (var (stream, _) in overlayStreams)
                stream.Dispose();
        }
    }

    private async Task<OpenApiDocument> LoadOpenApiDocumentAsync(IFileInfo openApiFile)
    {
        using var stream = openApiFile.CreateReadStream();
        return await OpenApiDocumentLoader.LoadAsync(
            stream,
            openApiFile.PhysicalPath ?? openApiFile.Name,
            s_openApiReaderSettings,
            onError: msg => Logger.LogError("{Message}", msg),
            onWarning: msg => Logger.LogWarning("{Message}", msg));
    }
}
