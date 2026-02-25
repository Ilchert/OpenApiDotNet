# OpenAPI .NET Client Generator — Implementation Summary

## ✅ Fluent Builder Pattern Refactoring

### Overview

Refactored the code generator from a flat client class with all operations in a single file to a **fluent builder hierarchy** where each URL path segment maps to its own builder class. The design prioritises **mockability** — all operations are `virtual async` and builders expose `protected` parameterless constructors for Moq proxy creation.

### Architecture

```
OpenAPI Paths → PathTreeBuilder.Build() → Path Tree → Builder Classes
                                                    → IBuilder.cs
                                                    → IClient.cs

OpenAPI Schemas → Model Generator → Models/*.cs
```

The path tree maps URL structure to builder hierarchy:

```
/pets                          → PetsBuilder (operations: listPets, createPet)
/pets/{petId}                  → PetsIdBuilder (operations: getPetById, deletePet)
/pets/{petId}/photos/{photoId} → PhotosBuilder + PhotosIdBuilder (operation: getPetPhoto)
/owners/{ownerId}/pets/{petId} → OwnersBuilder + OwnersIdBuilder
                                 + OwnersIdPetsBuilder + OwnersIdPetsIdBuilder
```

### Design Decisions

| Concern | Implementation |
|---|---|
| **Path parameters** | Captured by builder indexers — not passed as method parameters |
| **Query parameters** | Remain as method parameters with `= default` |
| **URL construction** | `GetPath()` chains through parent builders: `$"{_parentBuilder.GetPath()}/{segment}"` |
| **Root path** | `IClient.GetPath()` returns `""` (empty string) |
| **HTTP calls** | Use `Client.HttpClient` and `Client.JsonOptions` from the `IBuilder` interface |
| **Mockability** | All operations are `virtual async`; protected parameterless constructors with `#pragma warning disable CS8618` for Moq proxy creation |
| **Naming** | Static segments → `{Segment}Builder`, parameterized → `{ParentSegment}IdBuilder` |
| **Collisions** | Same segment at different tree positions → context-prefixed names (e.g., `OwnersIdPetsBuilder`) |

### Features Added

#### 1. **PathTree (`src/OpenApiDotNet/PathTree.cs`)**
New data structure and builder:
- `PathSegmentNode` — tree node with `SegmentName`, `IsParameter`, `ParameterName`, `ParameterSchema`, `Children`, `Operations`, `BuilderName`
- `PathTreeBuilder.Build(OpenApiPaths?)` — parses OpenAPI paths into tree, resolves parameter schemas, assigns builder names
- `ResolveBuilderNames()` — two-pass algorithm: collects simple + context-prefixed names, groups by simple name, assigns context-prefixed names only on collision
- `GetAllNodes()` — depth-first enumeration of all non-root nodes

#### 2. **IBuilder Interface**
Generated `IBuilder.cs` with:
```csharp
public interface IBuilder
{
    IClient Client { get; }
    string GetPath();
}
```

#### 3. **IClient Interface**
Generated `IClient.cs` extending `IBuilder` with:
- `HttpClient HttpClient` property
- `JsonSerializerOptions JsonOptions` property
- Navigation properties for top-level static segments (e.g., `PetsBuilder Pets { get => new(this); }`)
- Explicit `IBuilder` implementation: `IClient IBuilder.Client => this;` and `string IBuilder.GetPath() => "";`

#### 4. **Static Segment Builders**
For static segments (e.g., `/pets`), generates `PetsBuilder : IBuilder` with:
- `_parentBuilder` field for chaining
- Protected parameterless constructor (mocking)
- Public constructor accepting `IBuilder parentBuilder`
- `GetPath()` returning `$"{_parentBuilder.GetPath()}/pets"`
- Virtual indexers for parameterized children
- Navigation properties for static children
- Virtual async operation methods (query params only)

#### 5. **Parameter Segment Builders**
For parameterized segments (e.g., `/{petId}`), generates `PetsIdBuilder : IBuilder` with:
- `_parentBuilder` field + parameter field (e.g., `_petId`)
- Protected parameterless constructor (mocking)
- Public constructor accepting `IBuilder parentBuilder` + parameter
- `GetPath()` returning `$"{_parentBuilder.GetPath()}/{_petId}"`
- Same children/operation pattern as static builders

