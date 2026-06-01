using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using OpenApiDotNet;

namespace OpenApiDotNet.SourceGenerator;

[Generator]
public sealed class OpenApiSourceGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor s_multipleSpecsDescriptor = new(
        id: "OADNSG001",
        title: "Multiple OpenAPI specs found",
        messageFormat: "Multiple OpenAPI specification files found in AdditionalFiles. Using '{0}' as the primary spec. Mark overlay files with OpenApiOverlay='true' metadata to avoid this warning.",
        category: "OpenApiDotNet.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor s_loadFailureDescriptor = new(
        id: "OADNSG002",
        title: "Failed to load OpenAPI document",
        messageFormat: "{0}",
        category: "OpenApiDotNet.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor s_multipleConfigsDescriptor = new(
        id: "OADNSG003",
        title: "Multiple OpenApiDotNet configuration files found",
        messageFormat: "Multiple '{0}' files found in AdditionalFiles. Include only one configuration file per project build.",
        category: "OpenApiDotNet.SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly OpenApiReaderSettings s_readerSettings = CreateReaderSettings();
    private static readonly StringComparer s_pathComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var additionalFiles = context.AdditionalTextsProvider
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, _) =>
            {
                var (file, options) = pair;
                options.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.OpenApiOverlay", out var isOverlay);
                return (File: file, IsOverlay: string.Equals(isOverlay, "true", StringComparison.OrdinalIgnoreCase));
            })
            .Collect();

        context.RegisterSourceOutput(additionalFiles, static (productionContext, source) => Generate(productionContext, source));
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<(AdditionalText File, bool IsOverlay)> additionalFiles)
    {
        if (additionalFiles.IsDefaultOrEmpty)
            return;

        try
        {
            if (!TryResolveGenerationInputs(context, additionalFiles, out var primarySpec, out var overlays, out var config))
                return;

            GenerateCore(context, primarySpec, overlays, config);
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                s_loadFailureDescriptor,
                Location.None,
                ex.Message));
        }
    }

    private static void GenerateCore(
        SourceProductionContext context,
        AdditionalText spec,
        AdditionalText[] overlays,
        GenerationConfig? config)
    {
        var document = overlays.Length > 0
            ? LoadOpenApiDocumentWithOverlays(spec, overlays)
            : LoadOpenApiDocument(spec);

        var provider = new InMemoryGeneratedFileProvider();
        var generator = new OpenApiGenerator(
            document,
            GetRootNamespace(config),
            provider,
            namespacePrefix: GetNamespacePrefix(config),
            clientName: GetClientName(config),
            typeMappingConfig: CreateTypeMappings(config));

        generator.Generate();

        foreach (var generatedFile in provider.Files)
        {
            context.AddSource(
                generatedFile.Key.Replace('\\', '/'),
                SourceText.From(generatedFile.Value, Encoding.UTF8));
        }
    }

    private static OpenApiDocument LoadOpenApiDocument(AdditionalText spec)
    {
        var sourceText = spec.GetText();
        if (sourceText is null)
            throw new InvalidOperationException($"Unable to read OpenAPI spec '{spec.Path}'.");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(sourceText.ToString()));
        var (document, diagnostic) = OpenApiDocument.LoadAsync(stream, DetectFormat(spec.Path), s_readerSettings)
            .GetAwaiter()
            .GetResult();

        if (diagnostic?.Errors.Count > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, diagnostic.Errors.Select(static error => error.Message)));

        return document ?? throw new InvalidOperationException($"Unable to load OpenAPI spec '{spec.Path}'.");
    }

    private static OpenApiDocument LoadOpenApiDocumentWithOverlays(AdditionalText spec, AdditionalText[] overlays)
    {
        var document = LoadOpenApiDocument(spec);

        foreach (var overlay in overlays)
        {
            var overlaySourceText = overlay.GetText();
            if (overlaySourceText is null)
                throw new InvalidOperationException($"Unable to read overlay file '{overlay.Path}'.");

            ApplyOverlay(document, overlaySourceText.ToString(), overlay.Path);
        }

        return document;
    }

    private static void ApplyOverlay(OpenApiDocument document, string overlayJson, string overlayPath)
    {
        using var json = JsonDocument.Parse(overlayJson);

        if (!json.RootElement.TryGetProperty("actions", out var actions) || actions.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Overlay file '{overlayPath}' does not contain an 'actions' array.");

        foreach (var action in actions.EnumerateArray())
        {
            if (!action.TryGetProperty("target", out var targetElement) || targetElement.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException($"Overlay file '{overlayPath}' contains an action without a string 'target'.");

            var target = targetElement.GetString();
            if (string.IsNullOrWhiteSpace(target))
                throw new InvalidOperationException($"Overlay file '{overlayPath}' contains an action with an empty target.");

            var remove = action.TryGetProperty("remove", out var removeElement)
                         && removeElement.ValueKind is JsonValueKind.True or JsonValueKind.False
                         && removeElement.GetBoolean();

            if (!remove)
                throw new InvalidOperationException($"Overlay file '{overlayPath}' contains an unsupported action for target '{target}'. Only remove actions are supported.");

            ApplyRemoveAction(document, target, overlayPath);
        }
    }

    private static void ApplyRemoveAction(OpenApiDocument document, string target, string overlayPath)
    {
        if (!TryParsePathTarget(target, out var path, out var methodName))
            throw new InvalidOperationException($"Overlay file '{overlayPath}' contains unsupported target '{target}'.");

        if (document.Paths is null || !document.Paths.TryGetValue(path, out var pathItem))
            return;

        if (methodName.Length == 0)
        {
            document.Paths.Remove(path);
            return;
        }

        if (!TryParseHttpMethod(methodName, out var method))
            throw new InvalidOperationException($"Overlay file '{overlayPath}' contains unsupported HTTP method '{methodName}'.");

        pathItem.Operations?.Remove(method);

        if (pathItem.Operations is null || pathItem.Operations.Count == 0)
            document.Paths.Remove(path);
    }

    private static bool TryParsePathTarget(string target, out string path, out string methodName)
    {
        const string prefix = "$.paths['";
        path = string.Empty;
        methodName = string.Empty;

        if (!target.StartsWith(prefix, StringComparison.Ordinal) || target.Length <= prefix.Length)
            return false;

        var endOfPath = target.IndexOf("']", prefix.Length, StringComparison.Ordinal);
        if (endOfPath < 0)
            return false;

        path = target.Substring(prefix.Length, endOfPath - prefix.Length);
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (endOfPath == target.Length - 2)
            return true;

        if (target[endOfPath + 2] != '.')
            return false;

        methodName = target.Substring(endOfPath + 3);
        return !string.IsNullOrWhiteSpace(methodName);
    }

    private static bool TryParseHttpMethod(string methodName, out HttpMethod method)
    {
        method = methodName.ToLowerInvariant() switch
        {
            "get" => HttpMethod.Get,
            "put" => HttpMethod.Put,
            "post" => HttpMethod.Post,
            "delete" => HttpMethod.Delete,
            "options" => HttpMethod.Options,
            "head" => HttpMethod.Head,
            "trace" => HttpMethod.Trace,
            _ => default
        };

        return !EqualityComparer<HttpMethod>.Default.Equals(method, default);
    }

    private static OpenApiReaderSettings CreateReaderSettings()
    {
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();
        return settings;
    }

    private static bool TryResolveGenerationInputs(
        SourceProductionContext context,
        ImmutableArray<(AdditionalText File, bool IsOverlay)> additionalFiles,
        out AdditionalText primarySpec,
        out AdditionalText[] overlays,
        out GenerationConfig? config)
    {
        primarySpec = null!;
        overlays = [];
        config = null;

        var configFiles = additionalFiles
            .Where(static entry => IsGenerationConfigFile(entry.File.Path))
            .Select(static entry => entry.File)
            .ToList();

        if (configFiles.Count > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                s_multipleConfigsDescriptor,
                Location.None,
                GenerationConfig.FileName));
            return false;
        }

        var configFile = configFiles.SingleOrDefault();
        if (configFile is not null)
            config = LoadGenerationConfig(configFile);

        var specs = additionalFiles
            .Where(static entry => IsSupportedSpec(entry.File.Path))
            .ToList();

        if (specs.Count == 0)
            return false;

        var specLookup = specs.ToDictionary(static entry => GetNormalizedPath(entry.File.Path), static entry => entry.File, s_pathComparer);
        AdditionalText? configuredPrimarySpec = null;
        var configuredOverlayFiles = new List<AdditionalText>();
        var configuredOverlayPaths = new HashSet<string>(s_pathComparer);

        if (configFile is not null && config is not null)
        {
            if (!string.IsNullOrWhiteSpace(config.OpenApiFile))
            {
                configuredPrimarySpec = ResolveConfiguredFile(
                    specLookup,
                    configFile.Path,
                    config.OpenApiFile,
                    nameof(GenerationConfig.OpenApiFile));
            }

            foreach (var overlayPath in config.OverlayFiles)
            {
                var overlayFile = ResolveConfiguredFile(
                    specLookup,
                    configFile.Path,
                    overlayPath,
                    nameof(GenerationConfig.OverlayFiles));
                var normalizedOverlayPath = GetNormalizedPath(overlayFile.Path);

                if (configuredPrimarySpec is not null
                    && s_pathComparer.Equals(normalizedOverlayPath, GetNormalizedPath(configuredPrimarySpec.Path)))
                {
                    throw new InvalidOperationException(
                        $"Configuration file '{configFile.Path}' cannot use '{overlayPath}' as both '{nameof(GenerationConfig.OpenApiFile)}' and '{nameof(GenerationConfig.OverlayFiles)}'.");
                }

                configuredOverlayFiles.Add(overlayFile);
                configuredOverlayPaths.Add(normalizedOverlayPath);
            }
        }

        var overlayFiles = new List<AdditionalText>(configuredOverlayFiles);
        foreach (var spec in specs)
        {
            var normalizedSpecPath = GetNormalizedPath(spec.File.Path);
            if (!spec.IsOverlay || configuredOverlayPaths.Contains(normalizedSpecPath))
                continue;

            if (configuredPrimarySpec is not null
                && s_pathComparer.Equals(normalizedSpecPath, GetNormalizedPath(configuredPrimarySpec.Path)))
            {
                continue;
            }

            overlayFiles.Add(spec.File);
        }

        if (configuredPrimarySpec is null)
        {
            var overlayPathSet = new HashSet<string>(overlayFiles.Select(static file => GetNormalizedPath(file.Path)), s_pathComparer);
            var primarySpecs = specs
                .Where(entry => !overlayPathSet.Contains(GetNormalizedPath(entry.File.Path)))
                .ToList();

            if (primarySpecs.Count == 0)
            {
                configuredPrimarySpec = specs[0].File;
                overlayFiles = overlayFiles
                    .Where(file => !s_pathComparer.Equals(GetNormalizedPath(file.Path), GetNormalizedPath(configuredPrimarySpec.Path)))
                    .ToList();
            }
            else
            {
                configuredPrimarySpec = primarySpecs[0].File;
            }
        }

        overlayFiles = overlayFiles
            .Where(file => !s_pathComparer.Equals(GetNormalizedPath(file.Path), GetNormalizedPath(configuredPrimarySpec.Path)))
            .ToList();

        var remainingPrimarySpecs = specs
            .Count(entry =>
                !s_pathComparer.Equals(GetNormalizedPath(entry.File.Path), GetNormalizedPath(configuredPrimarySpec.Path))
                && !overlayFiles.Any(file => s_pathComparer.Equals(GetNormalizedPath(file.Path), GetNormalizedPath(entry.File.Path))));

        if (remainingPrimarySpecs > 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                s_multipleSpecsDescriptor,
                Location.None,
                Path.GetFileName(configuredPrimarySpec.Path)));
        }

        primarySpec = configuredPrimarySpec;
        overlays = overlayFiles.ToArray();
        return true;
    }

    private static GenerationConfig LoadGenerationConfig(AdditionalText configFile)
    {
        var sourceText = configFile.GetText();
        if (sourceText is null)
            throw new InvalidOperationException($"Unable to read OpenApiDotNet configuration '{configFile.Path}'.");

        return JsonSerializer.Deserialize<GenerationConfig>(sourceText.ToString())
            ?? throw new InvalidOperationException($"Unable to parse OpenApiDotNet configuration '{configFile.Path}'.");
    }

    private static AdditionalText ResolveConfiguredFile(
        IReadOnlyDictionary<string, AdditionalText> filesByPath,
        string configPath,
        string configuredPath,
        string propertyName)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            throw new InvalidOperationException($"Configuration file '{configPath}' contains an empty '{propertyName}' value.");

        var resolvedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(configPath) ?? string.Empty, configuredPath));
        if (filesByPath.TryGetValue(resolvedPath, out var file))
            return file;

        throw new InvalidOperationException(
            $"Configuration file '{configPath}' references '{configuredPath}', but that file was not included as an AdditionalFiles item.");
    }

    private static string GetNormalizedPath(string path) => Path.GetFullPath(path);

    private static TypeMappingConfig CreateTypeMappings(GenerationConfig? config)
    {
        var typeMappings = TypeMappingConfig.GetDefaults();
        if (config?.TypeMappings is not null)
        {
            foreach (var mapping in config.TypeMappings)
                typeMappings[mapping.Key] = mapping.Value;
        }

        return new TypeMappingConfig(typeMappings);
    }

    private static string GetRootNamespace(GenerationConfig? config)
    {
        if (!string.IsNullOrWhiteSpace(config?.Namespace))
            return config!.Namespace!;

        return "GeneratedClient";
    }

    private static string? GetNamespacePrefix(GenerationConfig? config) =>
        string.IsNullOrWhiteSpace(config?.NamespacePrefix)
            ? null
            : config!.NamespacePrefix!;

    private static string? GetClientName(GenerationConfig? config)
    {
        if (!string.IsNullOrWhiteSpace(config?.ClientName))
            return config!.ClientName!;

        return null;
    }

    private static string? DetectFormat(string path) =>
        path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            ? "yaml"
            : null;

    private static bool IsGenerationConfigFile(string path) =>
        string.Equals(Path.GetFileName(path), GenerationConfig.FileName, StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedSpec(string path) =>
        !IsGenerationConfigFile(path)
        && (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase));
}
