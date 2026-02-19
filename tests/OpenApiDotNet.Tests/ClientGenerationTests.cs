using FluentAssertions;
using Microsoft.OpenApi.Readers;
using OpenApiDotNet;

namespace OpenApiDotNet.Tests;

public class ClientGenerationTests
{
    private readonly string _fixturesPath;

    public ClientGenerationTests()
    {
        // Get the path to the fixtures directory
        var baseDirectory = AppContext.BaseDirectory;
        _fixturesPath = Path.Combine(baseDirectory, "..", "..", "..", "Fixtures");
    }

    [Fact]
    public void Generate_WithPetStoreSpec_CreatesExpectedFiles()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        try
        {
            using var stream = File.OpenRead(specPath);
            var reader = new OpenApiStreamReader();
            var document = reader.Read(stream, out var diagnostic);

            diagnostic.Errors.Should().BeEmpty();

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            Directory.Exists(outputDirectory).Should().BeTrue();
            Directory.Exists(Path.Combine(outputDirectory, "Models")).Should().BeTrue();

            // Check that model files were created
            File.Exists(Path.Combine(outputDirectory, "Models", "Pet.cs")).Should().BeTrue();
            File.Exists(Path.Combine(outputDirectory, "Models", "NewPet.cs")).Should().BeTrue();

            // Check that client file was created
            File.Exists(Path.Combine(outputDirectory, "PetStoreAPIClient.cs")).Should().BeTrue();

            // Check that JSON configuration was created
            File.Exists(Path.Combine(outputDirectory, "JsonConfiguration.cs")).Should().BeTrue();
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
        }
    }

    [Fact]
    public void Generate_PetModel_ContainsExpectedProperties()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        try
        {
            using var stream = File.OpenRead(specPath);
            var reader = new OpenApiStreamReader();
            var document = reader.Read(stream, out _);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var petModelPath = Path.Combine(outputDirectory, "Models", "Pet.cs");
            var content = File.ReadAllText(petModelPath);

            // Check for expected properties with correct types
            content.Should().Contain("public long Id");
            content.Should().Contain("public string Name");
            content.Should().Contain("public string? Tag");
            content.Should().Contain("public LocalDate? BirthDate");
            content.Should().Contain("public Instant? CreatedAt");
            content.Should().Contain("public bool? Vaccinated");
            content.Should().Contain("public double? Weight");

            // Check for NodaTime using statement
            content.Should().Contain("using NodaTime;");

            // Check for JSON attributes
            content.Should().Contain("[JsonPropertyName(\"id\")]");
            content.Should().Contain("[JsonPropertyName(\"birthDate\")]");
            content.Should().Contain("[JsonPropertyName(\"createdAt\")]");
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
    public void Generate_ClientClass_ContainsExpectedMethods()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        try
        {
            using var stream = File.OpenRead(specPath);
            var reader = new OpenApiStreamReader();
            var document = reader.Read(stream, out _);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var clientPath = Path.Combine(outputDirectory, "PetStoreAPIClient.cs");
            var content = File.ReadAllText(clientPath);

            // Check for expected methods based on operationIds
            content.Should().Contain("public async Task<List<Pet>> ListPetsAsync");
            content.Should().Contain("public async Task<Pet> CreatePetAsync");
            content.Should().Contain("public async Task<Pet> GetPetByIdAsync");
            content.Should().Contain("public async Task<void> DeletePetAsync");

            // Check for proper parameters
            content.Should().Contain("int? limit");
            content.Should().Contain("long petId");
            content.Should().Contain("NewPet request");
            content.Should().Contain("CancellationToken cancellationToken = default");

            // Check for HttpClient usage
            content.Should().Contain("private readonly HttpClient _httpClient;");
            content.Should().Contain("_httpClient.GetAsync");
            content.Should().Contain("_httpClient.PostAsJsonAsync");
            content.Should().Contain("_httpClient.DeleteAsync");
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
    public void Generate_JsonConfiguration_ContainsNodaTimeSetup()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        try
        {
            using var stream = File.OpenRead(specPath);
            var reader = new OpenApiStreamReader();
            var document = reader.Read(stream, out _);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var configPath = Path.Combine(outputDirectory, "JsonConfiguration.cs");
            var content = File.ReadAllText(configPath);

            // Check for NodaTime configuration
            content.Should().Contain("using NodaTime;");
            content.Should().Contain("using NodaTime.Serialization.SystemTextJson;");
            content.Should().Contain("ConfigureForNodaTime");
            content.Should().Contain("DateTimeZoneProviders.Tzdb");

            // Check for JSON options
            content.Should().Contain("JsonSerializerOptions");
            content.Should().Contain("PropertyNamingPolicy = JsonNamingPolicy.CamelCase");
            content.Should().Contain("DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull");
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
    public void Generate_WithQueryParameters_GeneratesProperQueryHandling()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        try
        {
            using var stream = File.OpenRead(specPath);
            var reader = new OpenApiStreamReader();
            var document = reader.Read(stream, out _);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var clientPath = Path.Combine(outputDirectory, "PetStoreAPIClient.cs");
            var content = File.ReadAllText(clientPath);

            // Check for query parameter handling in ListPets method
            content.Should().Contain("var queryString = new List<string>();");
            content.Should().Contain("if (limit != null)");
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
    public void Constructor_WithNullDocument_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ClientGenerator(null!, "TestNamespace", Path.GetTempPath());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public void Constructor_WithNullNamespace_ThrowsArgumentNullException()
    {
        // Arrange
        var document = new Microsoft.OpenApi.Models.OpenApiDocument
        {
            Info = new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Test", Version = "1.0" }
        };

        // Act
        var act = () => new ClientGenerator(document, null!, Path.GetTempPath());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("namespaceName");
    }

    [Fact]
    public void Constructor_WithNullOutputDirectory_ThrowsArgumentNullException()
    {
        // Arrange
        var document = new Microsoft.OpenApi.Models.OpenApiDocument
        {
            Info = new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Test", Version = "1.0" }
        };

        // Act
        var act = () => new ClientGenerator(document, "TestNamespace", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("outputDirectory");
    }

    [Fact]
    public void Generate_WithEnumSchema_CreatesEnumFile()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var reader = new OpenApiStreamReader();
            var document = reader.Read(stream, out _);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            File.Exists(Path.Combine(outputDirectory, "Models", "PetStatus.cs")).Should().BeTrue();
            File.Exists(Path.Combine(outputDirectory, "Models", "PetSize.cs")).Should().BeTrue();
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
    public void Generate_EnumModel_ContainsExpectedMembers()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var reader = new OpenApiStreamReader();
            var document = reader.Read(stream, out _);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var enumPath = Path.Combine(outputDirectory, "Models", "PetStatus.cs");
            var content = File.ReadAllText(enumPath);

            content.Should().Contain("public enum PetStatus");
            content.Should().Contain("[JsonConverter(typeof(JsonStringEnumConverter))]");
            content.Should().Contain("Available,");
            content.Should().Contain("Pending,");
            content.Should().Contain("Sold,");
            content.Should().Contain("using System.Text.Json.Serialization;");
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
    public void Generate_EnumWithHyphenatedValues_GeneratesPascalCaseMembers()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var reader = new OpenApiStreamReader();
            var document = reader.Read(stream, out _);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var enumPath = Path.Combine(outputDirectory, "Models", "PetSize.cs");
            var content = File.ReadAllText(enumPath);

            content.Should().Contain("public enum PetSize");
            content.Should().Contain("Small,");
            content.Should().Contain("Medium,");
            content.Should().Contain("Large,");
            content.Should().Contain("ExtraLarge,");
            content.Should().Contain("[JsonStringEnumMemberName(\"extra-large\")]");
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
    public void Generate_PetModel_ContainsEnumProperty()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var reader = new OpenApiStreamReader();
            var document = reader.Read(stream, out _);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var petPath = Path.Combine(outputDirectory, "Models", "Pet.cs");
            var content = File.ReadAllText(petPath);

            content.Should().Contain("public PetStatus? Status");
            content.Should().Contain("public PetSize? Size");
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
    public void Generate_JsonConfiguration_ContainsStringEnumConverter()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var reader = new OpenApiStreamReader();
            var document = reader.Read(stream, out _);

            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var configPath = Path.Combine(outputDirectory, "JsonConfiguration.cs");
            var content = File.ReadAllText(configPath);

            content.Should().Contain("JsonStringEnumConverter");
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
