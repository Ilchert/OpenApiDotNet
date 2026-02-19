# OpenAPI .NET Client Generator

A modern OpenAPI/Swagger client code generator for .NET that produces high-quality, strongly-typed HTTP clients with full NodaTime support for date and time handling.

## Features

- ? **Modern .NET**: Built for .NET 10 with C# 14.0
- ?? **NodaTime Integration**: Automatic mapping of date/time formats to NodaTime types (`Instant`, `LocalDate`, `LocalTime`, `LocalDateTime`, `Duration`)
- ?? **System.Text.Json**: Native JSON serialization with optimal performance
- ?? **Type-Safe**: Generates strongly-typed models and client methods
- ? **Async First**: All HTTP operations are async with proper cancellation support
- ?? **Well Documented**: Preserves OpenAPI descriptions as XML documentation comments
- ?? **Format Registry**: Comprehensive [OpenAPI Format Registry](https://spec.openapis.org/registry/format/index.html) support — integers, URIs, binary, decimals, and more
- ?? **Nullable Aware**: Respects required/optional properties with nullable reference types
- ?? **Modern CLI**: Uses `System.CommandLine` with built-in help, validation, and shell tab-completion

## Type Mapping

The generator maps OpenAPI types and formats to idiomatic C# types following the [OpenAPI Format Registry](https://spec.openapis.org/registry/format/index.html).

### String Formats

| OpenAPI Format | C# Type | Notes |
|---|---|---|
| `date-time` | `Instant` | NodaTime — RFC 3339 date-time |
| `date` | `LocalDate` | NodaTime — RFC 3339 full-date |
| `time` | `LocalTime` | NodaTime — RFC 3339 full-time |
| `time-local` | `LocalTime` | NodaTime — time without timezone |
| `date-time-local` | `LocalDateTime` | NodaTime — date-time without timezone |
| `duration` | `Duration` | NodaTime — RFC 3339 duration |
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

### Other Types

| OpenAPI Type | C# Type |
|---|---|
| `boolean` | `bool` |
| `array` | `List<T>` |
| `$ref` | Referenced class |

## Installation

### Prerequisites

- .NET 10.0 SDK or later
- Any IDE that supports .NET (Visual Studio, VS Code, Rider)

### Build from Source

```bash
git clone https://github.com/Ilchert/OpenApiDotNet.git
cd OpenApiDotNet
dotnet build
```

## Usage

### Command Line

```bash
dotnet run --project src/OpenApiDotNet -- <openapi-file> [options]
```

### Arguments & Options

| Argument / Option | Description | Default |
|---|---|---|
| `<openapi-file>` | Path to the OpenAPI specification file (JSON or YAML) | *required* |
| `-o`, `--output <dir>` | Directory where generated code will be placed | `./Generated` |
| `-n`, `--namespace <ns>` | Namespace for generated code | `GeneratedClient` |

Built-in flags provided by `System.CommandLine`:

| Flag | Description |
|---|---|
| `--help`, `-h`, `-?` | Show help and usage information |
| `--version` | Show version information |

### Shell Tab-Completion

The CLI supports shell tab-completion via the [`dotnet-suggest`](https://github.com/dotnet/command-line-api/blob/main/docs/dotnet-suggest.md) global tool.
Once configured, pressing <kbd>Tab</kbd> will auto-complete the `<openapi-file>` argument with `.json`, `.yaml`, and `.yml` files from the current directory.

```bash
# Install the global tool (one-time)
dotnet tool install -g dotnet-suggest

# Follow the shell-specific setup instructions from dotnet-suggest
```

### Examples

**Basic Usage:**
```bash
dotnet run --project src/OpenApiDotNet -- petstore.yaml
```

**With Custom Output Directory:**
```bash
dotnet run --project src/OpenApiDotNet -- api.yaml -o ./src/Client
```

**With Custom Namespace:**
```bash
dotnet run --project src/OpenApiDotNet -- swagger.json -o ./Generated -n MyCompany.ApiClient
```

**Show Help:**
```bash
dotnet run --project src/OpenApiDotNet -- --help
```

## Generated Code Structure

The generator creates the following structure:

```
Generated/
??? Models/
?   ??? Pet.cs
?   ??? User.cs
?   ??? Order.cs
??? [ApiName]Client.cs
??? JsonConfiguration.cs
```

### Example Generated Model

```csharp
using System.Text.Json.Serialization;
using NodaTime;

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
    public long Id { get; set; }

    /// <summary>
    /// Name of the pet
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Birth date of the pet
    /// </summary>
    [JsonPropertyName("birthDate")]
    public LocalDate? BirthDate { get; set; }

    /// <summary>
    /// When the pet was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public Instant? CreatedAt { get; set; }
}
```

### Example Generated Client

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using PetStoreClient.Models;

namespace PetStoreClient;

public class PetStoreAPIClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public PetStoreAPIClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _jsonOptions = JsonConfiguration.CreateOptions();
    }

    /// <summary>
    /// List all pets
    /// </summary>
    public async Task<List<Pet>> ListPetsAsync(int? limit, CancellationToken cancellationToken = default)
    {
        var url = "/pets";

        // Build query string with URL-encoded parameters
        var queryString = new List<string>();
        if (limit != null)
            queryString.Add($"limit={Uri.EscapeDataString(limit.ToString())}");
        if (queryString.Any())
            url += "?" + string.Join("&", queryString);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Pet>>(_jsonOptions, cancellationToken) 
            ?? throw new InvalidOperationException("Response was null");
    }

    /// <summary>
    /// Get a pet by ID
    /// </summary>
    public async Task<Pet> GetPetByIdAsync(long petId, CancellationToken cancellationToken = default)
    {
        // Build path with URL-encoded parameters
        var url = $"/pets/{Uri.EscapeDataString(petId.ToString())}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Pet>(_jsonOptions, cancellationToken) 
            ?? throw new InvalidOperationException("Response was null");
    }
}
```

## URL Encoding & Path Parameters

The generator automatically handles URL encoding for:
- **Path Parameters**: All path parameters are properly URL-encoded to handle special characters
- **Query Parameters**: Query string values are URL-encoded for safe transmission
- **Special Characters**: Spaces, ampersands, and other special characters are properly escaped

Example with special characters:
```csharp
// Path parameter encoding
await client.GetOwnerPetAsync("john doe", 123);
// Generates: /owners/john%20doe/pets/123

