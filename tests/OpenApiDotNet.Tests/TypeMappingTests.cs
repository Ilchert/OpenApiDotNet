using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.OpenApi;
using OpenApiDotNet;

namespace OpenApiDotNet.Tests;

public class TypeMappingTests
{
    [Fact]
    public void GetCSharpType_StringType_ReturnsString()
    {
        // Arrange
        var schema = new OpenApiSchema { Type =JsonSchemaType.String };
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
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
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
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" };
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
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "time" };
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
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
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
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer };
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
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int64" };
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
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number };
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
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "float" };
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
        var schema = new OpenApiSchema { Type = JsonSchemaType.Boolean };
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
            Type = JsonSchemaType.Array,
            Items = new OpenApiSchema { Type = JsonSchemaType.String }
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
            Type = JsonSchemaType.Array,
            Items = new OpenApiSchema { Type = JsonSchemaType.Object }
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
            Id = "Pet"
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
        var schema = new OpenApiSchema { Type = null };
        var generator = CreateGenerator();

        // Act
        var result = generator.GetCSharpType(schema);

        // Assert
        result.Should().Be("object");
    }

    [Fact]
    public void GetCSharpType_StringWithDurationFormat_ReturnsDuration()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "duration" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("Duration");
    }

    [Fact]
    public void GetCSharpType_StringWithDateTimeLocalFormat_ReturnsLocalDateTime()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time-local" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("LocalDateTime");
    }

    [Fact]
    public void GetCSharpType_StringWithTimeLocalFormat_ReturnsLocalTime()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "time-local" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("LocalTime");
    }

    [Fact]
    public void GetCSharpType_StringWithUriFormat_ReturnsUri()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("Uri");
    }

    [Fact]
    public void GetCSharpType_StringWithUriReferenceFormat_ReturnsUri()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri-reference" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("Uri");
    }

    [Fact]
    public void GetCSharpType_StringWithIriFormat_ReturnsUri()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "iri" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("Uri");
    }

    [Fact]
    public void GetCSharpType_StringWithIriReferenceFormat_ReturnsUri()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "iri-reference" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("Uri");
    }

    [Fact]
    public void GetCSharpType_StringWithByteFormat_ReturnsByteArray()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "byte" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("byte[]");
    }

    [Fact]
    public void GetCSharpType_StringWithBinaryFormat_ReturnsByteArray()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("byte[]");
    }

    [Fact]
    public void GetCSharpType_StringWithBase64UrlFormat_ReturnsByteArray()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "base64url" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("byte[]");
    }

    [Fact]
    public void GetCSharpType_StringWithCharFormat_ReturnsChar()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "char" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("char");
    }

    [Fact]
    public void GetCSharpType_IntegerWithInt32Format_ReturnsInt()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("int");
    }

    [Fact]
    public void GetCSharpType_IntegerWithInt16Format_ReturnsShort()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int16" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("short");
    }

    [Fact]
    public void GetCSharpType_IntegerWithInt8Format_ReturnsSbyte()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int8" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("sbyte");
    }

    [Fact]
    public void GetCSharpType_IntegerWithUint8Format_ReturnsByte()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "uint8" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("byte");
    }

    [Fact]
    public void GetCSharpType_IntegerWithUint16Format_ReturnsUshort()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "uint16" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("ushort");
    }

    [Fact]
    public void GetCSharpType_IntegerWithUint32Format_ReturnsUint()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "uint32" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("uint");
    }

    [Fact]
    public void GetCSharpType_IntegerWithUint64Format_ReturnsUlong()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "uint64" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("ulong");
    }

    [Fact]
    public void GetCSharpType_NumberWithDecimalFormat_ReturnsDecimal()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "decimal" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("decimal");
    }

    [Fact]
    public void GetCSharpType_NumberWithDecimal128Format_ReturnsDecimal()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "decimal128" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("decimal");
    }

    [Fact]
    public void GetCSharpType_NumberWithDoubleIntFormat_ReturnsLong()
    {
        var schema = new OpenApiSchema { Type = JsonSchemaType.Number, Format = "double-int" };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("long");
    }

    [Fact]
    public void GetCSharpType_EnumWithReference_ReturnsEnumName()
    {
        var schema = new OpenApiSchema
        {
            Id = "PetStatus"
        };
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("PetStatus");
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
        var generator = CreateGenerator();

        var result = generator.GetCSharpType(schema);

        result.Should().Be("string");
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
