using System.Text;
using Microsoft.OpenApi;
using OpenApiDotNet.Tests.IO;

namespace OpenApiDotNet.Tests;

public class OpenApiGeneratorTests
{
    private readonly InMemoryWritableFileProvider _output = new();

    private OpenApiGenerator CreateGenerator(
        string specJson,
        string namespaceName = "Test.Client",
        string? namespacePrefix = null)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(specJson));
        var (document, diagnostic) = OpenApiDocument.Load(stream);
        Assert.Empty(diagnostic?.Errors ?? []);
        return new OpenApiGenerator(document, namespaceName, _output, namespacePrefix: namespacePrefix);
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

        Assert.True(_output.Files.ContainsKey("Models/Pet.cs"));
        Assert.True(_output.Files.ContainsKey("Models/NewPet.cs"));
        Assert.True(_output.Files.ContainsKey("IOpenApiBuilder.cs"));
        Assert.True(_output.Files.ContainsKey("IOpenApiClient.cs"));
        Assert.True(_output.Files.ContainsKey("IPetStoreAPIClient.cs"));
        Assert.True(_output.Files.ContainsKey("Builders/PetsBuilder.cs"));
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

        var content = _output.Files["Models/Pet.cs"];

        Assert.Contains("public required long Id", content);
        Assert.Contains("public required string Name", content);
        Assert.Contains("public string? Tag", content);
        Assert.Contains("public System.DateOnly? BirthDate", content);
        Assert.Contains("public System.DateTimeOffset? CreatedAt", content);
        Assert.Contains("public bool? Vaccinated", content);
        Assert.Contains("public double? Weight", content);
        Assert.DoesNotContain("using NodaTime;", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonPropertyName(\"id\")]", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonPropertyName(\"birthDate\")]", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonPropertyName(\"createdAt\")]", content);
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
        var clientContent = _output.Files["IOpenApiClient.cs"];
        Assert.Contains("HttpClient HttpClient", clientContent);
        Assert.Contains("JsonSerializerOptions JsonOptions", clientContent);
        Assert.DoesNotContain("PetsBuilder Pets", clientContent);

        // Named client interface should have navigation property for Pets
        var namedClientContent = _output.Files["IPetStoreAPIClient.cs"];
        Assert.Contains("PetsBuilder Pets", namedClientContent);
        Assert.Contains(": IOpenApiClient", namedClientContent);

        // PetsBuilder should have Get and Post operations
        var petsContent = _output.Files["Builders/PetsBuilder.cs"];
        Assert.Contains("public virtual async System.Threading.Tasks.Task<System.Collections.Generic.List<PetStore.Client.Models.Pet>> Get", petsContent);
        Assert.Contains("public virtual async System.Threading.Tasks.Task<PetStore.Client.Models.Pet> Post", petsContent);
        Assert.Contains("int? limit", petsContent);
        Assert.Contains("PetStore.Client.Models.NewPet request", petsContent);
        Assert.Contains("System.Threading.CancellationToken cancellationToken = default", petsContent);
        Assert.Contains("Client.HttpClient.GetAsync", petsContent);
        Assert.Contains("HttpClientJsonExtensions.PostAsJsonAsync", petsContent);
        Assert.Contains("Pets.IdBuilder this[long petId]", petsContent);

        // IdBuilder (under Pets namespace) should have Get and Delete operations
        var petsIdContent = _output.Files["Builders/Pets/IdBuilder.cs"];
        Assert.Contains("public virtual async System.Threading.Tasks.Task<PetStore.Client.Models.Pet> Get", petsIdContent);
        Assert.Contains("public virtual async System.Threading.Tasks.Task Delete", petsIdContent);
        Assert.Contains("Client.HttpClient.DeleteAsync", petsIdContent);
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

        var content = _output.Files["Builders/ItemsBuilder.cs"];
        Assert.Contains("var queryString = new System.Collections.Generic.List<string>();", content);
        Assert.Contains("if (limit is {} limitValue)", content);
        Assert.Contains("System.Uri.EscapeDataString", content);
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

        var content = _output.Files["Builders/ItemsBuilder.cs"];

        // Required parameter should be non-nullable
        Assert.Contains("string category", content);
        Assert.DoesNotContain("string? category", content);
        Assert.DoesNotContain("string category = default", content);

        // Optional parameter should be nullable
        Assert.Contains("int? limit", content);

        // Required parameter should always be added to query string (no null check)
        Assert.Contains("System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(category, Client.JsonOptions)", content);
        Assert.DoesNotContain("if (category != null)", content);

        // Optional parameter should have null check using pattern matching (avoids CS8604)
        Assert.Contains("if (limit is {} limitValue)", content);
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

        var content = _output.Files["Builders/ItemsBuilder.cs"];

        // Optional list parameter should be nullable
        Assert.Contains("System.Collections.Generic.List<string>? tags", content);

        // Required list parameter should be non-nullable
        Assert.Contains("System.Collections.Generic.List<string> statuses", content);
        Assert.DoesNotContain("System.Collections.Generic.List<string>? statuses", content);

        // Optional list parameter should have null check before foreach
        Assert.Contains("if (tags != null)", content);
        Assert.Contains("foreach (var item in tags)", content);

        // Required list parameter should iterate without null check
        Assert.Contains("foreach (var item in statuses)", content);

        // Each item should be individually escaped and added with the parameter name
        Assert.Contains("System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(item, Client.JsonOptions)", content);

        // Scalar parameter should use pattern matching to avoid CS8604
        Assert.Contains("if (limit is {} limitValue)", content);
        Assert.Contains("System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(limitValue, Client.JsonOptions)", content);
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

        var content = _output.Files["Builders/ItemsBuilder.cs"];
        var signatureStart = content.IndexOf("Post(");
        var signatureEnd = content.IndexOf(')', signatureStart);
        var signature = content[signatureStart..signatureEnd];

        var categoryPos = signature.IndexOf("string category");
        var requestPos = signature.IndexOf("Test.Client.Models.SearchRequest request");
        var limitPos = signature.IndexOf("int? limit");
        var offsetPos = signature.IndexOf("int? offset");
        var ctPos = signature.IndexOf("System.Threading.CancellationToken cancellationToken");

        Assert.True(categoryPos > 0);
        Assert.True(requestPos > 0);
        Assert.True(limitPos > 0);
        Assert.True(offsetPos > 0);
        Assert.True(ctPos > 0);

        // Required params before optional params
        Assert.True(categoryPos < limitPos);
        Assert.True(categoryPos < offsetPos);
        Assert.True(requestPos < limitPos);
        Assert.True(requestPos < offsetPos);
        Assert.True(requestPos < ctPos);
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

        var content = _output.Files["Builders/StatsBuilder.cs"];

        // Return type should reference the nested class
        Assert.Contains("System.Threading.Tasks.Task<GetResponse>", content);
        Assert.Contains("Get", content);

        // Nested class should be generated with properties
        Assert.Contains("public partial class GetResponse", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonPropertyName(\"totalCount\")]", content);
        Assert.Contains("public int? TotalCount", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonPropertyName(\"activeCount\")]", content);
        Assert.Contains("public int? ActiveCount", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonPropertyName(\"lastUpdated\")]", content);
        Assert.Contains("public System.DateTimeOffset? LastUpdated", content);
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

        var content = _output.Files["Builders/FeedbackBuilder.cs"];

        // Request body should use the nested class
        Assert.Contains("PostRequest request", content);

        // Nested class should be generated with properties
        Assert.Contains("public partial class PostRequest", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonPropertyName(\"message\")]", content);
        Assert.Contains("public required string Message", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonPropertyName(\"rating\")]", content);
        Assert.Contains("public int? Rating", content);
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

        var content = _output.Files["Builders/NumbersBuilder.cs"];

        Assert.Contains("System.Collections.Generic.List<int> request", content);
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

        var content = _output.Files["Builders/PetsBuilder.cs"];

        Assert.Contains("newPet", content);
        Assert.DoesNotContain("Pet request", content);
        Assert.Contains("PostAsJsonAsync(Client.HttpClient, url, newPet,", content);
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

        var content = _output.Files["Builders/Items/IdBuilder.cs"];
        Assert.Contains("System.Threading.Tasks.Task<Test.Client.Models.Item>", content);

        // Should read from JSON and return the result
        Assert.Contains("ReadFromJsonAsync<Test.Client.Models.Item>", content);
        Assert.Contains("Client.HttpClient.DeleteAsync", content);
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

        var content = _output.Files["Builders/DataBuilder.cs"];

        // Return type should be object (not void, not a nested class)
        Assert.Contains("System.Threading.Tasks.Task<object>", content);
        Assert.DoesNotContain("System.Threading.Tasks.Task<GetResponse>", content);
        Assert.DoesNotContain("public partial class GetResponse", content);

        // Should read from JSON as object
        Assert.Contains("ReadFromJsonAsync<object>", content);
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

        var content = _output.Files["Builders/Items/IdBuilder.cs"];

        // Return type should be bool
        Assert.Contains("System.Threading.Tasks.Task<bool>", content);

        // Uses is {} pattern which works uniformly for both value types and reference types
        Assert.Contains("var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<bool>(response.Content, Client.JsonOptions, cancellationToken);", content);
        Assert.Contains("if (deserializedResponse is { } deserializedResponseValue)", content);
        Assert.Contains("    return deserializedResponseValue;", content);
        Assert.Contains("throw new System.InvalidOperationException($\"Response from {url} is null\");", content);
    }

    [Fact]
    public void Constructor_WithNullDocument_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new OpenApiGenerator(null!, "TestNamespace", _output);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(() => act());
        Assert.Equal("document", ex.ParamName);
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
        var act = () => new OpenApiGenerator(document, null!, _output);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(() => act());
        Assert.Equal("namespaceName", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOutput_ThrowsArgumentNullException()
    {
        // Arrange
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" }
        };

        // Act
        var act = () => new OpenApiGenerator(document, "TestNamespace", null!);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(() => act());
        Assert.Equal("output", ex.ParamName);
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

        Assert.True(_output.Files.ContainsKey("Models/PetStatus.cs"));
        Assert.True(_output.Files.ContainsKey("Models/PetSize.cs"));
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

        var content = _output.Files["Models/PetStatus.cs"];
        Assert.Contains("public enum PetStatus", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]", content);
        Assert.Contains("Available,", content);
        Assert.Contains("Pending,", content);
        Assert.Contains("Sold,", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonStringEnumMemberName(\"available\")]", content);
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

        var content = _output.Files["Models/PetSize.cs"];
        Assert.Contains("public enum PetSize", content);
        Assert.Contains("Small,", content);
        Assert.Contains("Medium,", content);
        Assert.Contains("Large,", content);
        Assert.Contains("ExtraLarge,", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonStringEnumMemberName(\"extra-large\")]", content);
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

        var content = _output.Files["Models/Pet.cs"];
        Assert.Contains("public Test.Client.Models.PetStatus? Status", content);
        Assert.Contains("public Test.Client.Models.PetSize? Size", content);
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

        Assert.True(_output.Files.ContainsKey("Models/Commerce/Order.cs"));
        Assert.True(_output.Files.ContainsKey("Models/Commerce/NewOrder.cs"));
        Assert.True(_output.Files.ContainsKey("Models/Commerce/OrderStatus.cs"));
        Assert.True(_output.Files.ContainsKey("Models/Identity/Customer.cs"));
        Assert.True(_output.Files.ContainsKey("Models/SimpleModel.cs"));
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

        var content = _output.Files["Models/Commerce/Order.cs"];
        Assert.Contains("public partial class Order", content);
        Assert.Contains("namespace DottedNames.Client.Models.Commerce;", content);
        Assert.DoesNotContain("using DottedNames.Client.Models", content);
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

        var content = _output.Files["Models/Commerce/OrderStatus.cs"];
        Assert.Contains("public enum OrderStatus", content);
        Assert.Contains("namespace DottedNames.Client.Models.Commerce;", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]", content);
        Assert.Contains("Pending,", content);
        Assert.Contains("Confirmed,", content);
        Assert.Contains("Shipped,", content);
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

        var content = _output.Files["Models/Commerce/Order.cs"];
        Assert.Contains("public required DottedNames.Client.Models.Commerce.OrderStatus Status", content);
        Assert.Contains("public DottedNames.Client.Models.Identity.Customer? Customer", content);
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

        var builderContent = _output.Files["Builders/OrdersBuilder.cs"];
        Assert.DoesNotContain("using DottedNames.Client.Models", builderContent);
        Assert.Contains("System.Threading.Tasks.Task<System.Collections.Generic.List<DottedNames.Client.Models.Commerce.Order>>", builderContent);
        Assert.Contains("System.Threading.Tasks.Task<DottedNames.Client.Models.Commerce.Order>", builderContent);
        Assert.Contains("DottedNames.Client.Models.Commerce.NewOrder request", builderContent);
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

        var content = _output.Files["Models/SimpleModel.cs"];
        Assert.Contains("namespace DottedNames.Client.Models;", content);
        Assert.Contains("public partial class SimpleModel", content);
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

        Assert.True(_output.Files.ContainsKey("Models/Order.cs"));
        Assert.True(_output.Files.ContainsKey("Models/NewOrder.cs"));
        Assert.True(_output.Files.ContainsKey("Models/OrderStatus.cs"));
        Assert.True(_output.Files.ContainsKey("Models/Identity/Customer.cs"));
        Assert.True(_output.Files.ContainsKey("Models/SimpleModel.cs"));

        var orderContent = _output.Files["Models/Order.cs"];
        Assert.Contains("namespace DottedNames.Client.Models;", orderContent);
        Assert.Contains("public partial class Order", orderContent);

        var customerContent = _output.Files["Models/Identity/Customer.cs"];
        Assert.Contains("namespace DottedNames.Client.Models.Identity;", customerContent);
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

        var content = _output.Files["Models/Order.cs"];

        Assert.Contains("public OrderAddress? Address", content);
        Assert.Contains("public partial class OrderAddress", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonPropertyName(\"street\")]", content);
        Assert.Contains("public string? Street", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonPropertyName(\"city\")]", content);
        Assert.Contains("public string? City", content);
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

        var content = _output.Files["Models/Pet.cs"];

        Assert.Contains("public PetStatus? Status", content);
        Assert.Contains("[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]", content);
        Assert.Contains("public enum PetStatus", content);
        Assert.Contains("Available,", content);
        Assert.Contains("Pending,", content);
        Assert.Contains("Sold,", content);
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

        var content = _output.Files["Models/Order.cs"];

        Assert.Contains("public OrderType? Type", content);
        Assert.Contains("public enum OrderType", content);
        Assert.Contains("Buy,", content);
        Assert.Contains("Sell,", content);
    }

    [Fact]
    public void Generate_ReturnsListOfGeneratedFiles()
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

        var generatedFiles = generator.Generate();

        Assert.NotEmpty(generatedFiles);
        Assert.Contains(Path.Combine("Models", "Pet.cs"), generatedFiles);
        Assert.Contains(Path.Combine("Models", "NewPet.cs"), generatedFiles);
        Assert.Contains("IOpenApiBuilder.cs", generatedFiles);
        Assert.Contains("IOpenApiClient.cs", generatedFiles);
        Assert.Contains("IPetStoreAPIClient.cs", generatedFiles);
        Assert.Contains(Path.Combine("Builders", "PetsBuilder.cs"), generatedFiles);
    }

    [Fact]
    public void Generate_SecondRun_WithRemovedSchema_ReturnsUpdatedFileList()
    {
        var specWithTwoSchemas = """
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
        var specWithOneSchema = """
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
                  "Pet": { "type": "object", "properties": { "id": { "type": "integer", "format": "int64" } } }
                }
              }
            }
            """;

        var firstGenerator = CreateGenerator(specWithTwoSchemas, "PetStore.Client");
        var firstFiles = firstGenerator.Generate();

        Assert.Contains(Path.Combine("Models", "NewPet.cs"), firstFiles);
        Assert.True(_output.Files.ContainsKey("Models/NewPet.cs"));

        var secondGenerator = CreateGenerator(specWithOneSchema, "PetStore.Client");
        var secondFiles = secondGenerator.Generate();

        Assert.DoesNotContain(Path.Combine("Models", "NewPet.cs"), secondFiles);
        Assert.Contains(Path.Combine("Models", "Pet.cs"), secondFiles);
    }
}