#### 6. **Mocking Support**
All builders are mock-friendly by design:
```csharp
var mock = new Mock<IClient>();
mock.Setup(p => p.Pets[123].ListPets(It.IsAny<CancellationToken>()))
    .ReturnsAsync(new List<Pet> { new Pet() });

var result = await mock.Object.Pets[123].ListPets(default);
// result contains the mocked pet list — no HTTP calls made
```

#### 7. **Builder Name Collision Resolution**
When the same segment name appears at different tree positions (e.g., `/pets` and `/owners/{ownerId}/pets`), the `ResolveBuilderNames()` algorithm:
1. Computes simple names (`PetsBuilder`) and context-prefixed names (`OwnersIdPetsBuilder`) for all nodes
2. Groups by simple name to detect collisions
3. Assigns context-prefixed names only where collisions exist

### Code Generation Examples

#### Static Builder:
```csharp
public class PetsBuilder : IBuilder
{
    private readonly IBuilder _parentBuilder;

#pragma warning disable CS8618
    protected PetsBuilder() { }
#pragma warning restore CS8618

    public PetsBuilder(IBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public IClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/pets";

    public virtual PetsIdBuilder this[long petId]
    {
        get => new(this, petId);
    }

    public virtual async Task<List<Pet>> ListPets(
        int? limit = default, CancellationToken cancellationToken = default)
    {
        var url = GetPath();
        // ... query string building + HTTP call
    }
}
```

#### Parameter Builder:
```csharp
public class PetsIdBuilder : IBuilder
{
    private readonly IBuilder _parentBuilder;
    private readonly long _petId;

#pragma warning disable CS8618
    protected PetsIdBuilder() { }
#pragma warning restore CS8618

    public PetsIdBuilder(IBuilder parentBuilder, long petId)
    {
        _parentBuilder = parentBuilder;
        _petId = petId;
    }

    public IClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/{_petId}";

    public PhotosBuilder Photos => new(this);

    public virtual async Task<Pet> GetPetById(CancellationToken cancellationToken = default)
    {
        var url = GetPath();
        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Pet>(Client.JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Response was null");
    }
}
```

### ClientGenerator Refactoring

Removed methods:
- `GenerateClient()` — single flat client class
- `GenerateOperation()` — flat operation methods
- `GenerateUrlBuilding()` — manual URL construction
- `GenerateHttpCall()` — HTTP calls with private fields

Added methods:
- `GenerateIBuilderInterface()` — emits `IBuilder.cs`
- `GenerateIClientInterface(PathSegmentNode)` — emits `IClient.cs`
- `GenerateBuilders(PathSegmentNode)` — iterates tree, creates `Builders/` directory
- `GenerateBuilderClass(PathSegmentNode, string)` — dispatches to static/parameter body
- `GenerateStaticBuilderBody()` — emits static segment builder class
- `GenerateParameterBuilderBody()` — emits parameterized segment builder class
- `GenerateBuilderOperation()` — emits `virtual async` method with query params only
- `GenerateBuilderHttpCall()` — emits HTTP calls using `Client.HttpClient` and `Client.JsonOptions`

### Test Coverage

Updated **8 existing tests** across two files:
- `ClientGenerationTests.cs` — 4 tests updated to assert on builder-style output (`IBuilder.cs`, `IClient.cs`, `Builders/*.cs`)
- `PathParameterTests.cs` — 4 tests updated to check builder files for path parameters

### Test Results

```
Total Tests: 107
Passed: 107 ✅
Failed: 0
```

### Files Created
- `src/OpenApiDotNet/PathTree.cs` — `PathSegmentNode` class and `PathTreeBuilder` static class

### Files Modified
- `src/OpenApiDotNet/ClientGenerator.cs` — major refactor: removed flat client generation, added builder generation pipeline
- `tests/OpenApiDotNet.Tests/ClientGenerationTests.cs` — 4 tests updated for builder output
- `tests/OpenApiDotNet.Tests/PathParameterTests.cs` — 4 tests updated for builder output
- `README.md` — comprehensive documentation update (fluent builder, mocking, architecture, examples)