// Query parameter encoding
await client.ListPetsAsync(limit: 10, status: "available & active");
// Generates: /pets?limit=10&status=available%20%26%20active
```

## Using Generated Code

### 1. Add Required NuGet Packages

Add these packages to your project:

```xml
<PackageReference Include="NodaTime" Version="3.3.0" />
<PackageReference Include="NodaTime.Serialization.SystemTextJson" Version="1.3.0" />
```

### 2. Register HttpClient

```csharp
// Using HttpClientFactory (recommended)
builder.Services.AddHttpClient<PetStoreAPIClient>(client =>
{
    client.BaseAddress = new Uri("https://api.petstore.example.com");
});

// Or create manually
var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://api.petstore.example.com")
};
var client = new PetStoreAPIClient(httpClient);
```

### 3. Use the Client

```csharp
// List pets
var pets = await client.ListPetsAsync(limit: 10);

// Create a pet
var newPet = new NewPet 
{ 
    Name = "Fluffy",
    BirthDate = LocalDate.FromDateTime(DateTime.Now.AddYears(-2))
};
var createdPet = await client.CreatePetAsync(newPet);

// Get specific pet
var pet = await client.GetPetByIdAsync(123);

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

- **45 tests** covering all major functionality
- **100% pass rate**
- Unit tests for type mapping and naming conventions
- Integration tests with real OpenAPI specifications

## Architecture

### Core Components

1. **ClientGenerator**: Main orchestrator that generates all code
2. **Type Mapper**: Converts OpenAPI schemas to C# types with NodaTime support
3. **Model Generator**: Creates C# classes from OpenAPI schemas
4. **Client Generator**: Generates HTTP client with operation methods
5. **JSON Configuration**: Sets up System.Text.Json with NodaTime converters

### Type Mapping Logic

The generator maps OpenAPI types and [format registry](https://spec.openapis.org/registry/format/index.html) values to C# types:

```
String formats
  "string"                             → string
  "string" (format: "date-time")       → Instant        (NodaTime)
  "string" (format: "date")            → LocalDate      (NodaTime)
  "string" (format: "time")            → LocalTime      (NodaTime)
  "string" (format: "time-local")      → LocalTime      (NodaTime)
  "string" (format: "date-time-local") → LocalDateTime   (NodaTime)
  "string" (format: "duration")        → Duration       (NodaTime)
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

Other types
  "boolean"                            → bool
  "array"                              → List<T>
  Reference ($ref)                     → Custom Type
```

## Supported OpenAPI Features

- ? OpenAPI 3.0 specifications
- ? JSON and YAML input formats
- ? Path parameters with URL encoding
- ? Query parameters with URL encoding
- ? Multiple path parameters (e.g., `/owners/{ownerId}/pets/{petId}`)
- ? Request bodies
- ? Response models
- ? Schema references (`$ref`)
- ? Required/optional properties
- ? Arrays and nested objects
- ? HTTP methods: GET, POST, PUT, PATCH, DELETE
- ? Operation IDs for method naming
- ? Descriptions and summaries
- ? Special character encoding in URLs
- ? [OpenAPI Format Registry](https://spec.openapis.org/registry/format/index.html) type mappings

## Naming Conventions

The generator follows standard .NET naming conventions:

- **Classes**: PascalCase (e.g., `PetStoreClient`, `Pet`)
- **Properties**: PascalCase (e.g., `BirthDate`, `CreatedAt`)
- **Method Parameters**: camelCase (e.g., `petId`, `limit`)
- **JSON Properties**: Preserved from OpenAPI spec (typically camelCase)

The generator automatically converts:
- `birth_date` ? `BirthDate` (property) / `birthDate` (parameter)
- `created-at` ? `CreatedAt` (property) / `createdAt` (parameter)
- `user-name` ? `UserName` (property) / `userName` (parameter)

## Dependencies

### Runtime Dependencies
- Microsoft.OpenApi (1.6.22)
- Microsoft.OpenApi.Readers (1.6.22)
- NodaTime (3.3.0)
- NodaTime.Serialization.SystemTextJson (1.3.0)
- System.CommandLine (2.0.2)

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

**Made with ?? for the .NET community**
