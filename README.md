# OpenAPI .NET Client Generator

A modern OpenAPI/Swagger client code generator for .NET that produces high-quality, strongly-typed HTTP clients using built-in .NET types by default, with optional NodaTime support for date and time handling.

## Features

- 🚀 **Modern .NET**: Built for .NET 10 with C# 14.0
- 🕐 **Built-in .NET Types**: Uses `DateTimeOffset`, `DateOnly`, `TimeOnly`, `DateTime`, `TimeSpan` for date/time mappings by default — no extra packages needed
- 🕐 **NodaTime Integration**: Optional `--use-nodatime` flag to use NodaTime types (`Instant`, `LocalDate`, `LocalTime`, `LocalDateTime`, `Duration`)
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
- 🏗️ **Build-Time Source Generation**: Generate client interfaces, builders, and models directly into consuming projects with the Roslyn source generator package

## Type Mapping

The generator maps OpenAPI types and formats to idiomatic C# types following the [OpenAPI Format Registry](https://spec.openapis.org/registry/format/index.html). All mappings can be [overridden via the configuration file](#custom-type-mappings).

### String Formats

| OpenAPI Format | C# Type (default) | C# Type (`--use-nodatime`) | Notes |
|---|---|---|---|
| `date-time` | `DateTimeOffset` | `NodaTime.Instant` | RFC 3339 date-time |
| `date` | `DateOnly` | `NodaTime.LocalDate` | RFC 3339 full-date |
| `time` | `TimeOnly` | `NodaTime.LocalTime` | RFC 3339 full-time |
| `time-local` | `TimeOnly` | `NodaTime.LocalTime` | Time without timezone |
| `date-time-local` | `DateTime` | `NodaTime.LocalDateTime` | Date-time without timezone |
| `duration` | `TimeSpan` | `NodaTime.Duration` | RFC 3339 duration |
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
dotnet tool install -g openapi-dotnet-generator

# Or install as a local tool
dotnet new tool-manifest   # if you don't have one yet
dotnet tool install openapi-dotnet-generator
```

### Build from Source

```bash
git clone https://github.com/Ilchert/OpenApiDotNet.git
cd OpenApiDotNet
dotnet build
```

### Prerequisites

- .NET 10.0 SDK or later

## Source Generator

The repository also includes `OpenApiDotNet.SourceGenerator`, a Roslyn analyzer/source generator that emits the same client interfaces, builder types, and models at build time from OpenAPI `AdditionalFiles`.

The source generator keeps the shared generator code in [`src/OpenApiDotNet`](src/OpenApiDotNet) and links the required files into [`src/OpenApiDotNet.SourceGenerator`](src/OpenApiDotNet.SourceGenerator). The analyzer assembly targets `netstandard2.0` for broad Roslyn host compatibility, and package dependencies needed at analyzer runtime are merged into the analyzer assembly with ILRepack so the NuGet package ships a single analyzer DLL.

### Consuming the source generator

Use a package reference after packing/publishing the analyzer, or a project reference while working in this repository:

```xml
<ItemGroup>
  <PackageReference Include="OpenApiDotNet.SourceGenerator" Version="0.11.0" PrivateAssets="all" />
  <AdditionalFiles Include="OpenApi\petstore.json" />
  <AdditionalFiles Include="OpenApi\remove-pets-post.overlay.json"
                   OpenApiOverlay="true" />
</ItemGroup>
```

```xml
<ItemGroup>
  <ProjectReference Include="..\src\OpenApiDotNet.SourceGenerator\OpenApiDotNet.SourceGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
  <AdditionalFiles Include="OpenApi\petstore.json" />
  <AdditionalFiles Include="OpenApi\remove-pets-post.overlay.json"
                   OpenApiOverlay="true" />
</ItemGroup>
```

To reuse CLI-style configuration, add `.openapidotnet.json` as another `AdditionalFiles` item. The source generator reads `namespace`, `namespacePrefix`, `clientName`, `typeMappings`, `openApiFile`, and `overlayFiles` from that file. Referenced specs and overlays must still be included as `AdditionalFiles` so Roslyn tracks their changes during builds.

```xml
<ItemGroup>
  <AdditionalFiles Include=".openapidotnet.json" />
  <AdditionalFiles Include="OpenApi\petstore.json" />
  <AdditionalFiles Include="OpenApi\remove-pets-post.overlay.json" />
