using Microsoft.OpenApi;

namespace OpenApiDotNet;

/// <summary>
/// Configuration for OpenAPI type to .NET type mappings.
/// Mappings use keys in the format "type:format" (e.g. "string:date-time") or just "type" for defaults (e.g. "string").
/// </summary>
public class TypeMappingConfig
{
    private readonly Dictionary<string, string> _mappings;

    public TypeMappingConfig(Dictionary<string, string>? customMappings = null)
    {
        _mappings = GetDefaults();

        if (customMappings != null)
        {
            foreach (var mapping in customMappings)
            {
                _mappings[mapping.Key] = mapping.Value;
            }
        }
    }

    /// <summary>
    /// Resolves the C# type for a given OpenAPI schema type and optional format.
    /// Returns null if no mapping is found (caller should handle special cases like arrays and references).
    /// </summary>
    public string? Resolve(JsonSchemaType? schemaType, string? format)
    {
        if (schemaType == null)
            return null;

        var typeName = SchemaTypeToString(schemaType.Value);
        if (typeName == null)
            return null;

        // Try type:format first
        if (!string.IsNullOrEmpty(format) && _mappings.TryGetValue($"{typeName}:{format}", out var formatted))
            return formatted;

        // Fall back to type default
        if (_mappings.TryGetValue(typeName, out var defaultType))
            return defaultType;

        return null;
    }

    private static string? SchemaTypeToString(JsonSchemaType type) => type switch
    {
        JsonSchemaType.String => "string",
        JsonSchemaType.Integer => "integer",
        JsonSchemaType.Number => "number",
        JsonSchemaType.Boolean => "boolean",
        _ => null
    };

    /// <summary>
    /// Returns the default OpenAPI to .NET type mappings.
    /// </summary>
    public static Dictionary<string, string> GetDefaults() => new()
    {
        // String types
        ["string"] = "string",
        ["string:date-time"] = "NodaTime.Instant",
        ["string:date"] = "NodaTime.LocalDate",
        ["string:time"] = "NodaTime.LocalTime",
        ["string:time-local"] = "NodaTime.LocalTime",
        ["string:date-time-local"] = "NodaTime.LocalDateTime",
        ["string:duration"] = "NodaTime.Duration",
        ["string:uuid"] = "System.Guid",
        ["string:uri"] = "System.Uri",
        ["string:uri-reference"] = "System.Uri",
        ["string:iri"] = "System.Uri",
        ["string:iri-reference"] = "System.Uri",
        ["string:byte"] = "byte[]",
        ["string:binary"] = "byte[]",
        ["string:base64url"] = "byte[]",
        ["string:char"] = "char",

        // Integer types
        ["integer"] = "int",
        ["integer:int64"] = "long",
        ["integer:int32"] = "int",
        ["integer:int16"] = "short",
        ["integer:int8"] = "sbyte",
        ["integer:uint8"] = "byte",
        ["integer:uint16"] = "ushort",
        ["integer:uint32"] = "uint",
        ["integer:uint64"] = "ulong",

        // Number types
        ["number"] = "double",
        ["number:float"] = "float",
        ["number:double"] = "double",
        ["number:decimal"] = "decimal",
        ["number:decimal128"] = "decimal",
        ["number:double-int"] = "long",

        // Boolean
        ["boolean"] = "bool",
    };
}
