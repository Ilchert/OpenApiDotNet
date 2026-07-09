using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal static class OpenApiSchemaExtensions
{
    public static string? GetSchemaName(this IOpenApiSchema schema)
    {
        if (!string.IsNullOrEmpty(schema.Id))
            return schema.Id;

        if (schema is OpenApiSchemaReference schemaRef)
            return schemaRef.Reference.Id;

        return null;
    }

    public static bool IsNullableSchema(this IOpenApiSchema? schema)
    {
        if (schema == null) return false;
        if (schema.Type.HasValue && schema.Type.Value.HasFlag(JsonSchemaType.Null))
            return true;
        // oneOf: [X, {type: null}] pattern
        if (schema.OneOf is { Count: 2 } oneOf)
            return oneOf.Any(s => s.Type.HasValue && s.Type.Value == JsonSchemaType.Null);
        return false;
    }

    /// <summary>
    /// For oneOf: [X, {type: null}] patterns, returns the non-null schema.
    /// Otherwise returns the schema itself.
    /// </summary>
    public static IOpenApiSchema UnwrapNullableOneOf(this IOpenApiSchema schema)
    {
        if (schema.OneOf is { Count: 2 } oneOf)
        {
            var nonNull = oneOf.FirstOrDefault(s => !(s.Type.HasValue && s.Type.Value == JsonSchemaType.Null));
            if (nonNull != null)
                return nonNull;
        }
        return schema;
    }

    public static bool IsInlineObjectSchema(this IOpenApiSchema? schema)
    {
        if (schema == null) return false;
        if (schema.GetSchemaName() != null) return false;
        return schema.Type.HasValue && schema.Type.Value.HasFlag(JsonSchemaType.Object) && schema.Properties?.Count > 0;
    }
}