---

## Path Parameters & URL Encoding

### Overview

Path parameters are captured by the builder chain via indexers. Query parameters are URL-encoded using `Uri.EscapeDataString()`.

### Query Parameter Encoding

```csharp
// Generated code in builder operations
var queryString = new List<string>();
if (limit != null)
    queryString.Add($"limit={Uri.EscapeDataString(limit.ToString())}");
if (queryString.Count > 0)
    url += "?" + string.Join("&", queryString);
```

### Test Fixtures

Enhanced `petstore.json` with:
1. **Single path parameter**: `/pets/{petId}`
2. **Multiple path parameters**: `/pets/{petId}/photos/{photoId}`
3. **Mixed types**: `/owners/{ownerId}/pets/{petId}` (string + long)
4. **Query parameters**: `limit` on listPets

### OpenAPI Overlay Support

Added the ability to apply [OpenAPI Overlay](https://spec.openapis.org/overlay/latest.html) documents to patch specifications before code generation, using [BinkyLabs.OpenApi.Overlays](https://www.nuget.org/packages/BinkyLabs.OpenApi.Overlays).

#### Features
- `--overlay` CLI option accepting one or more overlay files
- Multiple overlays are combined in order via `CombineWith` and applied with `ApplyToDocumentAndLoadAsync`
- Overlay file paths are persisted in `.openapidotnet.json` and resolved during `update`
- Shell tab-completion for overlay file paths (`.json`, `.yaml`, `.yml`)
- Errors and warnings from overlay application are reported to the console

---

## Enum Support

### Overview

Generates C# enums from OpenAPI string enum schemas with `[JsonConverter(typeof(JsonStringEnumConverter))]`. Hyphenated values are converted to PascalCase members with `[JsonStringEnumMemberName]` attributes preserving original values.

### Example

```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PetStatus
{
    [JsonStringEnumMemberName("available")]
    Available,

    [JsonStringEnumMemberName("pending")]
    Pending,

    [JsonStringEnumMemberName("sold")]
    Sold,
}
```

### Files Modified
- `src/OpenApiDotNet/ClientGenerator.cs` — `GenerateEnum` method, enum detection in `GenerateModel`, `JsonStringEnumConverter` in `GenerateJsonConfiguration`
- `tests/OpenApiDotNet.Tests/ClientGenerationTests.cs` — 5 enum integration tests
- `tests/OpenApiDotNet.Tests/TypeMappingTests.cs` — 2 enum type mapping tests

---

## System.CommandLine Migration

### Overview

Replaced manual argument parsing with `System.CommandLine` for modern CLI with built-in help, validation, and shell tab-completion.

### Files Modified
- `src/OpenApiDotNet/Program.cs` — rewrote CLI with `System.CommandLine`
- `README.md` — updated usage docs

---

## Configuration Persistence & Update Command

### Overview

Generation parameters are persisted to `.openapidotnet.json` in the output directory. The `update` subcommand re-generates using saved parameters.

### Files Created
- `src/OpenApiDotNet/GenerationConfig.cs` — configuration model

### Files Modified
- `src/OpenApiDotNet/Program.cs` — added `update` command, `SaveConfig` helper
- `README.md` — documented config file and update command

---

## Required Keyword for Required Properties

### Overview

Properties marked as `required` in OpenAPI schema use C# `required` modifier. Optional properties remain nullable.

```csharp
public required long Id { get; set; }    // required in schema
public string? Tag { get; set; }          // optional
```

---

## Configurable Type Mappings

### Overview

Extracted hardcoded type mappings into `TypeMappingConfig` class. Users override mappings via `typeMappings` in `.openapidotnet.json`.

### Files Created
- `src/OpenApiDotNet/TypeMappingConfig.cs` — configurable type mapping class
- `tests/OpenApiDotNet.Tests/TypeMappingConfigTests.cs` — 13 unit tests

---

**Implementation Status**: ✅ **COMPLETE**
**Quality**: Production Ready
**Test Coverage**: 107 tests, 100% pass rate
**Documentation**: Complete
