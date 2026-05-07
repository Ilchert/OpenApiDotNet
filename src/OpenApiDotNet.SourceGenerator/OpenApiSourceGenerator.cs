using System.Collections.Immutable;
using System.Text;
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
        title: "Multiple OpenAPI specs are not supported",
        messageFormat: "OpenApiDotNet.SourceGenerator currently supports a single OpenAPI AdditionalFile. Using '{0}'.",
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
            .Collect();

        var combined = specs.Combine(context.AnalyzerConfigOptionsProvider);
        context.RegisterSourceOutput(combined, static (productionContext, source) => Generate(productionContext, source.Left, source.Right));
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<AdditionalText> specs,
        AnalyzerConfigOptionsProvider optionsProvider)
    {
        if (specs.IsDefaultOrEmpty)
            return;

        var spec = specs[0];
        if (specs.Length > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                s_multipleSpecsDescriptor,
                Location.None,
                Path.GetFileName(spec.Path)));
        }

        try
        {
            GenerateCore(context, spec, optionsProvider);
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
        AnalyzerConfigOptionsProvider optionsProvider)
    {
        var document = LoadOpenApiDocument(spec);
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
