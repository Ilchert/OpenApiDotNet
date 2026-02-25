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

    [Fact]
    public void UrlBuilder_BuildsCorrectUrlWithPathParameters()
    {
        // Arrange
        var template = "/pets/{petId}/photos/{photoId}";
        var pathParams = new Dictionary<string, object?>
        {
            ["petId"] = 123,
            ["photoId"] = "abc-123"
        };

        // Act
        var url = UrlBuilder.Build(template, pathParams);

        // Assert
        url.Should().Be("/pets/123/photos/abc-123");
    }

    [Fact]
    public void UrlBuilder_EncodesSpecialCharactersInPath()
    {
        // Arrange
        var template = "/owners/{ownerId}/pets/{petId}";
        var pathParams = new Dictionary<string, object?>
        {
            ["ownerId"] = "john doe",
            ["petId"] = 456
        };

        // Act
        var url = UrlBuilder.Build(template, pathParams);

        // Assert
        url.Should().Contain("john%20doe");
        url.Should().Contain("456");
    }

    [Fact]
    public void UrlBuilder_BuildsUrlWithQueryParameters()
    {
        // Arrange
        var template = "/pets";
        var queryParams = new Dictionary<string, object?>
        {
            ["limit"] = 10,
            ["status"] = "available"
        };

        // Act
        var url = UrlBuilder.Build(template, queryParameters: queryParams);

        // Assert
        url.Should().StartWith("/pets?");
        url.Should().Contain("limit=10");
        url.Should().Contain("status=available");
    }

    [Fact]
    public void UrlBuilder_BuildsUrlWithPathAndQueryParameters()
    {
        // Arrange
        var template = "/pets/{petId}";
        var pathParams = new Dictionary<string, object?> { ["petId"] = 123 };
        var queryParams = new Dictionary<string, object?> { ["include"] = "photos" };

        // Act
        var url = UrlBuilder.Build(template, pathParams, queryParams);

        // Assert
        url.Should().Be("/pets/123?include=photos");
    }

    [Fact]
    public void UrlBuilder_HandlesNullQueryParameters()
    {
        // Arrange
        var template = "/pets";
        var queryParams = new Dictionary<string, object?>
        {
            ["limit"] = 10,
            ["status"] = null
        };

        // Act
        var url = UrlBuilder.Build(template, queryParameters: queryParams);

        // Assert
        url.Should().Be("/pets?limit=10");
        url.Should().NotContain("status");
    }

    [Fact]
    public void UrlBuilder_EncodesSpecialCharactersInQuery()
    {
        // Arrange
        var template = "/pets";
        var queryParams = new Dictionary<string, object?>
        {
            ["name"] = "Fluffy & Friends",
            ["tag"] = "cute+friendly"
        };

        // Act
        var url = UrlBuilder.Build(template, queryParameters: queryParams);

        // Assert
        url.Should().Contain("Fluffy%20%26%20Friends");
        url.Should().Contain("cute%2Bfriendly");
    }

    [Fact]
    public void UrlBuilder_HandlesArrayQueryParameters()
    {
        // Arrange
        var template = "/pets";
        var queryParams = new Dictionary<string, object?>
        {
            ["tags"] = new[] { "dog", "cat", "bird" }
        };

        // Act
        var url = UrlBuilder.Build(template, queryParameters: queryParams);

        // Assert
        url.Should().Contain("tags=dog");
        url.Should().Contain("tags=cat");
        url.Should().Contain("tags=bird");
    }

    [Fact]
    public void UrlBuilder_BuildQueryString_HandlesMultipleValues()
    {
        // Arrange
        var parameters = new Dictionary<string, object?>
        {
            ["id"] = new[] { 1, 2, 3 },
            ["name"] = "test"
        };

        // Act
        var queryString = UrlBuilder.BuildQueryString(parameters);

        // Assert
        queryString.Should().Contain("id=1");
        queryString.Should().Contain("id=2");
        queryString.Should().Contain("id=3");
        queryString.Should().Contain("name=test");
    }

    [Fact]
    public void UrlBuilder_EncodePath_EncodesSpecialCharacters()
    {
        // Act
        var encoded = UrlBuilder.EncodePath("hello world/test");

        // Assert
        encoded.Should().Be("hello%20world%2Ftest");
    }

    [Fact]
    public void UrlBuilder_EncodeQuery_EncodesSpecialCharacters()
    {
        // Act
        var encoded = UrlBuilder.EncodeQuery("value with spaces & special=chars");

        // Assert
        encoded.Should().Contain("%20");
        encoded.Should().Contain("%26");
        encoded.Should().Contain("%3D");
    }
}
