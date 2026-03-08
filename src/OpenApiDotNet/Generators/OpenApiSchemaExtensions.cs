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

    public static bool IsInlineObjectSchema(this IOpenApiSchema? schema)
    {
        if (schema == null) return false;
        if (schema.GetSchemaName() != null) return false;
        return schema.Type == JsonSchemaType.Object && schema.Properties?.Count > 0;
    }
}
