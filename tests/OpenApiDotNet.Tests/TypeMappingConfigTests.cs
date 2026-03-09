using Microsoft.OpenApi;
using OpenApiDotNet;
using OpenApiDotNet.Generators;

namespace OpenApiDotNet.Tests;

public class TypeMappingConfigTests
{
    [Fact]
    public void Resolve_DefaultStringType_ReturnsString()
    {
        var config = new TypeMappingConfig();

        var result = config.Resolve(JsonSchemaType.String, null);

        Assert.Equal("string", result);
    }

    [Fact]
    public void Resolve_DefaultStringWithDateTimeFormat_ReturnsInstant()
    {
        var config = new TypeMappingConfig();

        var result = config.Resolve(JsonSchemaType.String, "date-time");

        Assert.Equal("NodaTime.Instant", result);
    }

    [Fact]
    public void Resolve_DefaultIntegerType_ReturnsInt()
    {
        var config = new TypeMappingConfig();

        var result = config.Resolve(JsonSchemaType.Integer, null);

        Assert.Equal("int", result);
    }

    [Fact]
    public void Resolve_DefaultBooleanType_ReturnsBool()
    {
        var config = new TypeMappingConfig();

        var result = config.Resolve(JsonSchemaType.Boolean, null);

        Assert.Equal("bool", result);
    }

    [Fact]
    public void Resolve_ArrayType_ReturnsNull()
    {
        var config = new TypeMappingConfig();

        var result = config.Resolve(JsonSchemaType.Array, null);

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_NullType_ReturnsNull()
    {
        var config = new TypeMappingConfig();

        var result = config.Resolve(null, null);

        Assert.Null(result);
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

        Assert.Equal("DateTimeOffset", result);
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

        Assert.Equal("long", result);
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

        Assert.Equal("EmailAddress", result);
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
        Assert.Equal("NodaTime.LocalDate", config.Resolve(JsonSchemaType.String, "date"));
        Assert.Equal("System.Guid", config.Resolve(JsonSchemaType.String, "uuid"));
        Assert.Equal("string", config.Resolve(JsonSchemaType.String, null));
    }

    [Fact]
    public void GetDefaults_ReturnsAllExpectedMappings()
    {
        var defaults = TypeMappingConfig.GetDefaults();

        Assert.True(defaults.ContainsKey("string"));
        Assert.Equal("string", defaults["string"]);
        Assert.True(defaults.ContainsKey("string:date-time"));
        Assert.Equal("NodaTime.Instant", defaults["string:date-time"]);
        Assert.True(defaults.ContainsKey("string:uuid"));
        Assert.Equal("System.Guid", defaults["string:uuid"]);
        Assert.True(defaults.ContainsKey("integer"));
        Assert.Equal("int", defaults["integer"]);
        Assert.True(defaults.ContainsKey("integer:int64"));
        Assert.Equal("long", defaults["integer:int64"]);
        Assert.True(defaults.ContainsKey("number"));
        Assert.Equal("double", defaults["number"]);
        Assert.True(defaults.ContainsKey("number:float"));
        Assert.Equal("float", defaults["number:float"]);
        Assert.True(defaults.ContainsKey("boolean"));
        Assert.Equal("bool", defaults["boolean"]);
    }

    [Fact]
    public void GetCSharpType_WithCustomTypeMappings_UsesCustomMapping()
    {
        var customMappings = new Dictionary<string, string>
        {
            ["string:date-time"] = "DateTimeOffset"
        };
        var context = new GeneratorContext("TestNamespace", "TestClient", null, new TypeMappingConfig(customMappings));

        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
        var result = context.GetCSharpType(schema);

        Assert.Equal("DateTimeOffset", result.FullName);
    }

    [Fact]
    public void GetCSharpType_WithCustomTypeMappings_DefaultsStillWork()
    {
        var customMappings = new Dictionary<string, string>
        {
            ["string:date-time"] = "DateTimeOffset"
        };
        var context = new GeneratorContext("TestNamespace", "TestClient", null, new TypeMappingConfig(customMappings));

        // Non-overridden mapping should still work
        var schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
        var result = context.GetCSharpType(schema);

        Assert.Equal("System.Guid", result.FullName);
    }
}
