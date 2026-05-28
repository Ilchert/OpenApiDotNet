using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace OpenApiDotNet.Tests;

internal static class SourceGeneratorTestHelper
{
    private static readonly HashSet<string> s_excludedReferenceAssemblyNames =
    [
        "OpenApiDotNet.SourceGenerator",
        "OpenApiDotNet.Tests"
    ];

    private static readonly MetadataReference[] s_metadataReferences = CreateMetadataReferences();

    public static GeneratorDriverRunResult RunGenerator(
        IIncrementalGenerator generator,
        IReadOnlyList<TestAdditionalText> additionalTexts,
        IReadOnlyDictionary<string, string>? globalOptions = null)
    {
        var compilation = CreateBaseCompilation();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: additionalTexts,
            parseOptions: (CSharpParseOptions)compilation.SyntaxTrees[0].Options,
            optionsProvider: new TestAnalyzerConfigOptionsProvider(globalOptions, additionalTexts));

        driver = driver.RunGenerators(compilation);
        return driver.GetRunResult();
    }

    public static CSharpCompilation CreateGeneratedCompilation(
        GeneratorDriverRunResult runResult,
        string assemblyName = "GeneratedClient")
    {
        var syntaxTrees = runResult.Results
            .SelectMany(static result => result.GeneratedSources)
            .Select(static source => CSharpSyntaxTree.ParseText(
                source.SourceText,
                new CSharpParseOptions(LanguageVersion.Latest),
                path: source.HintName));

        return CSharpCompilation.Create(
            assemblyName,
            syntaxTrees,
            s_metadataReferences,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static CSharpCompilation CreateBaseCompilation()
    {
        return CSharpCompilation.Create(
            "GeneratorHost",
            [CSharpSyntaxTree.ParseText("namespace TestHost; public sealed class Marker;", new CSharpParseOptions(LanguageVersion.Latest))],
            s_metadataReferences,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    private static MetadataReference[] CreateMetadataReferences()
    {
        var trustedPlatformPaths = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        return trustedPlatformPaths
            .Concat(
            [
                typeof(System.Net.Http.Json.HttpClientJsonExtensions).Assembly.Location,
                typeof(System.Text.Json.JsonSerializer).Assembly.Location,
                typeof(NodaTime.Instant).Assembly.Location
            ])
            .Where(static path => !s_excludedReferenceAssemblyNames.Contains(Path.GetFileNameWithoutExtension(path)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }

    internal sealed class TestAdditionalText : AdditionalText
    {
        private readonly string _content;

        public TestAdditionalText(string path, string content, bool isOverlay = false)
        {
            Path = path;
            _content = content;
            IsOverlay = isOverlay;
        }

        public override string Path { get; }

        public bool IsOverlay { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(_content);
    }

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private static readonly AnalyzerConfigOptions s_emptyOptions = new TestAnalyzerConfigOptions(null);
        private readonly AnalyzerConfigOptions _globalOptions;
        private readonly Dictionary<string, AnalyzerConfigOptions> _perFileOptions = new();

        public TestAnalyzerConfigOptionsProvider(
            IReadOnlyDictionary<string, string>? globalOptions,
            IReadOnlyList<TestAdditionalText>? additionalTexts = null)
        {
            _globalOptions = new TestAnalyzerConfigOptions(globalOptions);

            if (additionalTexts != null)
            {
                foreach (var additionalText in additionalTexts)
                {
                    if (additionalText.IsOverlay)
                    {
                        _perFileOptions[additionalText.Path] = new TestAnalyzerConfigOptions(
                            new Dictionary<string, string>
                            {
                                ["build_metadata.AdditionalFiles.OpenApiOverlay"] = "true"
                            });
                    }
                }
            }
        }

        public override AnalyzerConfigOptions GlobalOptions => _globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => s_emptyOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            if (_perFileOptions.TryGetValue(textFile.Path, out var options))
                return options;

            return s_emptyOptions;
        }
    }

    private sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string>? values) : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> _values = values ?? new Dictionary<string, string>();

        public override bool TryGetValue(string key, out string value)
        {
            if (_values.TryGetValue(key, out value!))
                return true;

            value = string.Empty;
            return false;
        }
    }
}
