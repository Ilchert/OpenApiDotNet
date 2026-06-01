extern alias sourcegen;

using System.IO.Compression;
using System.Runtime.Versioning;
using Microsoft.CodeAnalysis;
using OpenApiSourceGenerator = sourcegen::OpenApiDotNet.SourceGenerator.OpenApiSourceGenerator;

namespace OpenApiDotNet.Tests;

public class SourceGeneratorTests
{
    private readonly string _fixturesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    [Fact]
    public void Generate_PetStoreSpec_CompilesWithoutWarnings()
    {
        var runResult = RunGenerator(["petstore.json"]);

        Assert.Empty(runResult.Diagnostics);

        var generatorResult = Assert.Single(runResult.Results);
        Assert.Empty(generatorResult.Diagnostics);
        Assert.NotEmpty(generatorResult.GeneratedSources);

        var generatedCode = string.Join(
            Environment.NewLine,
            generatorResult.GeneratedSources.Select(static source => source.SourceText.ToString()));

        Assert.Contains("namespace GeneratedClient.Models;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public interface IPetStoreAPIClient", generatedCode, StringComparison.Ordinal);

        var compilation = SourceGeneratorTestHelper.CreateGeneratedCompilation(runResult, "PetStore.Generated");
        var diagnostics = compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Generate_WithConfigurationAdditionalFile_UsesConfiguredNamespaceClientNameAndTypeMappings()
    {
        var runResult = SourceGeneratorTestHelper.RunGenerator(
            new OpenApiSourceGenerator(),
            [
                CreateConfigAdditionalText(
                    "ConfigDrivenOptions",
                    """
                    {
                      "openApiFile": "../petstore.json",
                      "namespace": "Contoso.Generated",
                      "clientName": "ContosoPets",
                      "typeMappings": {
                        "string:date": "NodaTime.LocalDate",
                        "string:date-time": "NodaTime.Instant"
                      }
                    }
                    """),
                CreateFixtureAdditionalText("petstore.json")
            ]);

        var generatorResult = Assert.Single(runResult.Results);

        Assert.Empty(runResult.Diagnostics);
        Assert.Empty(generatorResult.Diagnostics);

        var generatedCode = string.Join(
            Environment.NewLine,
            generatorResult.GeneratedSources.Select(static source => source.SourceText.ToString()));

        Assert.Contains("namespace Contoso.Generated;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public interface IContosoPets : IOpenApiClient", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public NodaTime.LocalDate? BirthDate { get; set; }", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public NodaTime.Instant? CreatedAt { get; set; }", generatedCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithConfigurationAdditionalFile_UsesConfiguredNamespacePrefix()
    {
        var runResult = SourceGeneratorTestHelper.RunGenerator(
            new OpenApiSourceGenerator(),
            [
                CreateConfigAdditionalText(
                    "ConfigDrivenNamespacePrefix",
                    """
                    {
                      "openApiFile": "../dotted-names.json",
                      "namespace": "DottedNames.Client",
                      "namespacePrefix": "Commerce"
                    }
                    """),
                CreateFixtureAdditionalText("dotted-names.json")
            ]);

        var generatorResult = Assert.Single(runResult.Results);

        Assert.Empty(runResult.Diagnostics);
        Assert.Empty(generatorResult.Diagnostics);

        var generatedCode = string.Join(
            Environment.NewLine,
            generatorResult.GeneratedSources.Select(static source => source.SourceText.ToString()));

        Assert.Contains("namespace DottedNames.Client.Models;", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public partial class Order", generatedCode, StringComparison.Ordinal);
        Assert.Contains("namespace DottedNames.Client.Models.Identity;", generatedCode, StringComparison.Ordinal);
        Assert.DoesNotContain("namespace DottedNames.Client.Models.Commerce;", generatedCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithConfigurationAdditionalFile_UsesConfiguredPrimarySpecAndOverlay()
    {
        var runResult = SourceGeneratorTestHelper.RunGenerator(
            new OpenApiSourceGenerator(),
            [
                new SourceGeneratorTestHelper.TestAdditionalText(
                    Path.GetFullPath(Path.Combine(_fixturesPath, "broken.json")),
                    "{ not valid json"),
                CreateFixtureAdditionalText("remove-pets-post.overlay.json"),
                CreateFixtureAdditionalText("petstore.json"),
                CreateConfigAdditionalText(
                    "ConfigDrivenSelection",
                    """
                    {
                      "openApiFile": "../petstore.json",
                      "overlayFiles": [
                        "../remove-pets-post.overlay.json"
                      ]
                    }
                    """)
            ]);

        var generatorResult = Assert.Single(runResult.Results);

        Assert.Contains(runResult.Diagnostics, static diagnostic => diagnostic.Id == "OADNSG001" && diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.Contains(generatorResult.Diagnostics, static diagnostic => diagnostic.Id == "OADNSG001" && diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.NotEmpty(generatorResult.GeneratedSources);

        var petsBuilder = generatorResult.GeneratedSources
            .FirstOrDefault(source => source.HintName.Contains("PetsBuilder.cs"));

        Assert.NotEqual(default, petsBuilder);

        var petsBuilderSource = petsBuilder.SourceText.ToString();

        Assert.NotNull(petsBuilderSource);
        Assert.DoesNotContain("Post(", petsBuilderSource, StringComparison.Ordinal);
        Assert.DoesNotContain("createPet", petsBuilderSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithMultipleConfigurationFiles_ReportsError()
    {
        var runResult = SourceGeneratorTestHelper.RunGenerator(
            new OpenApiSourceGenerator(),
            [
                CreateFixtureAdditionalText("petstore.json"),
                CreateConfigAdditionalText("ConfigA", """{ "openApiFile": "../petstore.json" }"""),
                CreateConfigAdditionalText("ConfigB", """{ "openApiFile": "../petstore.json" }""")
            ]);

        Assert.Contains(runResult.Diagnostics, static diagnostic => diagnostic.Id == "OADNSG003" && diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Generate_WithMultipleSpecs_WarnsAndUsesFirstSpec()
    {
        var runResult = SourceGeneratorTestHelper.RunGenerator(
            new OpenApiSourceGenerator(),
            [
                new SourceGeneratorTestHelper.TestAdditionalText("petstore.json", ReadFixture("petstore.json")),
                new SourceGeneratorTestHelper.TestAdditionalText("broken.json", "{ not valid json")
            ]);

        var generatorResult = Assert.Single(runResult.Results);

        Assert.Contains(runResult.Diagnostics, static diagnostic => diagnostic.Id == "OADNSG001" && diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.DoesNotContain(generatorResult.Diagnostics, static diagnostic => diagnostic.Id == "OADNSG002");
        Assert.NotEmpty(generatorResult.GeneratedSources);
    }

    [Fact]
    public void Generate_WithOverlay_AppliesOverlayChanges()
    {
        var runResult = SourceGeneratorTestHelper.RunGenerator(
            new OpenApiSourceGenerator(),
            [
                new SourceGeneratorTestHelper.TestAdditionalText("petstore.json", ReadFixture("petstore.json")),
                new SourceGeneratorTestHelper.TestAdditionalText("remove-pets-post.overlay.json", ReadFixture("remove-pets-post.overlay.json"), isOverlay: true)
            ]);

        var generatorResult = Assert.Single(runResult.Results);

        Assert.Empty(runResult.Diagnostics);
        Assert.Empty(generatorResult.Diagnostics);
        Assert.NotEmpty(generatorResult.GeneratedSources);

        var petsBuilder = generatorResult.GeneratedSources
            .FirstOrDefault(source => source.HintName.Contains("PetsBuilder.cs"));

        Assert.NotEqual(default, petsBuilder);

        var petsBuilderSource = petsBuilder.SourceText.ToString();

        Assert.NotNull(petsBuilderSource);
        Assert.DoesNotContain("Post(", petsBuilderSource, StringComparison.Ordinal);
        Assert.DoesNotContain("createPet", petsBuilderSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_WithMultipleOverlays_AppliesInOrder()
    {
        var runResult = SourceGeneratorTestHelper.RunGenerator(
            new OpenApiSourceGenerator(),
            [
                new SourceGeneratorTestHelper.TestAdditionalText("petstore.json", ReadFixture("petstore.json")),
                new SourceGeneratorTestHelper.TestAdditionalText("overlay1.json", ReadFixture("remove-pets-post.overlay.json"), isOverlay: true),
                new SourceGeneratorTestHelper.TestAdditionalText("overlay2.json", ReadFixture("remove-pets-post.overlay.json"), isOverlay: true)
            ]);

        var generatorResult = Assert.Single(runResult.Results);

        Assert.Empty(runResult.Diagnostics);
        Assert.Empty(generatorResult.Diagnostics);
        Assert.NotEmpty(generatorResult.GeneratedSources);
    }

    [Fact]
    public void Generate_WithTwoSpecsAndOneOverlay_WarnsAboutMultipleSpecs()
    {
        var runResult = SourceGeneratorTestHelper.RunGenerator(
            new OpenApiSourceGenerator(),
            [
                new SourceGeneratorTestHelper.TestAdditionalText("petstore.json", ReadFixture("petstore.json")),
                new SourceGeneratorTestHelper.TestAdditionalText("petstore2.json", ReadFixture("petstore.json")),
                new SourceGeneratorTestHelper.TestAdditionalText("overlay.json", ReadFixture("remove-pets-post.overlay.json"), isOverlay: true)
            ]);

        var generatorResult = Assert.Single(runResult.Results);

        Assert.Contains(runResult.Diagnostics, static diagnostic => diagnostic.Id == "OADNSG001" && diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.NotEmpty(generatorResult.GeneratedSources);
    }

    [Fact]
    public void Generate_WithYamlAdditionalFile_CompilesWithoutDiagnostics()
    {
        const string yamlSpec = """
            openapi: 3.0.3
            info:
              title: Pet Store API
              version: 1.0.0
            paths:
              /pets:
                get:
                  operationId: listPets
                  responses:
                    '200':
                      description: ok
                      content:
                        application/json:
                          schema:
                            type: array
                            items:
                              $ref: '#/components/schemas/Pet'
            components:
              schemas:
                Pet:
                  type: object
                  required:
                    - id
                    - name
                  properties:
                    id:
                      type: integer
                      format: int64
                    name:
                      type: string
                    birthDate:
                      type: string
                      format: date
                    createdAt:
                      type: string
                      format: date-time
            """;

        var runResult = SourceGeneratorTestHelper.RunGenerator(
            new OpenApiSourceGenerator(),
            [new SourceGeneratorTestHelper.TestAdditionalText("petstore.yaml", yamlSpec)]);

        var generatorResult = Assert.Single(runResult.Results);

        Assert.Empty(runResult.Diagnostics);
        Assert.Empty(generatorResult.Diagnostics);
        Assert.NotEmpty(generatorResult.GeneratedSources);

        var generatedCode = string.Join(
            Environment.NewLine,
            generatorResult.GeneratedSources.Select(static source => source.SourceText.ToString()));

        Assert.Contains("public interface IPetStoreAPIClient", generatedCode, StringComparison.Ordinal);
        Assert.Contains("public System.DateTimeOffset? CreatedAt { get; set; }", generatedCode, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceGeneratorAssembly_DoesNotReferenceExternalOpenApiAssemblies()
    {
        var referencedAssemblies = typeof(OpenApiSourceGenerator).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(referencedAssemblies, static assemblyName => string.Equals(assemblyName.Name, "Microsoft.OpenApi", StringComparison.Ordinal));
        Assert.DoesNotContain(referencedAssemblies, static assemblyName => string.Equals(assemblyName.Name, "Microsoft.OpenApi.Readers", StringComparison.Ordinal));
        Assert.DoesNotContain(referencedAssemblies, static assemblyName => string.Equals(assemblyName.Name, "Microsoft.OpenApi.YamlReader", StringComparison.Ordinal));
    }

    [Fact]
    public void SourceGeneratorAssembly_DoesNotReferenceExternalFileProviderAssembly()
    {
        var referencedAssemblies = typeof(OpenApiSourceGenerator).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(referencedAssemblies, static assemblyName => string.Equals(assemblyName.Name, "Microsoft.Extensions.FileProviders.Abstractions", StringComparison.Ordinal));
    }

    [Fact]
    public void SourceGeneratorAssembly_TargetsNetStandard20()
    {
        var targetFramework = typeof(OpenApiSourceGenerator).Assembly
            .GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
            .OfType<TargetFrameworkAttribute>()
            .SingleOrDefault()?
            .FrameworkName;

        Assert.Equal(".NETStandard,Version=v2.0", targetFramework);
    }

    [Fact]
    public void SourceGeneratorPackage_ShipsSingleAnalyzerAssembly()
    {
        using var package = ZipFile.OpenRead(GetSourceGeneratorPackagePath());
        var analyzerDlls = package.Entries
            .Select(static entry => entry.FullName)
            .Where(static entryName => entryName.StartsWith("analyzers/dotnet/cs/", StringComparison.OrdinalIgnoreCase)
                                     && entryName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.Contains("analyzers/dotnet/cs/OpenApiDotNet.SourceGenerator.dll", analyzerDlls);
        Assert.Single(analyzerDlls);
    }

    private GeneratorDriverRunResult RunGenerator(
        IReadOnlyList<string> fixtureNames)
    {
        var additionalTexts = fixtureNames
            .Select(fixtureName => CreateFixtureAdditionalText(fixtureName))
            .ToList();

        return SourceGeneratorTestHelper.RunGenerator(new OpenApiSourceGenerator(), additionalTexts);
    }

    private SourceGeneratorTestHelper.TestAdditionalText CreateFixtureAdditionalText(string fixtureName, bool isOverlay = false) =>
        new(
            Path.GetFullPath(Path.Combine(_fixturesPath, fixtureName)),
            ReadFixture(fixtureName),
            isOverlay);

    private SourceGeneratorTestHelper.TestAdditionalText CreateConfigAdditionalText(string directoryName, string content) =>
        new(
            Path.GetFullPath(Path.Combine(_fixturesPath, directoryName, GenerationConfig.FileName)),
            content);

    private static string GetSourceGeneratorPackagePath()
    {
        var version = typeof(OpenApiSourceGenerator).Assembly.GetName().Version
            ?? throw new InvalidOperationException("Unable to determine source generator assembly version.");
        var configuration = typeof(SourceGeneratorTests).Assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false)
            .OfType<System.Reflection.AssemblyConfigurationAttribute>()
            .SingleOrDefault()?
            .Configuration?
            .ToLowerInvariant() ?? "debug";

        var packageDirectory = Path.Combine(FindRepositoryRoot(), "artifacts", "package", configuration);
        var packageName = $"OpenApiDotNet.SourceGenerator.{version.Major}.{version.Minor}.{version.Build}.nupkg";

        var packagePath = Path.Combine(packageDirectory, packageName);
        Assert.True(File.Exists(packagePath), $"Expected package '{packageName}' to exist in '{packageDirectory}'.");
        return packagePath;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Directory.Build.props")))
            current = current.Parent;

        return current?.FullName ?? throw new InvalidOperationException("Unable to locate repository root from the test output directory.");
    }

    private string ReadFixture(string fixtureName) => File.ReadAllText(Path.Combine(_fixturesPath, fixtureName));
}
