using FluentAssertions;
using Microsoft.OpenApi;
using OpenApiDotNet.Tests.IO;

namespace OpenApiDotNet.Tests;

public class PathParameterTests
{
    private readonly string _fixturesPath;

    public PathParameterTests()
    {
        var baseDirectory = AppContext.BaseDirectory;
        _fixturesPath = Path.Combine(baseDirectory, "Fixtures");
    }

    [Fact]
    public async Task Generate_WithSinglePathParameter_GeneratesUrlEncodedPathAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.json");

        using var stream = File.OpenRead(specPath);
        var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

        var output = new InMemoryWritableFileProvider();
        var generator = new OpenApiGenerator(document, "PetStore.Client", output);

        // Act
        generator.Generate();

        // Assert - IdBuilder (under Pets namespace) should have Get operation and petId in constructor
        var content = output.Files["Builders/Pets/IdBuilder.cs"];

        // Path parameter is captured in the builder constructor
        content.Should().Contain("long petId");
        content.Should().Contain("Get");
    }

    [Fact]
    public async Task Generate_WithMultiplePathParameters_GeneratesCorrectUrlBuildingAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.json");

        using var stream = File.OpenRead(specPath);
        var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

        var output = new InMemoryWritableFileProvider();
        var generator = new OpenApiGenerator(document, "PetStore.Client", output);

        // Act
        generator.Generate();

        // Assert - Path parameters are distributed across builders
        var petsIdContent = output.Files["Builders/Pets/IdBuilder.cs"];
        petsIdContent.Should().Contain("long petId");

        var photosIdContent = output.Files["Builders/Pets/Id/Photos/IdBuilder.cs"];
        photosIdContent.Should().Contain("Get");
        photosIdContent.Should().Contain("Guid photoId");
    }

    [Fact]
    public async Task Generate_WithMixedPathTypes_GeneratesCorrectParametersAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.json");

        using var stream = File.OpenRead(specPath);
        var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

        var output = new InMemoryWritableFileProvider();
        var generator = new OpenApiGenerator(document, "PetStore.Client", output);

        // Act
        generator.Generate();

        // Assert - Check builder for GetOwnerPet with correct parameter types
        var ownersIdContent = output.Files["Builders/Owners/IdBuilder.cs"];
        ownersIdContent.Should().Contain("string ownerId");

        // pets under owners/{ownerId} uses short name in nested namespace
        var ownersIdPetsIdContent = output.Files["Builders/Owners/Id/Pets/IdBuilder.cs"];
        ownersIdPetsIdContent.Should().Contain("Get");
        ownersIdPetsIdContent.Should().Contain("long petId");
    }

    [Fact]
    public async Task Generate_WithPathAndQueryParameters_GeneratesBothCorrectlyAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.json");

        using var stream = File.OpenRead(specPath);
        var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

        var output = new InMemoryWritableFileProvider();
        var generator = new OpenApiGenerator(document, "PetStore.Client", output);

        // Act
        generator.Generate();

        // Assert - ListPets on PetsBuilder should have query parameters with encoding
        var content = output.Files["Builders/PetsBuilder.cs"];

        content.Should().Contain("Get");
        content.Should().Contain("int? limit");
        content.Should().Contain("Uri.EscapeDataString");
    }
}