</ItemGroup>
```

See [`samples/OpenApiDotNet.SourceGeneratorDemo/OpenApiDotNet.SourceGeneratorDemo.csproj`](samples/OpenApiDotNet.SourceGeneratorDemo/OpenApiDotNet.SourceGeneratorDemo.csproj) for the project-reference setup used in this repository.

### Supported inputs

- `AdditionalFiles` entries ending in `.json`, `.yaml`, or `.yml`
- An optional `.openapidotnet.json` `AdditionalFiles` entry for generator configuration
- One primary OpenAPI document per consuming project build
- Zero or more overlay `AdditionalFiles` marked with `OpenApiOverlay="true"`
- Source-generator configuration through `.openapidotnet.json`

Overlays are applied in declaration order before code generation. When `.openapidotnet.json` is present, the generator uses its `openApiFile` and `overlayFiles` values to select the primary document and overlays from `AdditionalFiles`; any `OpenApiOverlay="true"` metadata still applies to other overlay files. The source generator currently supports overlay remove actions targeting `$.paths['...']` and `$.paths['...'].<method>`. If more than one non-overlay OpenAPI document is included, the generator reports warning `OADNSG001` and uses the configured file or the first matching file.

### Supported options

The source generator reads its configuration from `.openapidotnet.json`. If a setting is omitted, the generator falls back to its built-in defaults such as `GeneratedClient` for the namespace root, the OpenAPI title-derived client name, and the default .NET type mappings.

Example `.openapidotnet.json`:

```json
{
  "openApiFile": "OpenApi/petstore.json",
  "namespace": "MyApp.Generated",
  "namespacePrefix": "Commerce",
  "overlayFiles": [
    "OpenApi/remove-pets-post.overlay.json"
  ],
  "clientName": "PetStoreAPIClient",
  "typeMappings": {
    "string:date": "NodaTime.LocalDate",
    "string:date-time": "NodaTime.Instant"
  }
}
```

### Current limitations

- Source-generator mode does not create `.openapidotnet.json`; add it manually as an `AdditionalFiles` item if you want config-file-driven generation
- `outputDirectory` and `generatedFiles` from `.openapidotnet.json` are ignored in source-generator mode
- CLI-only features such as `update` and `convert` are not available through the analyzer
- If more than one supported non-overlay OpenAPI file is included, the generator reports warning `OADNSG001` and uses the first matching file
- If more than one `.openapidotnet.json` file is included, the generator reports error `OADNSG003`
- Invalid or unreadable specs report error `OADNSG002`

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
| `--use-nodatime` | Use NodaTime types instead of built-in .NET types for date/time mappings | `false` |

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

The `update` command automatically tracks generated files. When the OpenAPI specification changes (e.g., schemas or endpoints are removed), files that are no longer needed are automatically deleted and empty directories are cleaned up.

### Convert Command

Convert an OpenAPI specification to a different version and/or format:

```bash
# Convert to OpenAPI 3.2 JSON (default)
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
| `-v`, `--version` | Target OpenAPI version (`2.0`, `3.0`, `3.1`, `3.2`) | `3.2` |
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

**With NodaTime Types:**
```bash
# Use NodaTime.Instant, NodaTime.LocalDate, etc. instead of built-in .NET types
openapi-dotnet-generator petstore.yaml --use-nodatime
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
│   └── Pets/
│       ├── IdBuilder.cs
│       └── Id/
│           ├── PhotosBuilder.cs
│           └── Photos/
│               └── IdBuilder.cs
├── IOpenApiBuilder.cs
├── IOpenApiClient.cs
├── IPetStoreClient.cs
└── .openapidotnet.json
```

