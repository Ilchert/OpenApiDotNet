# OpenAPI .NET Client Generator

A modern OpenAPI/Swagger client code generator for .NET that produces high-quality, strongly-typed HTTP clients with full NodaTime support for date and time handling.

## Features

- ? **Modern .NET**: Built for .NET 10 with C# 14.0
- ?? **NodaTime Integration**: Automatic mapping of date/time formats to NodaTime types
- ?? **System.Text.Json**: Native JSON serialization with optimal performance
- ?? **Type-Safe**: Generates strongly-typed models and client methods
- ? **Async First**: All HTTP operations are async with proper cancellation support
- ?? **Well Documented**: Preserves OpenAPI descriptions as XML documentation comments
- ?? **Nullable Aware**: Respects required/optional properties with nullable reference types

## NodaTime Type Mapping

The generator automatically maps OpenAPI date/time formats to appropriate NodaTime types:

| OpenAPI Format | C# Type |
|----------------|---------|
| `date-time` | `Instant` |
| `date` | `LocalDate` |
| `time` | `LocalTime` |
| `uuid` | `Guid` |

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
dotnet run --project src/OpenApiDotNet <openapi-file> [output-directory] [namespace]
```

### Parameters

- `openapi-file` - Path to your OpenAPI specification file (YAML or JSON)
- `output-directory` - Directory where generated code will be placed (default: `./Generated`)
- `namespace` - Namespace for generated code (default: `GeneratedClient`)

### Examples

**Basic Usage:**
```bash
dotnet run --project src/OpenApiDotNet petstore.yaml
```

**With Custom Output Directory:**
```bash
dotnet run --project src/OpenApiDotNet api.yaml ./src/Client
```

**With Custom Namespace:**
```bash
dotnet run --project src/OpenApiDotNet swagger.json ./Generated MyCompany.ApiClient
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
        var queryParams = new List<string>();
        if (limit != null) queryParams.Add($"limit={limit}");
        var url = $"/pets" + (queryParams.Any() ? "?" + string.Join("&", queryParams) : "");

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Pet>>(_jsonOptions, cancellationToken) 
            ?? throw new InvalidOperationException("Response was null");
    }
}
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

The generator intelligently maps OpenAPI types:

```csharp
"string" ? string
"string" (format: "date-time") ? Instant
"string" (format: "date") ? LocalDate
"string" (format: "time") ? LocalTime
"string" (format: "uuid") ? Guid
"integer" ? int
"integer" (format: "int64") ? long
"number" ? double
"number" (format: "float") ? float
"boolean" ? bool
"array" ? List<T>
Reference ? Custom Type
```

## Supported OpenAPI Features

- ? OpenAPI 3.0 specifications
- ? JSON and YAML input formats
- ? Path parameters
- ? Query parameters
- ? Request bodies
- ? Response models
- ? Schema references (`$ref`)
- ? Required/optional properties
- ? Arrays and nested objects
- ? HTTP methods: GET, POST, PUT, PATCH, DELETE
- ? Operation IDs for method naming
- ? Descriptions and summaries

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
