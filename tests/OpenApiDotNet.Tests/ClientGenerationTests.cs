using FluentAssertions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace OpenApiDotNet.Tests;

public class ClientGenerationTests
{
    private readonly string _fixturesPath;
    private OpenApiReaderSettings _settings;

    public ClientGenerationTests()
    {
        // Get the path to the fixtures directory
        var baseDirectory = AppContext.BaseDirectory;
        _fixturesPath = Path.Combine(baseDirectory, "..", "..", "..", "Fixtures");
        _settings = new OpenApiReaderSettings();
        _settings.AddYamlReader();
    }

    [Fact]
    public async Task Generate_WithPetStoreSpec_CreatesExpectedFiles()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

            diagnostic?.Errors.Should().BeEmpty();

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
    public async Task Generate_PetModel_ContainsExpectedProperties()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);
            var generator = new ClientGenerator(document, "PetStore.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var petModelPath = Path.Combine(outputDirectory, "Models", "Pet.cs");
            var content = File.ReadAllText(petModelPath);

            // Check for expected properties with correct types
            content.Should().Contain("public required long Id");
            content.Should().Contain("public required string Name");
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
    public async Task Generate_ClientClass_ContainsExpectedMethods()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
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
    public async Task Generate_JsonConfiguration_ContainsNodaTimeSetupAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);
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
    public async Task Generate_WithQueryParameters_GeneratesProperQueryHandlingAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
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
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" }
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
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" }
        };

        // Act
        var act = () => new ClientGenerator(document, "TestNamespace", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("outputDirectory");
    }

    [Fact]
    public async Task Generate_WithEnumSchema_CreatesEnumFileAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);
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
    public async Task Generate_EnumModel_ContainsExpectedMembersAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);
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
    public async Task Generate_EnumWithHyphenatedValues_GeneratesPascalCaseMembersAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);
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
    public async Task Generate_PetModel_ContainsEnumPropertyAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);
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
    public async Task Generate_JsonConfiguration_ContainsStringEnumConverterAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "petstore.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

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

    [Fact]
    public async Task Generate_WithDottedSchemaNames_CreatesFilesInSubDirectoriesAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "dotted-names.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

            diagnostic.Errors.Should().BeEmpty();

            var generator = new ClientGenerator(document, "DottedNames.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert - dotted names produce subdirectories
            File.Exists(Path.Combine(outputDirectory, "Models", "Commerce", "Order.cs")).Should().BeTrue();
            File.Exists(Path.Combine(outputDirectory, "Models", "Commerce", "NewOrder.cs")).Should().BeTrue();
            File.Exists(Path.Combine(outputDirectory, "Models", "Commerce", "OrderStatus.cs")).Should().BeTrue();
            File.Exists(Path.Combine(outputDirectory, "Models", "Identity", "Customer.cs")).Should().BeTrue();

            // Non-dotted name stays in root Models directory
            File.Exists(Path.Combine(outputDirectory, "Models", "SimpleModel.cs")).Should().BeTrue();
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
    public async Task Generate_DottedModel_HasCorrectNamespaceAndTypeNameAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "dotted-names.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

            var generator = new ClientGenerator(document, "DottedNames.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var orderPath = Path.Combine(outputDirectory, "Models", "Commerce", "Order.cs");
            var content = File.ReadAllText(orderPath);

            // Type name should be just the last segment
            content.Should().Contain("public class Order");
            // Namespace should include the dotted prefix
            content.Should().Contain("namespace DottedNames.Client.Models.Commerce;");
            // Should have using for other sub-namespace
            content.Should().Contain("using DottedNames.Client.Models.Identity;");
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
    public async Task Generate_DottedEnum_HasCorrectNamespaceAndTypeNameAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "dotted-names.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

            var generator = new ClientGenerator(document, "DottedNames.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var enumPath = Path.Combine(outputDirectory, "Models", "Commerce", "OrderStatus.cs");
            var content = File.ReadAllText(enumPath);

            content.Should().Contain("public enum OrderStatus");
            content.Should().Contain("namespace DottedNames.Client.Models.Commerce;");
            content.Should().Contain("[JsonConverter(typeof(JsonStringEnumConverter))]");
            content.Should().Contain("Pending,");
            content.Should().Contain("Confirmed,");
            content.Should().Contain("Shipped,");
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
    public async Task Generate_DottedModel_ReferencesUseSimpleTypeNameAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "dotted-names.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

            var generator = new ClientGenerator(document, "DottedNames.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert - properties referencing dotted types use simple names
            var orderPath = Path.Combine(outputDirectory, "Models", "Commerce", "Order.cs");
            var content = File.ReadAllText(orderPath);

            content.Should().Contain("public required OrderStatus Status");
            content.Should().Contain("public Customer? Customer");
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
    public async Task Generate_Client_WithDottedNames_HasSubNamespaceUsingsAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "dotted-names.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

            var generator = new ClientGenerator(document, "DottedNames.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var clientPath = Path.Combine(outputDirectory, "DottedNamesAPIClient.cs");
            var content = File.ReadAllText(clientPath);

            content.Should().Contain("using DottedNames.Client.Models;");
            content.Should().Contain("using DottedNames.Client.Models.Commerce;");
            content.Should().Contain("using DottedNames.Client.Models.Identity;");

            // Methods should use simple type names
            content.Should().Contain("Task<List<Order>>");
            content.Should().Contain("Task<Order>");
            content.Should().Contain("NewOrder request");
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
    public async Task Generate_NonDottedModel_StaysInRootModelsNamespaceAsync()
    {
        // Arrange
        var specPath = Path.Combine(_fixturesPath, "dotted-names.yaml");
        var outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            using var stream = File.OpenRead(specPath);
            var (document, diagnostic) = await OpenApiDocument.LoadAsync(stream);

            var generator = new ClientGenerator(document, "DottedNames.Client", outputDirectory);

            // Act
            generator.Generate();

            // Assert
            var modelPath = Path.Combine(outputDirectory, "Models", "SimpleModel.cs");
            var content = File.ReadAllText(modelPath);

            content.Should().Contain("namespace DottedNames.Client.Models;");
            content.Should().Contain("public class SimpleModel");
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
