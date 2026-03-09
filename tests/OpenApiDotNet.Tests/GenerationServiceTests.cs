using System.Text.Json;
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
            specFile, _output, new GenerationConfig { Namespace = "PetStore.Client" }, overlayFiles: []);

        Assert.NotEmpty(generatedFiles);
        foreach (var file in generatedFiles)
        {
            Assert.True(_output.Files.ContainsKey(Normalize(file)));
        }
    }

    [Fact]
    public async Task GenerateAsync_WritesConfigFile()
    {
        var specFile = CreateSpecFileInfo("petstore.json");

        await _service.GenerateAsync(
            specFile, _output, new GenerationConfig { Namespace = "PetStore.Client" }, overlayFiles: []);

        Assert.True(_output.Files.ContainsKey(GenerationConfig.FileName));
        var config = DeserializeConfig();
        Assert.NotNull(config);
        Assert.Equal("PetStore.Client", config!.Namespace);
        Assert.Equal(".", config.OutputDirectory);
        Assert.NotEmpty(config.GeneratedFiles);
    }

    [Fact]
    public async Task GenerateAsync_WithCustomClientName_SavesClientNameInConfig()
    {
        var specFile = CreateSpecFileInfo("petstore.json");

        await _service.GenerateAsync(
            specFile, _output, new GenerationConfig { Namespace = "PetStore.Client", ClientName = "MyPetClient" }, overlayFiles: []);

        var config = DeserializeConfig();
        Assert.Equal("MyPetClient", config!.ClientName);
    }

    [Fact]
    public async Task GenerateAsync_ReturnedFilesList_MatchesConfigGeneratedFiles()
    {
        var specFile = CreateSpecFileInfo("petstore.json");

        var generatedFiles = await _service.GenerateAsync(
            specFile, _output, new GenerationConfig { Namespace = "PetStore.Client" }, overlayFiles: []);

        var config = DeserializeConfig();
        Assert.Equivalent(generatedFiles.Select(Normalize), config!.GeneratedFiles.Select(Normalize));
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

        Assert.True(_output.Files.ContainsKey("Models/Pet.cs"));
        Assert.False(_output.Files.ContainsKey("Models/Order.cs"));
        Assert.False(_output.Files.ContainsKey("Models/User.cs"));
    }

    [Fact]
    public void CleanupRemovedFiles_WithNoPreviousFiles_DoesNothing()
    {
        _output.SetContent("Models/Pet.cs", "// pet");

        _service.CleanupRemovedFiles(_output, previousFiles: null, currentFiles: ["Models/Pet.cs"]);

        Assert.True(_output.Files.ContainsKey("Models/Pet.cs"));
    }

    [Fact]
    public void CleanupRemovedFiles_WithNoRemovedFiles_DoesNothing()
    {
        _output.SetContent("Models/Pet.cs", "// pet");
        _output.SetContent("Models/Order.cs", "// order");

        var files = new List<string> { "Models/Pet.cs", "Models/Order.cs" };

        _service.CleanupRemovedFiles(_output, previousFiles: files, currentFiles: files);

        Assert.Equal(2, _output.Files.Count);
    }

    [Fact]
    public void CleanupRemovedFiles_RemovesEmptyParentDirectories()
    {
        _output.SetContent("Models/Nested/Deep.cs", "// deep");

        var previousFiles = new List<string> { "Models/Nested/Deep.cs" };

        _service.CleanupRemovedFiles(_output, previousFiles, currentFiles: []);

        Assert.Empty(_output.Files);
        Assert.False(_output.GetDirectoryContents("Models/Nested").Exists);
        Assert.False(_output.GetDirectoryContents("Models").Exists);
    }

    [Fact]
    public async Task GenerateAsync_WithOverlay_RemovesOperation()
    {
        var specFile = CreateSpecFileInfo("petstore.json");
        var overlayFile = CreateSpecFileInfo("remove-pets-post.overlay.json");

        var generatedFiles = await _service.GenerateAsync(
            specFile, _output, new GenerationConfig { Namespace = "PetStore.Client" }, overlayFiles: [overlayFile]);

        Assert.NotEmpty(generatedFiles);
        var petsBuilderKey = generatedFiles.Select(Normalize).First(f => f.EndsWith("PetsBuilder.cs"));
        Assert.DoesNotContain("createPet", _output.Files[petsBuilderKey]);
        Assert.DoesNotContain("Post(", _output.Files[petsBuilderKey]);
    }

    [Fact]
    public async Task ConvertAsync_ConvertsDocumentToSpecifiedVersion()
    {
        var inputFile = CreateSpecFileInfo("petstore.json");
        var outputFile = _output.GetFileInfo("converted.json");

        await _service.ConvertAsync(inputFile, outputFile, "3.0", "json");

        Assert.True(_output.Files.ContainsKey("converted.json"));
        var content = _output.Files["converted.json"].TrimStart('\uFEFF');
        Assert.Contains("\"openapi\":", content);
        Assert.Contains("\"3.0.", content);
    }

    [Fact]
    public async Task ConvertAsync_WithUnsupportedVersion_ThrowsArgumentException()
    {
        var inputFile = CreateSpecFileInfo("petstore.json");
        var outputFile = _output.GetFileInfo("converted.json");

        var act = () => _service.ConvertAsync(inputFile, outputFile, "1.0", "json");

        var ex = await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Contains("Unsupported OpenAPI version", ex.Message);
    }
}
