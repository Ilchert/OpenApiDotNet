using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.OpenApi;
using OpenApiDotNet;
using OpenApiDotNet.Generators;

namespace OpenApiDotNet.Tests;

public class TypeMappingTests
{
    [Fact]
    public void GetCSharpType_StringType_ReturnsString()
    {
        // Arrange
        var schema = new OpenApiSchema { Type =JsonSchemaType.String };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("string");
    }

    [Fact]
    public void GetCSharpType_StringWithDateTimeFormat_ReturnsInstant()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("NodaTime.Instant");
    }

    [Fact]
    public void GetCSharpType_StringWithDateFormat_ReturnsLocalDate()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("NodaTime.LocalDate");
    }

    [Fact]
    public void GetCSharpType_StringWithTimeFormat_ReturnsLocalTime()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "time" };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("NodaTime.LocalTime");
    }

    [Fact]
    public void GetCSharpType_StringWithUuidFormat_ReturnsGuid()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("System.Guid");
    }

    [Fact]
    public void GetCSharpType_IntegerType_ReturnsInt()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("int");
    }

    [Fact]
    public void GetCSharpType_IntegerWithInt64Format_ReturnsLong()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("long");
    }

    [Fact]
    public void GetCSharpType_NumberType_ReturnsDouble()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("double");
    }

    [Fact]
    public void GetCSharpType_NumberWithFloatFormat_ReturnsFloat()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "float" };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("float");
    }

    [Fact]
    public void GetCSharpType_BooleanType_ReturnsBool()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = JsonSchemaType.Boolean };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("bool");
    }

    [Fact]
    public void GetCSharpType_ArrayOfStrings_ReturnsListOfString()
    {
        // Arrange
        var schema = new OpenApiSchema 
        { 
            Type = JsonSchemaType.Array,
            Items = new OpenApiSchema { Type = JsonSchemaType.String }
        };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("System.Collections.Generic.List<string>");
    }

    [Fact]
    public void GetCSharpType_ArrayOfObjects_ReturnsListOfObject()
    {
        // Arrange
        var schema = new OpenApiSchema 
        { 
            Type = JsonSchemaType.Array,
            Items = new OpenApiSchema { Type = JsonSchemaType.Object }
        };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("System.Collections.Generic.List<object>");
    }

    [Fact]
    public void GetCSharpType_Reference_ReturnsFullyQualifiedTypeName()
    {
        // Arrange
        var schema = new OpenApiSchema 
        { 
            Id = "Pet"
        };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("TestNamespace.Models.Pet");
    }

    [Fact]
    public void GetCSharpType_UnknownType_ReturnsObject()
    {
        // Arrange
        var schema = new OpenApiSchema { Type = null };
        var context = CreateContext();

        // Act
        var result = context.GetCSharpType(schema);

        // Assert
        result.FullName.Should().Be("object");
    }

    [Fact]
    public void GetCSharpType_StringWithDurationFormat_ReturnsDuration()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "duration" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("NodaTime.Duration");
    }

    [Fact]
    public void GetCSharpType_StringWithDateTimeLocalFormat_ReturnsLocalDateTime()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time-local" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("NodaTime.LocalDateTime");
    }

    [Fact]
    public void GetCSharpType_StringWithTimeLocalFormat_ReturnsLocalTime()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "time-local" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("NodaTime.LocalTime");
    }

    [Fact]
    public void GetCSharpType_StringWithUriFormat_ReturnsUri()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("System.Uri");
    }

    [Fact]
    public void GetCSharpType_StringWithUriReferenceFormat_ReturnsUri()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri-reference" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("System.Uri");
    }

    [Fact]
    public void GetCSharpType_StringWithIriFormat_ReturnsUri()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "iri" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("System.Uri");
    }

    [Fact]
    public void GetCSharpType_StringWithIriReferenceFormat_ReturnsUri()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "iri-reference" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("System.Uri");
    }

    [Fact]
    public void GetCSharpType_StringWithByteFormat_ReturnsByteArray()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "byte" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("byte[]");
    }

    [Fact]
    public void GetCSharpType_StringWithBinaryFormat_ReturnsByteArray()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("byte[]");
    }

    [Fact]
    public void GetCSharpType_StringWithBase64UrlFormat_ReturnsByteArray()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "base64url" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("byte[]");
    }

    [Fact]
    public void GetCSharpType_StringWithCharFormat_ReturnsChar()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "char" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("char");
    }

    [Fact]
    public void GetCSharpType_IntegerWithInt32Format_ReturnsInt()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("int");
    }

    [Fact]
    public void GetCSharpType_IntegerWithInt16Format_ReturnsShort()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int16" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("short");
    }

    [Fact]
    public void GetCSharpType_IntegerWithInt8Format_ReturnsSbyte()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int8" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("sbyte");
    }

    [Fact]
    public void GetCSharpType_IntegerWithUint8Format_ReturnsByte()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "uint8" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("byte");
    }

    [Fact]
    public void GetCSharpType_IntegerWithUint16Format_ReturnsUshort()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "uint16" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("ushort");
    }

    [Fact]
    public void GetCSharpType_IntegerWithUint32Format_ReturnsUint()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "uint32" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("uint");
    }

    [Fact]
    public void GetCSharpType_IntegerWithUint64Format_ReturnsUlong()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "uint64" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("ulong");
    }

    [Fact]
    public void GetCSharpType_NumberWithDecimalFormat_ReturnsDecimal()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "decimal" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("decimal");
    }

    [Fact]
    public void GetCSharpType_NumberWithDecimal128Format_ReturnsDecimal()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "decimal128" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("decimal");
    }

    [Fact]
    public void GetCSharpType_NumberWithDoubleIntFormat_ReturnsLong()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double-int" };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("long");
    }

    [Fact]
    public void GetCSharpType_EnumWithReference_ReturnsFullyQualifiedEnumName()
    {
        var schema = new OpenApiSchema
        {
            Id = "PetStatus"
        };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("TestNamespace.Models.PetStatus");
    }

    [Fact]
    public void GetCSharpType_InlineStringEnum_ReturnsString()
    {
        var schema = new OpenApiSchema
        {
            Type = JsonSchemaType.String,
            Enum = new List<System.Text.Json.Nodes.JsonNode>
            {
                JsonValue.Create("available"),
                JsonValue.Create("pending"),
                JsonValue.Create("sold")
            }
        };
        var context = CreateContext();

        var result = context.GetCSharpType(schema);

        result.FullName.Should().Be("string");
    }

    private static GeneratorContext CreateContext()
    {
        return new GeneratorContext("TestNamespace", "TestClient", null);
    }
}