Each API path segment gets its own builder class. Static segments (e.g., `/pets`) produce a `PetsBuilder` in the `Builders` folder, while parameterized segments (e.g., `/{petId}`) produce an `IdBuilder` nested inside a subfolder matching the parent segment (e.g., `Builders/Pets/IdBuilder.cs`). When the same segment name appears at different tree positions (e.g., `/pets` and `/owners/{ownerId}/pets`), the nested folder structure naturally avoids collisions.

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
    "string": "string",
    "string:date-time": "NodaTime.Instant",
    "string:date": "NodaTime.LocalDate",
    "string:uuid": "System.Guid",
    "integer": "long"
  },
  "generatedFiles": [
    "Models/Pet.cs",
    "Models/NewPet.cs",
    "IOpenApiBuilder.cs",
    "IOpenApiClient.cs",
    "IPetStoreClient.cs",
    "Builders/PetsBuilder.cs"
  ]
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
    "string:date-time": "NodaTime.Instant",
    "string:date": "NodaTime.LocalDate",
    "string:email": "EmailAddress",
    "integer": "long"
  }
}
```

In the example above:
- `string` with format `date-time` maps to `NodaTime.Instant` (via `--use-nodatime`)
- `string` with format `date` maps to `NodaTime.LocalDate` (via `--use-nodatime`)
- `string` with format `email` is a new custom mapping (no built-in default)
- `integer` without a format maps to `long` instead of the default `int`

### Example Generated Model

```csharp
namespace PetStoreClient.Models;

/// <summary>
/// A pet in the store
/// </summary>
public partial class Pet
{
    /// <summary>
    /// Unique identifier for the pet
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public required long Id { get; set; }

