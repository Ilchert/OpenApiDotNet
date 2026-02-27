using FluentAssertions;
using Microsoft.OpenApi;

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
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert - PetsIdBuilder should have Get operation and petId in constructor
            var builderPath = Path.Combine(outputDirectory, "Builders", "PetsIdBuilder.cs");
            var content = File.ReadAllText(builderPath);

            // Path parameter is captured in the builder constructor
            content.Should().Contain("long petId");
            content.Should().Contain("Get");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
        }
    }

    [Fact]
    public async Task Generate_WithMultiplePathParameters_GeneratesCorrectUrlBuildingAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.json");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert - Path parameters are distributed across builders
            var petsIdContent = File.ReadAllText(Path.Combine(outputDirectory, "Builders", "PetsIdBuilder.cs"));
            petsIdContent.Should().Contain("long petId");

            var photosIdContent = File.ReadAllText(Path.Combine(outputDirectory, "Builders", "PhotosIdBuilder.cs"));
            photosIdContent.Should().Contain("Get");
            photosIdContent.Should().Contain("Guid photoId");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
        }
    }

    [Fact]
    public async Task Generate_WithMixedPathTypes_GeneratesCorrectParametersAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.json");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert - Check builder for GetOwnerPet with correct parameter types
            var ownersIdContent = File.ReadAllText(Path.Combine(outputDirectory, "Builders", "OwnersIdBuilder.cs"));
            ownersIdContent.Should().Contain("string ownerId");

            // pets under owners/{ownerId} gets a context-prefixed name due to collision
            var ownersIdPetsIdContent = File.ReadAllText(Path.Combine(outputDirectory, "Builders", "OwnersIdPetsIdBuilder.cs"));
            ownersIdPetsIdContent.Should().Contain("Get");
            ownersIdPetsIdContent.Should().Contain("long petId");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
        }
    }

    [Fact]
    public async Task Generate_WithPathAndQueryParameters_GeneratesBothCorrectlyAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.json");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert - ListPets on PetsBuilder should have query parameters with encoding
            var builderPath = Path.Combine(outputDirectory, "Builders", "PetsBuilder.cs");
            var content = File.ReadAllText(builderPath);

            content.Should().Contain("Get");
            content.Should().Contain("int? limit");
            content.Should().Contain("Uri.EscapeDataString");
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
        }
    }
}
