# OpenAPI .NET Client Generator

A modern OpenAPI/Swagger client code generator for .NET that produces high-quality, strongly-typed HTTP clients with full NodaTime support for date and time handling.

## Features

- 🚀 **Modern .NET**: Built for .NET 10 with C# 14.0
- 🕐 **NodaTime Integration**: Automatic mapping of date/time formats to NodaTime types (`Instant`, `LocalDate`, `LocalTime`, `LocalDateTime`, `Duration`)
- ⚡ **System.Text.Json**: Native JSON serialization with optimal performance
- 🛡️ **Type-Safe**: Generates strongly-typed models and client methods
- 🧱 **Fluent Builder API**: Navigate resources naturally — `client.Pets[123].Photos[photoId].Get()`
- 🧪 **Mockable by Design**: All builders use `virtual` methods, navigation properties, and `protected` constructors for seamless Moq integration
- ♻️ **Async First**: All HTTP operations are async with proper cancellation support
- 📖 **Well Documented**: Preserves OpenAPI descriptions as XML documentation comments
- 📋 **Format Registry**: Comprehensive [OpenAPI Format Registry](https://spec.openapis.org/registry/format/index.html) support — integers, URIs, binary, decimals, and more
- ❔ **Nullable Aware**: Respects required/optional properties — required fields use the C# `required` modifier, optional fields use nullable reference types
- 🏷️ **Enum Support**: Generates C# enums from OpenAPI string enums with `JsonStringEnumConverter`
- 📦 **Inline Object Schemas**: Inline `type: object` schemas in responses and request bodies are generated as public nested classes inside builder classes
- 💻 **Modern CLI**: Uses `System.CommandLine` with built-in help, validation, and shell tab-completion
- 💾 **Configuration Persistence**: Saves generation parameters to a JSON config file for easy re-generation via `update` command
- 🔧 **Configurable Type Mappings**: Override default OpenAPI-to-.NET type mappings via the configuration file
- 🔄 **Spec Conversion**: Convert OpenAPI specifications between versions (2.0, 3.0, 3.1, 3.2) and formats (JSON, YAML)
- 🧩 **OpenAPI Overlays**: Apply [OpenAPI Overlay](https://spec.openapis.org/overlay/latest.html) documents to patch specifications before generation — powered by [BinkyLabs.OpenApi.Overlays](https://www.nuget.org/packages/BinkyLabs.OpenApi.Overlays)

## Type Mapping

The generator maps OpenAPI types and formats to idiomatic C# types following the [OpenAPI Format Registry](https://spec.openapis.org/registry/format/index.html). All mappings can be [overridden via the configuration file](#custom-type-mappings).

### String Formats

| OpenAPI Format | C# Type | Notes |
|---|---|---|
| `date-time` | `NodaTime.Instant` | NodaTime — RFC 3339 date-time |
| `date` | `NodaTime.LocalDate` | NodaTime — RFC 3339 full-date |
| `time` | `NodaTime.LocalTime` | NodaTime — RFC 3339 full-time |
| `time-local` | `NodaTime.LocalTime` | NodaTime — time without timezone |
| `date-time-local` | `NodaTime.LocalDateTime` | NodaTime — date-time without timezone |
| `duration` | `NodaTime.Duration` | NodaTime — RFC 3339 duration |
| `uuid` | `Guid` | RFC 4122 UUID |
| `uri` | `Uri` | RFC 3986 URI |
| `uri-reference` | `Uri` | RFC 3986 URI reference |
| `iri` | `Uri` | RFC 3987 Internationalized URI |
| `iri-reference` | `Uri` | RFC 3987 IRI reference |
| `byte` | `byte[]` | Base64-encoded binary (RFC 4648 §4) |
| `binary` | `byte[]` | Raw binary octets |
| `base64url` | `byte[]` | URL-safe base64 (RFC 4648 §5) |
| `char` | `char` | Single character |
| *(other / none)* | `string` | Default for unrecognised string formats |

### Integer Formats

| OpenAPI Format | C# Type |
|---|---|
| `int8` | `sbyte` |
| `int16` | `short` |
| `int32` | `int` |
| `int64` | `long` |
| `uint8` | `byte` |
| `uint16` | `ushort` |
| `uint32` | `uint` |
| `uint64` | `ulong` |
| *(none)* | `int` |

### Number Formats

| OpenAPI Format | C# Type |
|---|---|
| `float` | `float` |
| `double` | `double` |
| `decimal` | `decimal` |
| `decimal128` | `decimal` |
| `double-int` | `long` |
| *(none)* | `double` |

### Enum Types

| OpenAPI Schema | C# Type | Notes |
|---|---|---|
| `type: string` + `enum: [...]` | `enum` | Generated with `[JsonStringEnumConverter]` |
| `$ref` to enum schema | Enum type name | Strongly-typed enum reference |

Enum values are converted to PascalCase members (e.g., `extra-large` → `ExtraLarge`) with `[JsonStringEnumMemberName]` attributes preserving the original value.

### Other Types

| OpenAPI Type | C# Type |
|---|---|
| `boolean` | `bool` |
| `array` | `List<T>` |
| `object` (inline) | Nested class in builder (e.g., `GetResponse`) |
| `$ref` | Referenced class / enum |

## Installation

### As a .NET Tool (recommended)

```bash
# Install globally
dotnet tool install -g OpenApiDotNet

# Or install as a local tool
dotnet new tool-manifest   # if you don't have one yet
dotnet tool install OpenApiDotNet
```

### Build from Source

```bash
git clone https://github.com/Ilchert/OpenApiDotNet.git
cd OpenApiDotNet
dotnet build
```

### Prerequisites

- .NET 10.0 SDK or later

## Usage

### Command Line

```bash
openapi-dotnet-generator <openapi-file> [options]
```

### Arguments & Options

| Argument / Option | Description | Default |
|---|---|---|
| `<openapi-file>` | Path to the OpenAPI specification file (JSON or YAML) | *required* |
| `-o`, `--output <dir>` | Directory where generated code will be placed | `./Generated` |
| `-n`, `--namespace <ns>` | Namespace for generated code | `GeneratedClient` |
| `-p`, `--namespace-prefix <prefix>` | Strip this dotted prefix from schema names when generating namespaces | *none* |
| `-c`, `--client-name <name>` | Custom name for the generated client class | Derived from API title |
| `--overlay <file>` | Path to overlay file(s) to apply before generation (repeatable) | *none* |

Built-in flags provided by `System.CommandLine`:

| Flag | Description |
|---|---|
| `--help`, `-h`, `-?` | Show help and usage information |
| `--version` | Show version information |

### Update Command

After the initial generation, a `.openapidotnet.json` configuration file is saved in the output directory. Use the `update` command to re-generate the client using the saved parameters:

```bash
# Re-generate from config in the current directory
openapi-dotnet-generator update

# Re-generate from a specific config file
openapi-dotnet-generator update ./Generated/.openapidotnet.json
```

| Argument | Description | Default |
|---|---|---|
| `[config-file]` | Path to the `.openapidotnet.json` configuration file | `.openapidotnet.json` |

### Convert Command

Convert an OpenAPI specification to a different version and/or format:

```bash
# Convert to OpenAPI 3.1 JSON (default)
openapi-dotnet-generator convert petstore.yaml output.json

# Convert to OpenAPI 2.0 (Swagger) JSON
openapi-dotnet-generator convert petstore.yaml swagger.json -v 2.0

# Convert to OpenAPI 3.0 YAML
openapi-dotnet-generator convert api.json api-v3.yaml -v 3.0 -f yaml

# Convert to OpenAPI 3.2 YAML
openapi-dotnet-generator convert api.yaml api-v32.yaml -v 3.2 -f yaml
```

| Argument / Option | Description | Default |
|---|---|---|
| `<openapi-file>` | Path to the OpenAPI specification file to convert | *required* |
| `<output-file>` | Path for the converted output file | *required* |
| `-v`, `--version` | Target OpenAPI version (`2.0`, `3.0`, `3.1`, `3.2`) | `3.1` |
| `-f`, `--format` | Output format (`json`, `yaml`) | `json` |

### Shell Tab-Completion

The CLI supports shell tab-completion via the [`dotnet-suggest`](https://github.com/dotnet/command-line-api/blob/main/docs/dotnet-suggest.md) global tool.
Once configured, pressing <kbd>Tab</kbd> will auto-complete the `<openapi-file>` argument with `.json`, `.yaml`, and `.yml` files from the current directory.

```bash
# Install the suggest tool (one-time)
dotnet tool install -g dotnet-suggest

# Follow the shell-specific setup instructions from dotnet-suggest
```

### Examples

**Basic Usage:**
```bash
openapi-dotnet-generator petstore.yaml
```

**With Custom Output Directory:**
```bash
openapi-dotnet-generator api.yaml -o ./src/Client
```

**With Custom Namespace:**
```bash
openapi-dotnet-generator swagger.json -o ./Generated -n MyCompany.ApiClient
```

**Show Help:**
```bash
openapi-dotnet-generator --help
```

**With Overlays:**
```bash
# Apply a single overlay before generation
openapi-dotnet-generator petstore.yaml --overlay remove-deprecated.yaml

# Apply multiple overlays (applied in order)
openapi-dotnet-generator petstore.yaml --overlay base-overlay.yaml --overlay team-overlay.yaml
```

**With Namespace Prefix Stripping:**
```bash
# Strip the 'Commerce' prefix from dotted schema names
# Commerce.Order → Order (in root Models namespace)
# Identity.Customer → Customer (in Identity sub-namespace, unchanged)
openapi-dotnet-generator api.yaml -n MyCompany.Client -p Commerce
```

**With Custom Client Name:**
```bash
# Override the default client class name derived from the API title
openapi-dotnet-generator petstore.yaml -c PetStoreClient
```

**Re-generate from Saved Configuration:**
```bash
# After initial generation, update from the saved config (overlay paths are preserved)
openapi-dotnet-generator update ./Generated/.openapidotnet.json
```

**Convert to a Different Version/Format:**
```bash
openapi-dotnet-generator convert petstore.yaml petstore-v2.json --version 2.0
openapi-dotnet-generator convert api.json api.yaml -f yaml
```

## Generated Code Structure

The generator creates the following structure:

```
Generated/
├── Models/
│   ├── Pet.cs
│   ├── NewPet.cs
│   └── PetStatus.cs
├── Builders/
│   ├── PetsBuilder.cs
│   ├── PetsIdBuilder.cs
│   ├── PhotosBuilder.cs
│   └── PhotosIdBuilder.cs
├── IOpenApiBuilder.cs
├── IOpenApiClient.cs
├── IPetStoreClient.cs
└── .openapidotnet.json
```

Each API path segment gets its own builder class. Static segments (e.g., `/pets`) produce a `PetsBuilder`, while parameterized segments (e.g., `/{petId}`) produce a `PetsIdBuilder`. When the same segment name appears at different tree positions (e.g., `/pets` and `/owners/{ownerId}/pets`), the generator resolves collisions by prefixing with ancestor context (e.g., `OwnersIdPetsBuilder`).

The `.openapidotnet.json` file stores the generation parameters so the client can be re-generated with the `update` command:

```json
{
  "openApiFile": "../petstore.yaml",
  "outputDirectory": ".",
  "namespace": "GeneratedClient",
  "overlayFiles": [
    "../remove-deprecated.yaml"
  ],
  "namespacePrefix": "Commerce",
  "clientName": "PetStoreClient",
  "typeMappings": {
    "string:date-time": "DateTimeOffset",
    "integer": "long"
  }
}
```

### Custom Type Mappings

You can override default OpenAPI-to-.NET type mappings by adding a `typeMappings` section to the `.openapidotnet.json` configuration file. Mappings use keys in the format `"type:format"` (e.g. `"string:date-time"`) or just `"type"` for the default mapping of a type (e.g. `"integer"`).

Only specified keys are overridden; all other defaults remain intact.

```json
{
  "openApiFile": "../api.yaml",
  "outputDirectory": ".",
  "namespace": "MyApp",
  "typeMappings": {
    "string:date-time": "DateTimeOffset",
    "string:date": "DateTime",
    "string:email": "EmailAddress",
    "integer": "long"
  }
}
```

In the example above:
- `string` with format `date-time` maps to `DateTimeOffset` instead of the default `NodaTime.Instant`
- `string` with format `date` maps to `DateTime` instead of the default `NodaTime.LocalDate`
- `string` with format `email` is a new custom mapping (no built-in default)
- `integer` without a format maps to `long` instead of the default `int`

### Example Generated Model

```csharp
using System.Text.Json.Serialization;

namespace PetStoreClient.Models;

/// <summary>
/// A pet in the store
/// </summary>
public class Pet
{
    /// <summary>
    /// Unique identifier for the pet
    /// </summary>
    [JsonPropertyName("id")]
    public required long Id { get; set; }

    /// <summary>
    /// Name of the pet
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Birth date of the pet
    /// </summary>
    [JsonPropertyName("birthDate")]
    public NodaTime.LocalDate? BirthDate { get; set; }

    /// <summary>
    /// When the pet was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public NodaTime.Instant? CreatedAt { get; set; }
}
```

### Example Generated Enum

```csharp
using System.Text.Json.Serialization;

namespace PetStoreClient.Models;

/// <summary>
/// The status of a pet in the store
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PetStatus
{
    [JsonStringEnumMemberName("available")]
    Available,

    [JsonStringEnumMemberName("pending")]
    Pending,

    [JsonStringEnumMemberName ("sold")]
    Sold,
}
```

### Example Generated IOpenApiClient Interface

`IOpenApiClient` is a base interface containing the HTTP infrastructure. A separate named interface (derived from the `--client-name` option or the API title) inherits from it and exposes the top-level navigation properties:

```csharp
using System.Text.Json;

namespace PetStoreClient;

/// <summary>
/// Base interface for all OpenAPI clients
/// </summary>
public interface IOpenApiClient : IOpenApiBuilder
{
    HttpClient HttpClient { get; }
    JsonSerializerOptions JsonOptions { get; }

    IOpenApiClient IOpenApiBuilder.Client => this;
    string IOpenApiBuilder.GetPath() => "";
}
```

### Example Generated Named Client Interface

```csharp
namespace PetStoreClient;

/// <summary>
/// A simple pet store API
/// </summary>
public interface IPetStoreClient : IOpenApiClient
{
    PetsBuilder Pets { get => new(this); }
}
```

### Example Generated Builder

```csharp
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace PetStoreClient;

public class PetsBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

#pragma warning disable CS8618
    protected PetsBuilder() { }
#pragma warning restore CS8618

    public PetsBuilder(IOpenApiBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public IOpenApiClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/pets";

    public virtual PetsIdBuilder this[long petId]
    {
        get => new(this, petId);
    }

    /// <summary>
    /// List all pets
    /// </summary>
    public virtual async Task<List<PetStoreClient.Models.Pet>> Get(
        int? limit = default, CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var queryString = new List<string>();
        if (limit != null)
            queryString.Add($"limit={Uri.EscapeDataString(limit.ToString())}");
        if (queryString.Count > 0)
            url += "?" + string.Join("&", queryString);

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<PetStoreClient.Models.Pet>>(Client.JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Response was null");
    }
}

public class PetsIdBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;
    private readonly long _petId;

#pragma warning disable CS8618
    protected PetsIdBuilder() { }
#pragma warning restore CS8618

    public PetsIdBuilder(IOpenApiBuilder parentBuilder, long petId)
    {
        _parentBuilder = parentBuilder;
        _petId = petId;
    }

    public IOpenApiClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/{_petId}";

    // Navigation properties are virtual so they can be overridden in mocks
    public virtual PhotosBuilder Photos => new(this);

    /// <summary>
    /// Get a pet by ID
    /// </summary>
    public virtual async Task<PetStoreClient.Models.Pet> Get(
        CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PetStoreClient.Models.Pet>(Client.JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Response was null");
    }
}
```

### Example Generated Inline Response

When a response schema is defined inline (not via `$ref`), the generator creates a public nested class inside the builder:

```yaml
# OpenAPI spec
/stats:
  get:
    responses:
      200:
        content:
          application/json:
            schema:
              type: object
              properties:
                totalCount:
                  type: integer
                  format: int32
                activeCount:
                  type: integer
                  format: int32
                lastUpdated:
                  type: string
                  format: date-time
```

```csharp
// Generated StatsBuilder.cs
public class StatsBuilder : IOpenApiBuilder
{
    // ... builder infrastructure ...

    /// <summary>
    /// Get statistics
    /// </summary>
    public virtual async Task<GetResponse> Get(
        CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GetResponse>(
            Client.JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Response was null");
    }

    public class GetResponse
    {
        [JsonPropertyName("totalCount")]
        public int? TotalCount { get; set; }

        [JsonPropertyName("activeCount")]
        public int? ActiveCount { get; set; }

        [JsonPropertyName("lastUpdated")]
        public NodaTime.Instant? LastUpdated { get; set; }
    }
}
```

The same applies to inline request body schemas, which generate `{HttpMethod}Request` nested classes (e.g., `PostRequest`).

## Fluent Builder Pattern

The generator produces a **fluent builder API** where each URL segment maps to its own builder class. Path parameters are captured by the builder chain via indexers, and operations are invoked on the terminal builder:

```csharp
// GET /pets?limit=10
var pets = await client.Pets.Get(limit: 10);

// GET /pets/123
var pet = await client.Pets[123].Get();

// GET /pets/123/photos/{photoId}
var photo = await client.Pets[123].Photos[photoId].Get();

// DELETE /pets/123
await client.Pets[123].Delete();
```

Path building is handled automatically by chaining `GetPath()` through the builder hierarchy — no manual URL construction needed.

### Mocking Support

All builder classes are designed for easy mocking with frameworks like [Moq](https://github.com/devlooped/moq):

- **`virtual` methods** on all operations, indexers, and navigation properties
- **`protected` parameterless constructors** so Moq can create subclass proxies
- **Named client interface** (e.g., `IPetStoreClient`) as the entry point for mock setup

```csharp
var mock = new Mock<IPetStoreClient>();
mock.Setup(c => c.Pets[123].Get(It.IsAny<CancellationToken>()))
    .ReturnsAsync(new List<Pet> { new Pet() });

var result = await mock.Object.Pets[123].Get(default);
```

## URL Encoding & Query Parameters

Query string values are automatically URL-encoded for safe transmission:

```csharp
// Query parameter encoding
var pets = await client.Pets.Get(limit: 10, status: "available & active");
// Generates: /pets?limit=10&status=available%20%26%20active
```

## Using Generated Code

### 1. Add Required NuGet Packages

Add these packages to your project:

```xml
<PackageReference Include="NodaTime" Version="3.3.0" />
<PackageReference Include="NodaTime.Serialization.SystemTextJson" Version="1.3.0" />
```

### 2. Implement the Named Client Interface

Create a concrete class that implements the generated named client interface (e.g., `IPetStoreClient`):

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

public class PetStoreClient : IPetStoreClient
{
    public HttpClient HttpClient { get; }
    public JsonSerializerOptions JsonOptions { get; }

    public PetStoreClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}
```

Register it with dependency injection:

```csharp
builder.Services.AddHttpClient<PetStoreClient>(client =>
{
    client.BaseAddress = new Uri("https://api.petstore.example.com");
});
```

### 3. Use the Client

```csharp
// List pets with query parameters
var pets = await client.Pets.Get(limit: 10);

// Create a pet
var newPet = new NewPet
{
    Name = "Fluffy",
    BirthDate = LocalDate.FromDateTime(DateTime.Now.AddYears(-2))
};
var createdPet = await client.Pets.Post(newPet);

// Get specific pet by ID
var pet = await client.Pets[123].Get();

// Navigate nested resources
var photo = await client.Pets[123].Photos[photoId].Get();

// Delete a pet
await client.Pets[123].Delete();

// Check timestamps with NodaTime
if (pet.CreatedAt.HasValue)
{
    var zonedTime = pet.CreatedAt.Value.InUtc();
    Console.WriteLine($"Pet created at: {zonedTime}");
}
```

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run with verbosity
dotnet test --logger "console;verbosity=detailed"

# Run specific test
dotnet test --filter "FullyQualifiedName~TypeMappingTests"
```

The project includes comprehensive test coverage:
- **Type Mapping Tests**: Verify OpenAPI to C# type conversions
- **Naming Convention Tests**: Ensure proper PascalCase/camelCase conversions
- **Integration Tests**: End-to-end client generation tests
- **Validation Tests**: Argument validation and error handling

### Test Coverage

- **112 tests** covering all major functionality
- **100% pass rate**
- Unit tests for type mapping and naming conventions
- Integration tests with real OpenAPI specifications
- Builder generation and path tree construction tests

## Architecture

### Core Components

1. **ClientGenerator**: Main orchestrator that generates all code — models, builder classes, and interfaces
2. **PathTreeBuilder**: Parses OpenAPI paths into a tree of `PathSegmentNode` objects, where each node represents a URL segment (static or parameterized), and resolves unique builder class names with collision detection
3. **TypeMappingConfig**: Configurable OpenAPI-to-.NET type mappings with defaults and user overrides
4. **Model Generator**: Creates C# classes and enums from OpenAPI component schemas
5. **Builder Generator**: For each path tree node, emits a builder class (`IBuilder` implementation) with navigation properties, indexers, and HTTP operation methods

### Builder Generation Pipeline

```
OpenAPI Paths → PathTreeBuilder.Build() → Path Tree → Builder Classes
                                                    → IOpenApiBuilder.cs
                                                    → IOpenApiClient.cs
                                                    → I{ClientName}.cs

OpenAPI Schemas → Model Generator → Models/*.cs
```

The path tree maps URL structure to builder hierarchy:

```
/pets                          → PetsBuilder (operations: Get, Post)
/pets/{petId}                  → PetsIdBuilder (operations: Get, Delete)
/pets/{petId}/photos/{photoId} → PhotosBuilder + PhotosIdBuilder (operation: Get)
/owners/{ownerId}/pets/{petId} → OwnersBuilder + OwnersIdBuilder
                                 + OwnersIdPetsBuilder + OwnersIdPetsIdBuilder
```

When the same segment name appears at multiple tree positions, context-prefixed names are assigned automatically to avoid collisions.

### Type Mapping Logic

The type mapping logic is driven by `TypeMappingConfig`, which holds a dictionary of mappings keyed by `"type:format"` (e.g. `"string:date-time"` → `"Instant"`) or just `"type"` for defaults (e.g. `"string"` → `"string"`). Custom mappings from the configuration file are merged on top of the built-in defaults.

The generator maps OpenAPI types and [format registry](https://spec.openapis.org/registry/format/index.html) values to C# types:

```
String formats
  "string"                             → string
  "string" (format: "date-time")       → NodaTime.Instant
  "string" (format: "date")            → NodaTime.LocalDate
  "string" (format: "time")            → NodaTime.LocalTime
  "string" (format: "time-local")      → NodaTime.LocalTime
  "string" (format: "date-time-local") → NodaTime.LocalDateTime
  "string" (format: "duration")        → NodaTime.Duration
  "string" (format: "uuid")            → Guid
  "string" (format: "uri/iri")         → Uri
  "string" (format: "byte/binary")     → byte[]
  "string" (format: "char")            → char

Integer formats
  "integer"                            → int
  "integer" (format: "int8")           → sbyte
  "integer" (format: "int16")          → short
  "integer" (format: "int32")          → int
  "integer" (format: "int64")          → long
  "integer" (format: "uint8")          → byte
  "integer" (format: "uint16")         → ushort
  "integer" (format: "uint32")         → uint
  "integer" (format: "uint64")         → ulong

Number formats
  "number"                             → double
  "number" (format: "float")           → float
  "number" (format: "double")          → double
  "number" (format: "decimal")         → decimal
  "number" (format: "decimal128")      → decimal
  "number" (format: "double-int")      → long

Enum types
  "string" + enum: [...]               → C# enum    (with JsonStringEnumConverter)
  $ref to enum schema                  → Enum type

Other types
  "boolean"                            → bool
  "array"                              → List<T>
  Inline "object" (in response/body)   → Nested class in builder
  Reference ($ref)                     → Custom Type / Enum
```

## Supported OpenAPI Features

- ✅ OpenAPI 3.0 specifications
- ✅ JSON and YAML input formats
- ✅ Fluent builder-style API with path segment navigation
- ✅ Path parameters captured via builder indexers
- ✅ Query parameters with URL encoding
- ✅ Multiple path parameters (e.g., `/owners/{ownerId}/pets/{petId}`)
- ✅ Automatic builder name collision resolution for shared segment names
- ✅ Named client interface (e.g., `IPetStoreClient`) derived from `IOpenApiClient`
- ✅ Mock-friendly design (`virtual` methods, navigation properties, and `protected` constructors, and named client interface)
- ✅ Request bodies
- ✅ Response models
- ✅ Schema references (`$ref`)
- ✅ Required/optional properties
- ✅ Arrays and nested objects
- ✅ Inline object schemas as nested classes in builders
- ✅ Enum types with `JsonStringEnumConverter`
- ✅ HTTP methods: GET, POST, PUT, PATCH, DELETE
- ✅ HTTP method-based operation naming (Get, Post, Put, Patch, Delete)
- ✅ Descriptions and summaries
- ✅ [OpenAPI Format Registry](https://spec.openapis.org/registry/format/index.html) type mappings
- ✅ Configurable type mappings via `.openapidotnet.json`
- ✅ Specification conversion between OpenAPI versions and formats
- ✅ [OpenAPI Overlay Specification](https://spec.openapis.org/overlay/latest.html) support (single or multiple overlays)
- ✅ Namespace prefix stripping for dotted schema names

## Naming Conventions

The generator follows standard .NET naming conventions:

- **Model Classes**: PascalCase (e.g., `Pet`, `NewPet`)
- **Builder Classes**: `{Segment}Builder` / `{Segment}IdBuilder` (e.g., `PetsBuilder`, `PetsIdBuilder`)
- **Collision Resolution**: Context-prefixed names when the same segment appears at multiple tree positions (e.g., `OwnersIdPetsBuilder` for `/owners/{ownerId}/pets`)
- **Properties**: PascalCase (e.g., `BirthDate`, `CreatedAt`)
- **Method Parameters**: camelCase (e.g., `petId`, `limit`)
- **JSON Properties**: Preserved from OpenAPI spec (typically camelCase)

The generator automatically converts:
- `birth_date` → `BirthDate` (property) / `birthDate` (parameter)
- `created-at` → `CreatedAt` (property) / `createdAt` (parameter)
- `user-name` → `UserName` (property) / `userName` (parameter)

## Dependencies

### Runtime Dependencies
- BinkyLabs.OpenApi.Overlays (2.4.0)
- Microsoft.OpenApi (3.3.1)
- Microsoft.OpenApi.YamlReader (3.3.1)
- NodaTime (3.3.0)
- NodaTime.Serialization.SystemTextJson (1.3.1)
- System.CommandLine (2.0.3)

### Generated Code Dependencies
Projects using the generated code need:
- NodaTime (3.3.0)
- NodaTime.Serialization.SystemTextJson (1.3.0)

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

### Development Setup

1. Clone the repository
2. Ensure .NET 10 SDK is installed
3. Run `dotnet restore`
4. Run `dotnet build`
5. Run `dotnet test` to verify everything works

## License

This project is open source. Please check the license file for details.

## Roadmap

Future enhancements being considered:

- [x] Enum types with `JsonStringEnumConverter` support
- [x] Configurable type mappings via configuration file
- [x] Fluent builder-style API with path segment navigation
- [x] Mock-friendly design for unit testing
- [ ] Support for authentication schemes (Bearer, API Key, OAuth2)
- [ ] Polymorphic types with discriminators
- [ ] `allOf`, `oneOf`, `anyOf` schema composition
- [ ] Webhook support
- [ ] Multipart form data handling
- [ ] Custom templates for code generation
- [ ] MSBuild task for build-time generation
- [ ] Source generator for zero-overhead generation

## Acknowledgments

Built with:
- [Microsoft.OpenApi](https://github.com/microsoft/OpenAPI.NET) - OpenAPI parsing
- [NodaTime](https://nodatime.org/) - Modern date/time library
- [xUnit](https://xunit.net/) - Testing framework
- [FluentAssertions](https://fluentassertions.com/) - Assertion library

---

**Made with ❤️ for the .NET community**
