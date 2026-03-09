using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using OpenApiDotNet.IO;
using OpenApiDotNet.Tests.IO;

namespace OpenApiDotNet.Tests;

public class GenerationServiceTests
{
    private readonly string _fixturesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures");
    private readonly InMemoryWritableFileProvider _output = new();
    private readonly GenerationService _service = new();

    private IFileInfo CreateSpecFileInfo(string fixtureName)
    {
        var content = File.ReadAllText(Path.Combine(_fixturesPath, fixtureName));
        _output.SetContent(fixtureName, content);
        return _output.GetFileInfo(fixtureName);
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private GenerationConfig? DeserializeConfig()
    {
        var content = _output.Files[GenerationConfig.FileName].TrimStart('\uFEFF');
        return JsonSerializer.Deserialize<GenerationConfig>(content);
    }

    [Fact]
    public async Task GenerateAsync_WritesGeneratedFilesAndConfig()
    {
        var specFile = CreateSpecFileInfo("petstore.json");

        var generatedFiles = await _service.GenerateAsync(
            specFile, _output, "PetStore.Client",
            namespacePrefix: null, clientName: null, overlayFiles: [], typeMappings: null);

        generatedFiles.Should().NotBeEmpty();
        foreach (var file in generatedFiles)
        {
            _output.Files.Should().ContainKey(Normalize(file));
        }
    }

    [Fact]
    public async Task GenerateAsync_WritesConfigFile()
    {
        var specFile = CreateSpecFileInfo("petstore.json");

        await _service.GenerateAsync(
            specFile, _output, "PetStore.Client",
            namespacePrefix: null, clientName: null, overlayFiles: [], typeMappings: null);

        _output.Files.Should().ContainKey(GenerationConfig.FileName);
        var config = DeserializeConfig();
        config.Should().NotBeNull();
        config!.Namespace.Should().Be("PetStore.Client");
        config.OutputDirectory.Should().Be(".");
        config.GeneratedFiles.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_WithCustomClientName_SavesClientNameInConfig()
    {
        var specFile = CreateSpecFileInfo("petstore.json");

        await _service.GenerateAsync(
            specFile, _output, "PetStore.Client",
            namespacePrefix: null, clientName: "MyPetClient", overlayFiles: [], typeMappings: null);

        var config = DeserializeConfig();
        config!.ClientName.Should().Be("MyPetClient");
    }

    [Fact]
    public async Task GenerateAsync_ReturnedFilesList_MatchesConfigGeneratedFiles()
    {
        var specFile = CreateSpecFileInfo("petstore.json");

        var generatedFiles = await _service.GenerateAsync(
            specFile, _output, "PetStore.Client",
            namespacePrefix: null, clientName: null, overlayFiles: [], typeMappings: null);

        var config = DeserializeConfig();
        config!.GeneratedFiles!.Select(Normalize).Should().BeEquivalentTo(generatedFiles.Select(Normalize));
    }

    [Fact]
    public void CleanupRemovedFiles_DeletesStaleFiles()
    {
        _output.SetContent("Models/Pet.cs", "// pet");
        _output.SetContent("Models/Order.cs", "// order");
        _output.SetContent("Models/User.cs", "// user");

        var previousFiles = new List<string> { "Models/Pet.cs", "Models/Order.cs", "Models/User.cs" };
        var currentFiles = new List<string> { "Models/Pet.cs" };

        _service.CleanupRemovedFiles(_output, previousFiles, currentFiles);

        _output.Files.Should().ContainKey("Models/Pet.cs");
        _output.Files.Should().NotContainKey("Models/Order.cs");
        _output.Files.Should().NotContainKey("Models/User.cs");
    }

    [Fact]
    public void CleanupRemovedFiles_WithNoPreviousFiles_DoesNothing()
    {
        _output.SetContent("Models/Pet.cs", "// pet");

        _service.CleanupRemovedFiles(_output, previousFiles: null, currentFiles: ["Models/Pet.cs"]);

        _output.Files.Should().ContainKey("Models/Pet.cs");
    }

    [Fact]
    public void CleanupRemovedFiles_WithNoRemovedFiles_DoesNothing()
    {
        _output.SetContent("Models/Pet.cs", "// pet");
        _output.SetContent("Models/Order.cs", "// order");

        var files = new List<string> { "Models/Pet.cs", "Models/Order.cs" };

        _service.CleanupRemovedFiles(_output, previousFiles: files, currentFiles: files);

        _output.Files.Should().HaveCount(2);
    }

    [Fact]
    public void CleanupRemovedFiles_RemovesEmptyParentDirectories()
    {
        _output.SetContent("Models/Nested/Deep.cs", "// deep");

        var previousFiles = new List<string> { "Models/Nested/Deep.cs" };

        _service.CleanupRemovedFiles(_output, previousFiles, currentFiles: []);

        _output.Files.Should().BeEmpty();
        _output.GetDirectoryContents("Models/Nested").Exists.Should().BeFalse();
        _output.GetDirectoryContents("Models").Exists.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAsync_WithOverlay_RemovesOperation()
    {
        var specFile = CreateSpecFileInfo("petstore.json");
        var overlayFile = CreateSpecFileInfo("remove-pets-post.overlay.json");

        var generatedFiles = await _service.GenerateAsync(
            specFile, _output, "PetStore.Client",
            namespacePrefix: null, clientName: null, overlayFiles: [overlayFile], typeMappings: null);

        generatedFiles.Should().NotBeEmpty();
        var petsBuilderKey = generatedFiles.Select(Normalize).First(f => f.EndsWith("PetsBuilder.cs"));
        _output.Files[petsBuilderKey].Should().NotContain("createPet")
            .And.NotContain("Post(");
    }

    [Fact]
    public async Task ConvertAsync_ConvertsDocumentToSpecifiedVersion()
    {
        var inputFile = CreateSpecFileInfo("petstore.json");
        var outputFile = _output.GetFileInfo("converted.json");

        await _service.ConvertAsync(inputFile, outputFile, "3.0", "json");

        _output.Files.Should().ContainKey("converted.json");
        var content = _output.Files["converted.json"].TrimStart('\uFEFF');
        content.Should().Contain("\"openapi\":");
        content.Should().Contain("\"3.0.");
    }

    [Fact]
    public async Task ConvertAsync_WithUnsupportedVersion_ThrowsArgumentException()
    {
        var inputFile = CreateSpecFileInfo("petstore.json");
        var outputFile = _output.GetFileInfo("converted.json");

        var act = () => _service.ConvertAsync(inputFile, outputFile, "1.0", "json");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Unsupported OpenAPI version*");
    }
}
