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

            // Assert
            var clientPath = Path.Combine(outputDirectory, "PetStoreAPIClient.cs");
            var content = File.ReadAllText(clientPath);

            // Should contain URL encoding for path parameter
            content.Should().Contain("Uri.EscapeDataString(petId.ToString())");
            content.Should().Contain("GetPetByIdAsync");
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

            // Assert
            var clientPath = Path.Combine(outputDirectory, "PetStoreAPIClient.cs");
            var content = File.ReadAllText(clientPath);

            // Should have method with multiple path parameters
            content.Should().Contain("GetPetPhotoAsync");
            content.Should().Contain("long petId");
            content.Should().Contain("Guid photoId");
            
            // Should encode both path parameters
            content.Should().Contain("Uri.EscapeDataString(petId.ToString())");
            content.Should().Contain("Uri.EscapeDataString(photoId.ToString())");
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

            // Assert
            var clientPath = Path.Combine(outputDirectory, "PetStoreAPIClient.cs");
            var content = File.ReadAllText(clientPath);

            // GetOwnerPet should have string ownerId and long petId
            content.Should().Contain("GetOwnerPetAsync");
            content.Should().Contain("string ownerId");
            content.Should().Contain("long petId");
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

            // Assert
            var clientPath = Path.Combine(outputDirectory, "PetStoreAPIClient.cs");
            var content = File.ReadAllText(clientPath);

            // ListPets should have query parameters with encoding
            content.Should().Contain("ListPetsAsync");
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
