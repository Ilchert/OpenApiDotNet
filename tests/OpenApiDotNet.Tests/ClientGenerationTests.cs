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
        File.Exists(Path.Combine(_outputDirectory, "IOpenApiBuilder.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "IOpenApiClient.cs")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "IPetStoreAPIClient.cs")).Should().BeTrue();
        Directory.Exists(Path.Combine(_outputDirectory, "Builders")).Should().BeTrue();
        File.Exists(Path.Combine(_outputDirectory, "Builders", "PetsBuilder.cs")).Should().BeTrue();
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
        content.Should().Contain("[System.Text.Json.Serialization.JsonPropertyName(\"id\")]");
        content.Should().Contain("[System.Text.Json.Serialization.JsonPropertyName(\"birthDate\")]");
        content.Should().Contain("[System.Text.Json.Serialization.JsonPropertyName(\"createdAt\")]");
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

        // IOpenApiClient should have HttpClient and JsonOptions but no navigation properties
        var clientContent = File.ReadAllText(Path.Combine(_outputDirectory, "IOpenApiClient.cs"));
        clientContent.Should().Contain("HttpClient HttpClient");
        clientContent.Should().Contain("JsonSerializerOptions JsonOptions");
        clientContent.Should().NotContain("PetsBuilder Pets");

        // Named client interface should have navigation property for Pets
        var namedClientContent = File.ReadAllText(Path.Combine(_outputDirectory, "IPetStoreAPIClient.cs"));
        namedClientContent.Should().Contain("PetsBuilder Pets");
        namedClientContent.Should().Contain(": IOpenApiClient");

        // PetsBuilder should have Get and Post operations
        var petsContent = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "PetsBuilder.cs"));
        petsContent.Should().Contain("public virtual async Task<List<PetStore.Client.Models.Pet>> Get");
        petsContent.Should().Contain("public virtual async Task<PetStore.Client.Models.Pet> Post");
        petsContent.Should().Contain("int? limit");
        petsContent.Should().Contain("PetStore.Client.Models.NewPet request");
        petsContent.Should().Contain("CancellationToken cancellationToken = default");
        petsContent.Should().Contain("Client.HttpClient.GetAsync");
        petsContent.Should().Contain("Client.HttpClient.PostAsJsonAsync");
        petsContent.Should().Contain("PetsIdBuilder this[long petId]");

        // PetsIdBuilder should have Get and Delete operations
        var petsIdContent = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "PetsIdBuilder.cs"));
        petsIdContent.Should().Contain("public virtual async Task<PetStore.Client.Models.Pet> Get");
        petsIdContent.Should().Contain("public virtual async Task Delete");
        petsIdContent.Should().Contain("Client.HttpClient.DeleteAsync");
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
        content.Should().Contain("if (limit is {} limitValue)");
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

        // Optional parameter should have null check using pattern matching (avoids CS8604)
        content.Should().Contain("if (limit is {} limitValue)");
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

        // Scalar parameter should use pattern matching to avoid CS8604
        content.Should().Contain("if (limit is {} limitValue)");
        content.Should().Contain("Uri.EscapeDataString(limitValue.ToString())");
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
        var signatureStart = content.IndexOf("Post(");
        var signatureEnd = content.IndexOf(')', signatureStart);
        var signature = content[signatureStart..signatureEnd];

        var categoryPos = signature.IndexOf("string category");
        var requestPos = signature.IndexOf("Test.Client.Models.SearchRequest request");
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
    public void Generate_WithInlineObjectSchemaInResponse_GeneratesNestedClass()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/stats": {
                  "get": {
                    "operationId": "getStats",
                    "summary": "Get statistics",
                    "responses": {
                      "200": {
                        "description": "Statistics",
                        "content": {
                          "application/json": {
                            "schema": {
                              "type": "object",
                              "properties": {
                                "totalCount": { "type": "integer", "format": "int32" },
                                "activeCount": { "type": "integer", "format": "int32" },
                                "lastUpdated": { "type": "string", "format": "date-time" }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "StatsBuilder.cs"));

        // Return type should reference the nested class
        content.Should().Contain("Task<GetResponse>");
        content.Should().Contain("Get");

        // Nested class should be generated with properties
        content.Should().Contain("public class GetResponse");
        content.Should().Contain("[System.Text.Json.Serialization.JsonPropertyName(\"totalCount\")]");
        content.Should().Contain("public int? TotalCount");
        content.Should().Contain("[System.Text.Json.Serialization.JsonPropertyName(\"activeCount\")]");
        content.Should().Contain("public int? ActiveCount");
        content.Should().Contain("[System.Text.Json.Serialization.JsonPropertyName(\"lastUpdated\")]");
        content.Should().Contain("public NodaTime.Instant? LastUpdated");
    }

    [Fact]
    public void Generate_WithInlineObjectSchemaInRequestBody_GeneratesNestedClass()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/feedback": {
                  "post": {
                    "operationId": "submitFeedback",
                    "requestBody": {
                      "required": true,
                      "content": {
                        "application/json": {
                          "schema": {
                            "type": "object",
                            "required": ["message"],
                            "properties": {
                              "message": { "type": "string" },
                              "rating": { "type": "integer", "format": "int32" }
                            }
                          }
                        }
                      }
                    },
                    "responses": { "204": { "description": "ok" } }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "FeedbackBuilder.cs"));

        // Request body should use the nested class
        content.Should().Contain("PostRequest request");

        // Nested class should be generated with properties
        content.Should().Contain("public class PostRequest");
        content.Should().Contain("[System.Text.Json.Serialization.JsonPropertyName(\"message\")]");
        content.Should().Contain("public required string Message");
        content.Should().Contain("[System.Text.Json.Serialization.JsonPropertyName(\"rating\")]");
        content.Should().Contain("public int? Rating");
    }

    [Fact]
    public void Generate_WithArraySchemaInRequestBody_GeneratesListParameter()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/numbers": {
                  "post": {
                    "operationId": "submitNumbers",
                    "requestBody": {
                      "required": true,
                      "content": {
                        "application/json": {
                          "schema": {
                            "type": "array",
                            "items": {
                              "type": "integer",
                              "format": "int32"
                            }
                          }
                        }
                      }
                    },
                    "responses": { "204": { "description": "ok" } }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "NumbersBuilder.cs"));

        content.Should().Contain("List<int> request");
    }

    [Fact]
    public void Generate_WithXBodyNameExtension_UsesCustomParameterName()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/pets": {
                  "post": {
                    "operationId": "createPet",
                    "requestBody": {
                      "x-bodyName": "newPet",
                      "required": true,
                      "content": {
                        "application/json": {
                          "schema": { "$ref": "#/components/schemas/Pet" }
                        }
                      }
                    },
                    "responses": { "204": { "description": "ok" } }
                  }
                }
              },
              "components": {
                "schemas": {
                  "Pet": { "type": "object", "properties": { "name": { "type": "string" } } }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "PetsBuilder.cs"));

        content.Should().Contain("newPet");
        content.Should().NotContain("Pet request");
        content.Should().Contain("PostAsJsonAsync(url, newPet,");
    }

    [Fact]
    public void Generate_DeleteWithResponseBody_GeneratesReturnStatement()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/items/{id}": {
                  "delete": {
                    "operationId": "deleteItem",
                    "parameters": [{ "name": "id", "in": "path", "required": true, "schema": { "type": "integer", "format": "int64" } }],
                    "responses": {
                      "200": {
                        "description": "OK",
                        "content": {
                          "application/json": {
                            "schema": { "$ref": "#/components/schemas/Item" }
                          }
                        }
                      }
                    }
                  }
                }
              },
              "components": {
                "schemas": {
                  "Item": { "type": "object", "properties": { "id": { "type": "integer", "format": "int64" } } }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "ItemsIdBuilder.cs"));

        // Return type should be the referenced model
        content.Should().Contain("Task<Test.Client.Models.Item>");

        // Should read from JSON and return the result
        content.Should().Contain("ReadFromJsonAsync<Test.Client.Models.Item>");
        content.Should().Contain("Client.HttpClient.DeleteAsync");
    }

    [Fact]
    public void Generate_WithObjectReturnType_GeneratesTaskOfObject()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/data": {
                  "get": {
                    "operationId": "getData",
                    "summary": "Get data",
                    "responses": {
                      "200": {
                        "description": "OK",
                        "content": {
                          "application/json": {
                            "schema": {
                              "type": "object"
                            }
                          },
                          "text/json": {
                            "schema": {
                              "type": "object"
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "DataBuilder.cs"));

        // Return type should be object (not void, not a nested class)
        content.Should().Contain("Task<object>");
        content.Should().NotContain("Task<GetResponse>");
        content.Should().NotContain("public class GetResponse");

        // Should read from JSON as object
        content.Should().Contain("ReadFromJsonAsync<object>");
    }

    [Fact]
    public void Generate_WithBooleanResponseType_GeneratesCorrectDeserializationCode()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {
                "/items/{id}": {
                  "get": {
                    "operationId": "checkItem",
                    "parameters": [{ "name": "id", "in": "path", "required": true, "schema": { "type": "integer", "format": "int64" } }],
                    "responses": {
                      "200": {
                        "description": "OK",
                        "content": {
                          "application/json": {
                            "schema": { "type": "boolean" }
                          },
                          "text/json": {
                            "schema": { "type": "boolean" }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Builders", "ItemsIdBuilder.cs"));

        // Return type should be bool
        content.Should().Contain("Task<bool>");

        // Uses is {} pattern which works uniformly for both value types and reference types
        content.Should().Contain("var deserializedResponse = await response.Content.ReadFromJsonAsync<bool>(Client.JsonOptions, cancellationToken);");
        content.Should().Contain("if (deserializedResponse is { } deserializedResponseValue)");
        content.Should().Contain("    return deserializedResponseValue;");
        content.Should().Contain("throw new InvalidOperationException($\"Response from {url} is null\");");
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
        content.Should().Contain("[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]");
        content.Should().Contain("Available,");
        content.Should().Contain("Pending,");
        content.Should().Contain("Sold,");
        content.Should().Contain("[System.Text.Json.Serialization.JsonStringEnumMemberName(\"available\")]");
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
        content.Should().Contain("[System.Text.Json.Serialization.JsonStringEnumMemberName(\"extra-large\")]");
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
        content.Should().Contain("public Test.Client.Models.PetStatus? Status");
        content.Should().Contain("public Test.Client.Models.PetSize? Size");
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
        content.Should().NotContain("using DottedNames.Client.Models");
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
        content.Should().Contain("[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]");
        content.Should().Contain("Pending,");
        content.Should().Contain("Confirmed,");
        content.Should().Contain("Shipped,");
    }

    [Fact]
    public void Generate_DottedModel_ReferencesUseFullyQualifiedTypeNameAsync()
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
        content.Should().Contain("public required DottedNames.Client.Models.Commerce.OrderStatus Status");
        content.Should().Contain("public DottedNames.Client.Models.Identity.Customer? Customer");
    }

    [Fact]
    public void Generate_Client_WithDottedNames_UsesFullyQualifiedTypeNamesAsync()
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
        builderContent.Should().NotContain("using DottedNames.Client.Models");
        builderContent.Should().Contain("Task<List<DottedNames.Client.Models.Commerce.Order>>");
        builderContent.Should().Contain("Task<DottedNames.Client.Models.Commerce.Order>");
        builderContent.Should().Contain("DottedNames.Client.Models.Commerce.NewOrder request");
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

    [Fact]
    public void Generate_ModelWithInlineObjectProperty_GeneratesNestedClass()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Order": {
                    "type": "object",
                    "properties": {
                      "id": { "type": "integer", "format": "int64" },
                      "address": {
                        "type": "object",
                        "properties": {
                          "street": { "type": "string" },
                          "city": { "type": "string" }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "Order.cs"));

        content.Should().Contain("public OrderAddress? Address");
        content.Should().Contain("public class OrderAddress");
        content.Should().Contain("[System.Text.Json.Serialization.JsonPropertyName(\"street\")]");
        content.Should().Contain("public string? Street");
        content.Should().Contain("[System.Text.Json.Serialization.JsonPropertyName(\"city\")]");
        content.Should().Contain("public string? City");
    }

    [Fact]
    public void Generate_ModelWithInlineEnumProperty_GeneratesNestedEnum()
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
                      "name": { "type": "string" },
                      "status": {
                        "type": "string",
                        "enum": ["available", "pending", "sold"]
                      }
                    }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "Pet.cs"));

        content.Should().Contain("public PetStatus? Status");
        content.Should().Contain("[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]");
        content.Should().Contain("public enum PetStatus");
        content.Should().Contain("Available,");
        content.Should().Contain("Pending,");
        content.Should().Contain("Sold,");
    }

    [Fact]
    public void Generate_OrderWithInlineEnumType_GeneratesEnumNamedOrderType()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0.0" },
              "paths": {},
              "components": {
                "schemas": {
                  "Order": {
                    "type": "object",
                    "properties": {
                      "Type": {
                        "type": "string",
                        "enum": ["Buy", "Sell"]
                      }
                    }
                  }
                }
              }
            }
            """;
        var generator = CreateGenerator(spec);

        generator.Generate();

        var content = File.ReadAllText(Path.Combine(_outputDirectory, "Models", "Order.cs"));

        content.Should().Contain("public OrderType? Type");
        content.Should().Contain("public enum OrderType");
        content.Should().Contain("Buy,");
        content.Should().Contain("Sell,");
    }
}
