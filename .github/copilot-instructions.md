# Copilot Instructions

## Project Overview

OpenApiDotNet is a .NET CLI tool that generates strongly-typed HTTP clients from OpenAPI specifications. It produces fluent builder-style APIs with NodaTime support.

### Solution Structure

| Project | Path | Purpose |
|---|---|---|
| `OpenApiDotNet` | `src/OpenApiDotNet/` | Main CLI tool & code generator |
| `OpenApiDotNet.Tests` | `tests/OpenApiDotNet.Tests/` | Unit & integration tests |
| `OpenApiDotNet.Demo` | `samples/OpenApiDotNet.Demo/` | Sample generated client usage |

### Key Architecture

- **`Program.cs`** — CLI entry point using `System.CommandLine`
- **`OpenApiGenerator`** — Orchestrates generation by coordinating generators
- **`PathTreeBuilder`** — Parses OpenAPI paths into a segment tree, resolves builder names
- **`GeneratorContext`** — Holds namespace, type mapping config, naming helpers (`ToPascalCase`, `ToCamelCase`)
- **`BaseGenerator`** — Abstract base for all generators; provides `WriteNamespace`, `WriteSummary`, and `Write(CodeWriter)` pattern
- **`CodeWriter`** — `StringBuilder` wrapper with indent/unindent support (4-space indentation)
- **`TypeMappingConfig`** — Configurable OpenAPI-to-.NET type mappings with built-in defaults

Generator hierarchy under `Generators/`:
`ClientGenerator`, `BuilderGenerator`, `BuilderOperationGenerator`, `BuilderPropertyGenerator`, `ObjectGenerator`, `EnumGenerator`, `ResponseGenerator`, `BodyGenerator`, `QueryParameterGenerator`

## Project Guidelines

### Target & Language
- Target framework: **.NET 10** (`net10.0`)
- Language version: **C# 14.0**
- Nullable reference types: **enabled**
- Implicit usings: **enabled**

### Coding Conventions
- Use **file-scoped namespaces** (`namespace Foo;`)
- Source classes in `src/OpenApiDotNet/` are **`internal`** (exposed to tests via `InternalsVisibleTo`)
- Use **4-space indentation** (no tabs) — enforced by `.editorconfig`
- Follow existing patterns: field naming `_camelCase`, static fields `s_camelCase`
- Prefer **pattern matching** and **collection expressions** (`[]`) over explicit constructors
- Use **raw string literals** (`"""..."""`) for multi-line code generation templates

### Generated Code Rules
- In generated code, use **fully qualified type names (FQN)** instead of writing `using` directives at the top of generated files
- For enum serialization in System.Text.Json (.NET 9+), use **`[JsonStringEnumMemberName]`** attribute instead of `[JsonPropertyName]` on enum members
- Generated enums use `[JsonStringEnumConverter]` as the class-level converter
- The `CodeWriter` class handles indentation — use `writer.Indent()` / `writer.Unindent()` and `writer.WriteLine()` for emitting code

### Package Management
- Uses **Central Package Management** via `Directory.Packages.props` — do not specify `Version` attributes in individual `.csproj` files
- Add or update package versions only in `Directory.Packages.props`

### Testing
- Test framework: **xunit v3** (`xunit.v3` package)
- Assertions: **FluentAssertions** (`.Should().Be(...)`, `.Should().BeEmpty()`, etc.)
- Mocking: **Moq** (`Mock<T>`, `.Setup(...)`, `.ReturnsAsync(...)`)
- Test fixtures (OpenAPI spec files) live in `tests/OpenApiDotNet.Tests/Fixtures/` and are copied to output via `<Content Include="Fixtures\**\*" CopyToOutputDirectory="PreserveNewest" />`
- Global using for `Xunit` is configured in the test project
- Tests use `IDisposable` for temp directory cleanup when generating files
- Integration tests (`CompilationVerificationTests`) verify generated code compiles using Roslyn `CSharpCompilation`

### Build & Test Commands
```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run a specific test class
dotnet test --filter "FullyQualifiedName~TypeMappingTests"

# Run the generator CLI
dotnet run --project src/OpenApiDotNet -- <openapi-file> [options]
```

### README
- `README.md` is at the repository root and is packed into the NuGet tool package
- Keep the README updated when adding new features, CLI options, or type mappings
- The README documents: features, type mapping tables, CLI usage/examples, generated code examples, architecture, and roadmap

### Examples & Demo
- `samples/OpenApiDotNet.Demo/` contains a working sample using the generated client
- `samples/OpenApiDotNet.Demo/Generated/` contains pre-generated code from a petstore spec
- Update the demo when generator output format changes