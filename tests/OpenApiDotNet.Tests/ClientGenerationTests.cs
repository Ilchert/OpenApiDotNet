using System.Text;
using FluentAssertions;
using Microsoft.OpenApi;

namespace OpenApiDotNet.Tests;

public class ClientGenerationTests : IDisposable
{
    private string _outputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    private ClientGenerator CreateGenerator(
        string specJson,
        string namespaceName = "Test.Client",
        string? namespacePrefix = null)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(specJson));
        var (document, diagnostic) = OpenApiDocument.Load(stream);
        diagnostic?.Errors.Should().BeEmpty();
        return new ClientGenerator(document, namespaceName, _outputDirectory, namespacePrefix: namespacePrefix);
    }

    public void Dispose()
    {
        if (_outputDirectory is not null && Directory.Exists(_outputDirectory))
            Directory.Delete(_outputDirectory, true);
    }

    [Fact]
    public void Generate_WithPetStoreSpec_CreatesExpectedFiles()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Pet Store API", "version": "1.0.0" },
              "paths": {
                "/pets": {
                  "get": { "operationId": "listPets", "responses": { "200": { "description": "ok" } } }
                }
              },
              "components": {
                "schemas": {
                  "Pet": { "type": "object", "properties": { "id": { "type": "integer", "format": "int64" } } },
                  "NewPet": { "type": "object", "properties": { "name": { "type": "string" } } }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec, "PetStore.Client");

        generator.Generate();

        Directory.Exists(_outputDirectory).Should().BeTrue();
        Directory.Exists(Path.Combine(_outputDirectory, "Models")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Models", "Pet.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Models", "NewPet.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "IBuilder.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "IClient.cs")).Should().BeTrue();
        Directory.Exists(Path.Combine(_outputDirectory, "Builders")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Builders", "PetsBuilder.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "JsonConfiguration.cs")).Should().BeTrue();
    }

    [Fact]
    public void Generate_PetModel_ContainsExpectedProperties()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Pet": {
                    "type": "object",
                    "required": ["id", "name"],
                    "properties": {
                      "id": { "type": "integer", "format": "int64" },
                      "name": { "type": "string" },
                      "tag": { "type": "string" },
                      "birthDate": { "type": "string", "format": "date" },
                      "createdAt": { "type": "string", "format": "date-time" },
                      "vaccinated": { "type": "boolean" },
                      "weight": { "type": "number", "format": "double" }
                    }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "Pet.cs"));

        content.Should().Contain("public required long Id");
        content.Should().Contain("public required string Name");
        content.Should().Contain("public string? Tag");
        content.Should().Contain("public NodaTime.LocalDate? BirthDate");
        content.Should().Contain("public NodaTime.Instant? CreatedAt");
        content.Should().Contain("public bool? Vaccinated");
        content.Should().Contain("public double? Weight");
        content.Should().NotContain("using NodaTime;");
        content.Should().Contain("[JsonPropertyName(\"id\")]");
        content.Should().Contain("[JsonPropertyName(\"birthDate\")]");
        content.Should().Contain("[JsonPropertyName(\"createdAt\")]");
    }

    [Fact]
    public void Generate_ClientClass_ContainsExpectedMethods()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Pet Store API", "version": "1.0.0" },
              "paths": {
                "/pets": {
                  "get": {
                    "operationId": "listPets",
                    "parameters": [{ "name": "limit", "in": "query", "required": false, "schema": { "type": "integer", "format": "int32" } }],
                    "responses": { "200": { "description": "ok", "content": { "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/Pet" } } } } } }
                  },
                  "post": {
                    "operationId": "createPet",
                    "requestBody": { "required": true, "content": { "application/json": { "schema": { "$ref": "#/components/schemas/NewPet" } } } },
                    "responses": { "201": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Pet" } } } } }
                  }
                },
                "/pets/{petId}": {
                  "get": {
                    "operationId": "getPetById",
                    "parameters": [{ "name": "petId", "in": "path", "required": true, "schema": { "type": "integer", "format": "int64" } }],
                    "responses": { "200": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Pet" } } } } }
                  },
                  "delete": {
                    "operationId": "deletePet",
                    "parameters": [{ "name": "petId", "in": "path", "required": true, "schema": { "type": "integer", "format": "int64" } }],
                    "responses": { "204": { "description": "ok" } }
                  }
                }
              },
              "components": {
                "schemas": {
                  "Pet": { "type": "object", "properties": { "id": { "type": "integer", "format": "int64" } } },
                  "NewPet": { "type": "object", "properties": { "name": { "type": "string" } } }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec, "PetStore.Client");

        generator.Generate();

        // IClient should have navigation property for Pets
        var clientContent = File.ReadAllText(Path.Combine(_outputDirectory, "IClient.cs"));
        clientContent.Should().Contain("PetsBuilder Pets");
        clientContent.Should().Contain("HttpClient HttpClient");
        clientContent.Should().Contain("JsonSerializerOptions JsonOptions");

        // PetsBuilder should have listPets and createPet operations
        var petsContent = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "PetsBuilder.cs"));
        petsContent.Should().Contain("public virtual async Task<List<Pet>> ListPets");
        petsContent.Should().Contain("public virtual async Task<Pet> CreatePet");
        petsContent.Should().Contain("int? limit");
        petsContent.Should().Contain("NewPet request");
        petsContent.Should().Contain("CancellationToken cancellationToken = default");
        petsContent.Should().Contain("Client.HttpClient.GetAsync");
        petsContent.Should().Contain("Client.HttpClient.PostAsJsonAsync");
        petsContent.Should().Contain("PetsIdBuilder this[long petId]");

        // PetsIdBuilder should have getPetById and deletePet operations
        var petsIdContent = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "PetsIdBuilder.cs"));
        petsIdContent.Should().Contain("public virtual async Task<Pet> GetPetById");
        petsIdContent.Should().Contain("public virtual async Task DeletePet");
        petsIdContent.Should().Contain("Client.HttpClient.DeleteAsync");
    }

    [Fact]
    public void Generate_JsonConfiguration_ContainsNodaTimeSetupAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {}
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "JsonConfiguration.cs"));
        content.Should().NotContain("using NodaTime;");
        content.Should().Contain("using NodaTime.Serialization.SystemTextJson;");
        content.Should().Contain("ConfigureForNodaTime");
        content.Should().Contain("NodaTime.DateTimeZoneProviders.Tzdb");
        content.Should().Contain("JsonSerializerOptions");
        content.Should().Contain("PropertyNamingPolicy = JsonNamingPolicy.CamelCase");
        content.Should().Contain("DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull");
    }

    [Fact]
    public void Generate_WithQueryParameters_GeneratesProperQueryHandlingAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/items": {
                  "get": {
                    "operationId": "listItems",
                    "parameters": [{ "name": "limit", "in": "query", "required": false, "schema": { "type": "integer", "format": "int32" } }],
                    "responses": { "200": { "description": "ok" } }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "ItemsBuilder.cs"));
        content.Should().Contain("var queryString = new List<string>();");
        content.Should().Contain("if (limit != null)");
        content.Should().Contain("Uri.EscapeDataString");
    }

    [Fact]
    public void Generate_WithRequiredQueryParameter_GeneratesNonNullableParameterAndAlwaysAddsToQuery()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/items": {
                  "get": {
                    "operationId": "listItems",
                    "parameters": [
                      { "name": "category", "in": "query", "required": true, "schema": { "type": "string" } },
                      { "name": "limit", "in": "query", "required": false, "schema": { "type": "integer", "format": "int32" } }
                    ],
                    "responses": { "200": { "description": "ok" } }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "ItemsBuilder.cs"));

        // Required parameter should be non-nullable
        content.Should().Contain("string category");
        content.Should().NotContain("string? category");
        content.Should().NotContain("string category = default");
        
        // Optional parameter should be nullable
        content.Should().Contain("int? limit");

        // Required parameter should always be added to query string (no null check)
        content.Should().Contain("Uri.EscapeDataString(category.ToString())");
        content.Should().NotContain("if (category != null)");

        // Optional parameter should have null check
        content.Should().Contain("if (limit != null)");
    }

    [Fact]
    public void Generate_WithListQueryParameter_GeneratesForeachOverItems()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/items": {
                  "get": {
                    "operationId": "listItems",
                    "parameters": [
                      { "name": "tags", "in": "query", "required": false, "schema": { "type": "array", "items": { "type": "string" } } },
                      { "name": "statuses", "in": "query", "required": true, "schema": { "type": "array", "items": { "type": "string" } } },
                      { "name": "limit", "in": "query", "required": false, "schema": { "type": "integer", "format": "int32" } }
                    ],
                    "responses": { "200": { "description": "ok" } }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "ItemsBuilder.cs"));

        // Optional list parameter should be nullable
        content.Should().Contain("List<string>? tags");

        // Required list parameter should be non-nullable
        content.Should().Contain("List<string> statuses");
        content.Should().NotContain("List<string>? statuses");

        // Optional list parameter should have null check before foreach
        content.Should().Contain("if (tags != null)");
        content.Should().Contain("foreach (var item in tags)");

        // Required list parameter should iterate without null check
        content.Should().Contain("foreach (var item in statuses)");

        // Each item should be individually escaped and added with the parameter name
        content.Should().Contain("Uri.EscapeDataString(item.ToString())");

        // Scalar parameter should still use direct .ToString()
        content.Should().Contain("if (limit != null)");
        content.Should().Contain("Uri.EscapeDataString(limit.ToString())");
    }

    [Fact]
    public void Generate_WithMixedRequiredAndOptionalQueryParameters_RequiredParametersGoFirst()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/items": {
                  "post": {
                    "operationId": "searchItems",
                    "parameters": [
                      { "name": "limit", "in": "query", "required": false, "schema": { "type": "integer", "format": "int32" } },
                      { "name": "category", "in": "query", "required": true, "schema": { "type": "string" } },
                      { "name": "offset", "in": "query", "required": false, "schema": { "type": "integer", "format": "int32" } }
                    ],
                    "requestBody": { "required": true, "content": { "application/json": { "schema": { "$ref": "#/components/schemas/SearchRequest" } } } },
                    "responses": { "200": { "description": "ok" } }
                  }
                }
              },
              "components": {
                "schemas": {
                  "SearchRequest": { "type": "object", "properties": { "query": { "type": "string" } } }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "ItemsBuilder.cs"));

        // Required parameters (category, request) should appear before optional ones (limit, offset, cancellationToken)
        var signatureStart = content.IndexOf("SearchItems(");
        var signatureEnd = content.IndexOf(')', signatureStart);
        var signature = content[signatureStart..signatureEnd];

        var categoryPos = signature.IndexOf("string category");
        var requestPos = signature.IndexOf("SearchRequest request");
        var limitPos = signature.IndexOf("int? limit");
        var offsetPos = signature.IndexOf("int? offset");
        var ctPos = signature.IndexOf("CancellationToken cancellationToken");

        categoryPos.Should().BePositive();
        requestPos.Should().BePositive();
        limitPos.Should().BePositive();
        offsetPos.Should().BePositive();
        ctPos.Should().BePositive();

        // Required params before optional params
        categoryPos.Should().BeLessThan(limitPos);
        categoryPos.Should().BeLessThan(offsetPos);
        requestPos.Should().BeLessThan(limitPos);
        requestPos.Should().BeLessThan(offsetPos);
        requestPos.Should().BeLessThan(ctPos);
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
    public void Generate_WithEnumSchema_CreatesEnumFileAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "PetStatus": { "type": "string", "enum": ["available", "pending", "sold"] },
                  "PetSize": { "type": "string", "enum": ["small", "medium", "large", "extra-large"] }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        File.Exists(Path.Combine(_outputDirectory, "Models", "PetStatus.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Models", "PetSize.cs")).Should().BeTrue();
    }

    [Fact]
    public void Generate_EnumModel_ContainsExpectedMembersAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "PetStatus": {
                    "type": "string",
                    "description": "The status of a pet in the store",
                    "enum": ["available", "pending", "sold"]
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "PetStatus.cs"));
        content.Should().Contain("public enum PetStatus");
        content.Should().Contain("[JsonConverter(typeof(JsonStringEnumConverter))]");
        content.Should().Contain("Available,");
        content.Should().Contain("Pending,");
        content.Should().Contain("Sold,");
        content.Should().Contain("using System.Text.Json.Serialization;");
    }

    [Fact]
    public void Generate_EnumWithHyphenatedValues_GeneratesPascalCaseMembersAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "PetSize": { "type": "string", "enum": ["small", "medium", "large", "extra-large"] }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "PetSize.cs"));
        content.Should().Contain("public enum PetSize");
        content.Should().Contain("Small,");
        content.Should().Contain("Medium,");
        content.Should().Contain("Large,");
        content.Should().Contain("ExtraLarge,");
        content.Should().Contain("[JsonStringEnumMemberName(\"extra-large\")]");
    }

    [Fact]
    public void Generate_PetModel_ContainsEnumPropertyAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Pet": {
                    "type": "object",
                    "properties": {
                      "status": { "$ref": "#/components/schemas/PetStatus" },
                      "size": { "$ref": "#/components/schemas/PetSize" }
                    }
                  },
                  "PetStatus": { "type": "string", "enum": ["available"] },
                  "PetSize": { "type": "string", "enum": ["small"] }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "Pet.cs"));
        content.Should().Contain("public PetStatus? Status");
        content.Should().Contain("public PetSize? Size");
    }

    [Fact]
    public void Generate_JsonConfiguration_ContainsStringEnumConverterAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {}
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "JsonConfiguration.cs"));
        content.Should().Contain("JsonStringEnumConverter");
    }

    [Fact]
    public void Generate_WithDottedSchemaNames_CreatesFilesInSubDirectoriesAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Dotted Names API", "version": "1.0.0" },
              "paths": {
                "/orders": {
                  "get": { "operationId": "listOrders", "responses": { "200": { "description": "ok" } } }
                }
              },
              "components": {
                "schemas": {
                  "Commerce.Order": { "type": "object", "properties": { "id": { "type": "integer", "format": "int64" } } },
                  "Commerce.NewOrder": { "type": "object", "properties": { "customerId": { "type": "integer", "format": "int64" } } },
                  "Commerce.OrderStatus": { "type": "string", "enum": ["pending", "confirmed", "shipped"] },
                  "Identity.Customer": { "type": "object", "properties": { "id": { "type": "integer", "format": "int64" } } },
                  "SimpleModel": { "type": "object", "properties": { "value": { "type": "string" } } }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec, "DottedNames.Client");

        generator.Generate();

        File.Exists(Path.Combine(_outputDirectory, "Models", "Commerce", "Order.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Models", "Commerce", "NewOrder.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Models", "Commerce", "OrderStatus.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Models", "Identity", "Customer.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Models", "SimpleModel.cs")).Should().BeTrue();
    }

    [Fact]
    public void Generate_DottedModel_HasCorrectNamespaceAndTypeNameAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Commerce.Order": {
                    "type": "object",
                    "properties": { "id": { "type": "integer", "format": "int64" } }
                  },
                  "Identity.Customer": {
                    "type": "object",
                    "properties": { "id": { "type": "integer", "format": "int64" } }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec, "DottedNames.Client");

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "Commerce", "Order.cs"));
        content.Should().Contain("public class Order");
        content.Should().Contain("namespace DottedNames.Client.Models.Commerce;");
        content.Should().Contain("using DottedNames.Client.Models.Identity;");
    }

    [Fact]
    public void Generate_DottedEnum_HasCorrectNamespaceAndTypeNameAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Commerce.OrderStatus": { "type": "string", "enum": ["pending", "confirmed", "shipped"] }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec, "DottedNames.Client");

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "Commerce", "OrderStatus.cs"));
        content.Should().Contain("public enum OrderStatus");
        content.Should().Contain("namespace DottedNames.Client.Models.Commerce;");
        content.Should().Contain("[JsonConverter(typeof(JsonStringEnumConverter))]");
        content.Should().Contain("Pending,");
        content.Should().Contain("Confirmed,");
        content.Should().Contain("Shipped,");
    }

    [Fact]
    public void Generate_DottedModel_ReferencesUseSimpleTypeNameAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Commerce.Order": {
                    "type": "object",
                    "required": ["id", "status"],
                    "properties": {
                      "id": { "type": "integer", "format": "int64" },
                      "status": { "$ref": "#/components/schemas/Commerce.OrderStatus" },
                      "customer": { "$ref": "#/components/schemas/Identity.Customer" }
                    }
                  },
                  "Commerce.OrderStatus": { "type": "string", "enum": ["pending"] },
                  "Identity.Customer": {
                    "type": "object",
                    "required": ["id"],
                    "properties": { "id": { "type": "integer", "format": "int64" } }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec, "DottedNames.Client");

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "Commerce", "Order.cs"));
        content.Should().Contain("public required OrderStatus Status");
        content.Should().Contain("public Customer? Customer");
    }

    [Fact]
    public void Generate_Client_WithDottedNames_HasSubNamespaceUsingsAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Dotted Names API", "version": "1.0.0" },
              "paths": {
                "/orders": {
                  "get": {
                    "operationId": "listOrders",
                    "responses": { "200": { "description": "ok", "content": { "application/json": { "schema": { "type": "array", "items": { "$ref": "#/components/schemas/Commerce.Order" } } } } } }
                  },
                  "post": {
                    "operationId": "createOrder",
                    "requestBody": { "required": true, "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Commerce.NewOrder" } } } },
                    "responses": { "201": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Commerce.Order" } } } } }
                  }
                }
              },
              "components": {
                "schemas": {
                  "Commerce.Order": { "type": "object", "properties": { "id": { "type": "integer", "format": "int64" } } },
                  "Commerce.NewOrder": { "type": "object", "properties": { "customerId": { "type": "integer", "format": "int64" } } },
                  "Identity.Customer": { "type": "object", "properties": { "id": { "type": "integer", "format": "int64" } } }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec, "DottedNames.Client");

        generator.Generate();

        var builderContent = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "OrdersBuilder.cs"));
        builderContent.Should().Contain("using DottedNames.Client.Models;");
        builderContent.Should().Contain("using DottedNames.Client.Models.Commerce;");
        builderContent.Should().Contain("using DottedNames.Client.Models.Identity;");
        builderContent.Should().Contain("Task<List<Order>>");
        builderContent.Should().Contain("Task<Order>");
        builderContent.Should().Contain("NewOrder request");
    }

    [Fact]
    public void Generate_NonDottedModel_StaysInRootModelsNamespaceAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "SimpleModel": { "type": "object", "properties": { "value": { "type": "string" } } }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec, "DottedNames.Client");

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "SimpleModel.cs"));
        content.Should().Contain("namespace DottedNames.Client.Models;");
        content.Should().Contain("public class SimpleModel");
    }

    [Fact]
    public void Generate_WithNamespacePrefix_StripsMatchingPrefixFromNamespaceAsync()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Commerce.Order": { "type": "object", "properties": { "id": { "type": "integer", "format": "int64" } } },
                  "Commerce.NewOrder": { "type": "object", "properties": { "customerId": { "type": "integer", "format": "int64" } } },
                  "Commerce.OrderStatus": { "type": "string", "enum": ["pending"] },
                  "Identity.Customer": {
                    "type": "object",
                    "required": ["id", "name"],
                    "properties": {
                      "id": { "type": "integer", "format": "int64" },
                      "name": { "type": "string" }
                    }
                  },
                  "SimpleModel": { "type": "object", "properties": { "value": { "type": "string" } } }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec, "DottedNames.Client", namespacePrefix: "Commerce");

        generator.Generate();

        File.Exists(Path.Combine(_outputDirectory, "Models", "Order.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Models", "NewOrder.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Models", "OrderStatus.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Models", "Identity", "Customer.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Models", "SimpleModel.cs")).Should().BeTrue();

        var orderContent = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "Order.cs"));
        orderContent.Should().Contain("namespace DottedNames.Client.Models;");
        orderContent.Should().Contain("public class Order");

        var customerContent = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "Identity", "Customer.cs"));
        customerContent.Should().Contain("namespace DottedNames.Client.Models.Identity;");
    }
}
