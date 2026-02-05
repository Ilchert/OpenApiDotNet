using FluentAssertions;
using Microsoft.OpenApi.Models;
using OpenApiDotNet;

namespace OpenApiDotNet.Tests;

public class TypeMappingTests
{
    [Fact]
    public void GetCSharpType_StringType_ReturnsString()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string" };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("string");
    }

    [Fact]
    public void GetCSharpType_StringWithDateTimeFormat_ReturnsInstant()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string", Format = "date-time" };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("Instant");
    }

    [Fact]
    public void GetCSharpType_StringWithDateFormat_ReturnsLocalDate()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string", Format = "date" };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("LocalDate");
    }

    [Fact]
    public void GetCSharpType_StringWithTimeFormat_ReturnsLocalTime()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string", Format = "time" };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("LocalTime");
    }

    [Fact]
    public void GetCSharpType_StringWithUuidFormat_ReturnsGuid()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "string", Format = "uuid" };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("Guid");
    }

    [Fact]
    public void GetCSharpType_IntegerType_ReturnsInt()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "integer" };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("int");
    }

    [Fact]
    public void GetCSharpType_IntegerWithInt64Format_ReturnsLong()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "integer", Format = "int64" };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("long");
    }

    [Fact]
    public void GetCSharpType_NumberType_ReturnsDouble()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "number" };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("double");
    }

    [Fact]
    public void GetCSharpType_NumberWithFloatFormat_ReturnsFloat()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "number", Format = "float" };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("float");
    }

    [Fact]
    public void GetCSharpType_BooleanType_ReturnsBool()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "boolean" };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("bool");
    }

    [Fact]
    public void GetCSharpType_ArrayOfStrings_ReturnsListOfString()
    {
        // Arrange
        var schema = new OpenApiSchema 
        { 
            Type = "array",
            Items = new OpenApiSchema { Type = "string" }
        };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("List<string>");
    }

    [Fact]
    public void GetCSharpType_ArrayOfObjects_ReturnsListOfObject()
    {
        // Arrange
        var schema = new OpenApiSchema 
        { 
            Type = "array",
            Items = new OpenApiSchema { Type = "object" }
        };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("List<object>");
    }

    [Fact]
    public void GetCSharpType_Reference_ReturnsReferencedTypeName()
    {
        // Arrange
        var schema = new OpenApiSchema 
        { 
            Reference = new OpenApiReference
            {
                Id = "Pet",
                Type = ReferenceType.Schema
            }
        };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("Pet");
    }

    [Fact]
    public void GetCSharpType_UnknownType_ReturnsObject()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = "unknown" };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("object");
    }

    private static ClientGenerator CreateGenerator()
    {
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths()
        };
        return new ClientGenerator(document, "TestNamespace", Path.GetTempPath());
    }
}