    /// <summary>
    /// Name of the pet
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Birth date of the pet
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("birthDate")]
    public System.DateOnly? BirthDate { get; set; }

    /// <summary>
    /// When the pet was created
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public System.DateTimeOffset? CreatedAt { get; set; }
}
```

With `--use-nodatime`, the same model uses NodaTime types:

```csharp
    [System.Text.Json.Serialization.JsonPropertyName("birthDate")]
    public NodaTime.LocalDate? BirthDate { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public NodaTime.Instant? CreatedAt { get; set; }
```

### Example Generated Enum

```csharp
namespace PetStoreClient.Models;

/// <summary>
/// The status of a pet in the store
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum PetStatus
{
    [System.Text.Json.Serialization.JsonStringEnumMemberName("available")]
    Available,

    [System.Text.Json.Serialization.JsonStringEnumMemberName("pending")]
    Pending,

    [System.Text.Json.Serialization.JsonStringEnumMemberName("sold")]
    Sold,
}
```

### Example Generated IOpenApiClient Interface

`IOpenApiClient` is a base interface containing the HTTP infrastructure. A separate named interface (derived from the `--client-name` option or the API title) inherits from it and exposes the top-level navigation properties:

```csharp
namespace PetStoreClient;

/// <summary>
/// Base interface for all OpenAPI clients
/// </summary>
public interface IOpenApiClient : IOpenApiBuilder
{
    System.Net.Http.HttpClient HttpClient { get; }
    System.Text.Json.JsonSerializerOptions JsonOptions { get; }

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
    public virtual PetStoreClient.Builders.PetsBuilder Pets => new(this);
}
```

### Example Generated Builder

```csharp
namespace PetStoreClient.Builders;

public partial class PetsBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected PetsBuilder() { }
    #pragma warning restore CS8618

    public PetsBuilder(IOpenApiBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public string GetPath() => $"{_parentBuilder.GetPath()}/pets";


    public IOpenApiClient Client => _parentBuilder.Client;

    public virtual PetStoreClient.Builders.Pets.IdBuilder this[long petId]
    {
        get => new(this, petId);
    }

    /// <summary>
    /// List all pets
    /// </summary>
    public virtual async System.Threading.Tasks.Task<System.Collections.Generic.List<PetStoreClient.Models.Pet>> Get(
        int? limit = default, System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();
        var queryString = new System.Collections.Generic.List<string>();

        if (limit is {} limitValue)
            queryString.Add($"limit={System.Uri.EscapeDataString(
                System.Text.Json.JsonSerializer.Serialize(limitValue, Client.JsonOptions).Trim('"'))}");
        if (queryString.Count > 0)
            url += "?" + string.Join("&", queryString);

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions
            .ReadFromJsonAsync<System.Collections.Generic.List<PetStoreClient.Models.Pet>>(
                response.Content, Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new System.InvalidOperationException($"Response from {url} is null");
    }
}
```

```csharp
namespace PetStoreClient.Builders.Pets;

public partial class IdBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected IdBuilder() { }
    #pragma warning restore CS8618

    private readonly long _petId;

    public IdBuilder(IOpenApiBuilder parentBuilder, long petId)
    {
        _parentBuilder = parentBuilder;
        _petId = petId;
    }

    public string GetPath() => $"{_parentBuilder.GetPath()}/{System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(_petId, Client.JsonOptions).Trim('"'))}";

    public IOpenApiClient Client => _parentBuilder.Client;

    public virtual PetStoreClient.Builders.Pets.Id.PhotosBuilder Photos => new(this);

    /// <summary>
    /// Get a pet by ID
    /// </summary>
    public virtual async System.Threading.Tasks.Task<PetStoreClient.Models.Pet> Get(
        System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions
            .ReadFromJsonAsync<PetStoreClient.Models.Pet>(
                response.Content, Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new System.InvalidOperationException($"Response from {url} is null");
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
public partial class StatsBuilder : IOpenApiBuilder
{
    // ... builder infrastructure ...

    /// <summary>
    /// Get statistics
    /// </summary>
    public virtual async System.Threading.Tasks.Task<GetResponse> Get(
        System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions
            .ReadFromJsonAsync<GetResponse>(
                response.Content, Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new System.InvalidOperationException($"Response from {url} is null");
    }

    public partial class GetResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("totalCount")]
        public int? TotalCount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("activeCount")]
        public int? ActiveCount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("lastUpdated")]
        public System.DateTimeOffset? LastUpdated { get; set; }  // or NodaTime.Instant? with --use-nodatime
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

**Default mode** — no additional packages are needed. All generated types (`DateTimeOffset`, `DateOnly`, `TimeOnly`, `DateTime`, `TimeSpan`) are built into .NET.

**With `--use-nodatime`** — add these packages to your project:

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
/pets                          → Builders.PetsBuilder (operations: Get, Post)
/pets/{petId}                  → Builders.Pets.IdBuilder (operations: Get, Delete)
/pets/{petId}/photos/{photoId} → Builders.Pets.Id.PhotosBuilder + Builders.Pets.Id.Photos.IdBuilder (operation: Get)
/owners/{ownerId}/pets/{petId} → Builders.OwnersBuilder + Builders.Owners.IdBuilder
                                 + Builders.Owners.Id.PetsBuilder + Builders.Owners.Id.Pets.IdBuilder
```

Static segments produce a named builder (e.g., `PetsBuilder`), while parameterized segments produce an `IdBuilder` nested inside a subfolder matching the parent segment. The nested folder structure naturally avoids collisions when the same segment name appears at different tree positions.

### Type Mapping Logic

The type mapping logic is driven by `TypeMappingConfig`, which holds a dictionary of mappings keyed by `"type:format"` (e.g. `"string:date-time"` → `"Instant"`) or just `"type"` for defaults (e.g. `"string"` → `"string"`). Custom mappings from the configuration file are merged on top of the built-in defaults.

The generator maps OpenAPI types and [format registry](https://spec.openapis.org/registry/format/index.html) values to C# types:

```
String formats
  "string"                             → string
  "string" (format: "date-time")       → DateTimeOffset (or NodaTime.Instant with --use-nodatime)
  "string" (format: "date")            → DateOnly (or NodaTime.LocalDate)
  "string" (format: "time")            → TimeOnly (or NodaTime.LocalTime)
  "string" (format: "time-local")      → TimeOnly (or NodaTime.LocalTime)
  "string" (format: "date-time-local") → DateTime (or NodaTime.LocalDateTime)
  "string" (format: "duration")        → TimeSpan (or NodaTime.Duration)
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
- **Builder Classes**: `{Segment}Builder` for static segments, `IdBuilder` in a nested namespace for parameterized segments (e.g., `Builders.PetsBuilder`, `Builders.Pets.IdBuilder`)
- **Collision Resolution**: Nested folder/namespace structure naturally avoids collisions when the same segment name appears at different tree positions
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
Projects using the generated code need (with `--use-nodatime`):
- NodaTime (3.3.0)
- NodaTime.Serialization.SystemTextJson (1.3.0)

With the default built-in types mode, no additional packages are required.

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
