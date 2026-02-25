using FluentAssertions;
using Microsoft.OpenApi;
using OpenApiDotNet;

namespace OpenApiDotNet.Tests;

public class TypeMappingConfigTests
{
    [Fact]
    public void Resolve_DefaultStringType_ReturnsString()
    {
        var config = new TypeMappingConfig();

        var result = config.Resolve(JsonSchemaType.String, null);

        result.Should().Be("string");
    }

    [Fact]
    public void Resolve_DefaultStringWithDateTimeFormat_ReturnsInstant()
    {
        var config = new TypeMappingConfig();

        var result = config.Resolve(JsonSchemaType.String, "date-time");

        result.Should().Be("Instant");
    }

    [Fact]
    public void Resolve_DefaultIntegerType_ReturnsInt()
    {
        var config = new TypeMappingConfig();

        var result = config.Resolve(JsonSchemaType.Integer, null);

        result.Should().Be("int");
    }

    [Fact]
    public void Resolve_DefaultBooleanType_ReturnsBool()
    {
        var config = new TypeMappingConfig();

        var result = config.Resolve(JsonSchemaType.Boolean, null);

        result.Should().Be("bool");
    }

    [Fact]
    public void Resolve_ArrayType_ReturnsNull()
    {
        var config = new TypeMappingConfig();

        var result = config.Resolve(JsonSchemaType.Array, null);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_NullType_ReturnsNull()
    {
        var config = new TypeMappingConfig();

        var result = config.Resolve(null, null);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_CustomOverride_OverridesDefault()
    {
        var customMappings = new Dictionary<string, string>
        {
            ["string:date-time"] = "DateTimeOffset"
        };
        var config = new TypeMappingConfig(customMappings);

        var result = config.Resolve(JsonSchemaType.String, "date-time");

        result.Should().Be("DateTimeOffset");
    }

    [Fact]
    public void Resolve_CustomDefaultOverride_OverridesDefault()
    {
        var customMappings = new Dictionary<string, string>
        {
            ["integer"] = "long"
        };
        var config = new TypeMappingConfig(customMappings);

        var result = config.Resolve(JsonSchemaType.Integer, null);

        result.Should().Be("long");
    }

    [Fact]
    public void Resolve_CustomNewFormat_AddsMapping()
    {
        var customMappings = new Dictionary<string, string>
        {
            ["string:email"] = "EmailAddress"
        };
        var config = new TypeMappingConfig(customMappings);

        var result = config.Resolve(JsonSchemaType.String, "email");

        result.Should().Be("EmailAddress");
    }

    [Fact]
    public void Resolve_CustomOverrideDoesNotAffectOtherMappings()
    {
        var customMappings = new Dictionary<string, string>
        {
            ["string:date-time"] = "DateTimeOffset"
        };
        var config = new TypeMappingConfig(customMappings);

        // Other string format mappings should remain unchanged
        config.Resolve(JsonSchemaType.String, "date").Should().Be("LocalDate");
        config.Resolve(JsonSchemaType.String, "uuid").Should().Be("Guid");
        config.Resolve(JsonSchemaType.String, null).Should().Be("string");
    }

    [Fact]
    public void GetDefaults_ReturnsAllExpectedMappings()
    {
        var defaults = TypeMappingConfig.GetDefaults();

        defaults.Should().ContainKey("string").WhoseValue.Should().Be("string");
        defaults.Should().ContainKey("string:date-time").WhoseValue.Should().Be("Instant");
        defaults.Should().ContainKey("string:uuid").WhoseValue.Should().Be("Guid");
        defaults.Should().ContainKey("integer").WhoseValue.Should().Be("int");
        defaults.Should().ContainKey("integer:int64").WhoseValue.Should().Be("long");
        defaults.Should().ContainKey("number").WhoseValue.Should().Be("double");
        defaults.Should().ContainKey("number:float").WhoseValue.Should().Be("float");
        defaults.Should().ContainKey("boolean").WhoseValue.Should().Be("bool");
    }

    [Fact]
    public void GetCSharpType_WithCustomTypeMappings_UsesCustomMapping()
    {
        var customMappings = new Dictionary<string, string>
        {
            ["string:date-time"] = "DateTimeOffset"
        };
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths()
        };
        var generator = new ClientGenerator(document, "TestNamespace", Path.GetTempPath(), typeMappingConfig: new TypeMappingConfig(customMappings));

        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
        var result = generator.GetCSharpType(schema);

        result.Should().Be("DateTimeOffset");
    }

    [Fact]
    public void GetCSharpType_WithCustomTypeMappings_DefaultsStillWork()
    {
        var customMappings = new Dictionary<string, string>
        {
            ["string:date-time"] = "DateTimeOffset"
        };
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo { Title = "Test", Version = "1.0" },
            Paths = new OpenApiPaths()
        };
        var generator = new ClientGenerator(document, "TestNamespace", Path.GetTempPath(), typeMappingConfig: new TypeMappingConfig(customMappings));

        // Non-overridden mapping should still work
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
        var result = generator.GetCSharpType(schema);

        result.Should().Be("Guid");
    }
}
