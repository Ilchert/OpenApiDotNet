using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.OpenApi;
using OpenApiDotNet.Tests.IO;

namespace OpenApiDotNet.Tests;

public class CompilationVerificationTests
{
    private readonly string _fixturesPath;

    public CompilationVerificationTests()
    {
        _fixturesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures");
    }

    [Fact]
    public async Task Generate_PetStoreSpec_CompilesWithoutWarnings()
    {
        // Arrange - petstore.json covers all OpenAPI definition cases:
        //   - object models with required/optional properties
        //   - all property types: long, string, bool, double, date, date-time, uuid, enum refs
        //   - enum models (PetStatus, PetSize) with hyphenated member names
        //   - GET/POST/DELETE operations
        //   - single path parameter (/pets/{petId})
        //   - multiple path parameters (/pets/{petId}/photos/{photoId})
        //   - nested resource paths (/owners/{ownerId}/pets/{petId})
        //   - query parameters (int, array, enum, System.Object via untyped schema)
        //   - request body and response body
        var specPath = Path.Combine(_fixturesPath, "petstore.json");

        using var stream = File.OpenRead(specPath);
        var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);
        diagnostic?.Errors.Should().BeEmpty();

        var output = new InMemoryWritableFileProvider();
        var generator = new OpenApiGenerator(document, "PetStore.Client", output);

        // Act
        generator.Generate();

        // Collect all generated .cs files from the in-memory provider
        output.Files.Should().NotBeEmpty("generator should produce at least one .cs file");

        var syntaxTrees = output.Files
            .Where(f => f.Key.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(f => CSharpSyntaxTree.ParseText(
                f.Value,
                new CSharpParseOptions(LanguageVersion.Latest),
                path: f.Key))
            .ToList();

        // Build metadata references from trusted platform assemblies (BCL) and
        // assemblies already loaded in this process (for NodaTime, etc.)
        var trustedPlatformPaths = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var references = trustedPlatformPaths
            .Concat(AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => a.Location))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            "PetStore.Client",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        // Assert - no warnings or errors in the generated code
        var diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToList();

        diagnostics.Should().BeEmpty(
            $"generated code should compile without warnings or errors, but got:\n" +
            string.Join("\n", diagnostics.Select(d => $"  [{d.Severity}] {d.Id}: {d.GetMessage()} ({d.Location})")));
    }
}
