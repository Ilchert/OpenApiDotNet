using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

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

    private static readonly OpenApiReaderSettings s_readerSettings = CreateReaderSettings();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var specs = context.AdditionalTextsProvider
            .Where(static file => IsSupportedSpec(file.Path))
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (pair, _) =>
            {
                var (file, options) = pair;
                options.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles.OpenApiOverlay", out var isOverlay);
                return (File: file, IsOverlay: string.Equals(isOverlay, "true", StringComparison.OrdinalIgnoreCase));
            })
            .Collect();

        var combined = specs.Combine(context.AnalyzerConfigOptionsProvider);
        context.RegisterSourceOutput(combined, static (productionContext, source) => Generate(productionContext, source.Left, source.Right));
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<(AdditionalText File, bool IsOverlay)> specs,
        AnalyzerConfigOptionsProvider optionsProvider)
    {
        if (specs.IsDefaultOrEmpty)
            return;

        var primarySpecs = specs.Where(s => !s.IsOverlay).ToList();
        var overlaySpecs = specs.Where(s => s.IsOverlay).ToList();

        if (primarySpecs.Count == 0)
        {
            // All files marked as overlays, treat first as primary
            primarySpecs.Add(specs[0]);
            overlaySpecs.RemoveAt(0);
        }

        var primarySpec = primarySpecs[0].File;

        if (primarySpecs.Count > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                s_multipleSpecsDescriptor,
                Location.None,
                Path.GetFileName(primarySpec.Path)));
        }

        try
        {
            GenerateCore(context, primarySpec, overlaySpecs.Select(s => s.File).ToArray(), optionsProvider);
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
        AnalyzerConfigOptionsProvider optionsProvider)
    {
        var document = overlays.Length > 0
            ? LoadOpenApiDocumentWithOverlays(spec, overlays)
            : LoadOpenApiDocument(spec);

        var provider = new InMemoryGeneratedFileProvider();
        var generator = new OpenApiGenerator(
            document,
            GetRootNamespace(optionsProvider),
            provider,
            clientName: GetClientName(optionsProvider),
            typeMappingConfig: CreateTypeMappings(optionsProvider));

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

    private static TypeMappingConfig CreateTypeMappings(AnalyzerConfigOptionsProvider optionsProvider)
    {
        var typeMappings = TypeMappingConfig.GetDefaults();
        if (!TryGetBooleanOption(optionsProvider.GlobalOptions, "build_property.openapidotnetusenodatime", out var useNodaTime) || !useNodaTime)
            return new TypeMappingConfig(typeMappings);

        foreach (var mapping in TypeMappingConfig.GetNodaTimeOverrides())
            typeMappings[mapping.Key] = mapping.Value;

        return new TypeMappingConfig(typeMappings);
    }

    private static string GetRootNamespace(AnalyzerConfigOptionsProvider optionsProvider)
    {
        if (optionsProvider.GlobalOptions.TryGetValue("build_property.rootnamespace", out var rootNamespace)
            && !string.IsNullOrWhiteSpace(rootNamespace))
        {
            return rootNamespace;
        }

        return "GeneratedClient";
    }

    private static string? GetClientName(AnalyzerConfigOptionsProvider optionsProvider)
    {
        if (optionsProvider.GlobalOptions.TryGetValue("build_property.openapidotnetclientname", out var clientName)
            && !string.IsNullOrWhiteSpace(clientName))
        {
            return clientName;
        }

        return null;
    }

    private static bool TryGetBooleanOption(AnalyzerConfigOptions options, string key, out bool value)
    {
        if (options.TryGetValue(key, out var rawValue) && bool.TryParse(rawValue, out value))
            return true;

        value = false;
        return false;
    }

    private static string? DetectFormat(string path) =>
        path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            ? "yaml"
            : null;

    private static bool IsSupportedSpec(string path) =>
        path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);
}
