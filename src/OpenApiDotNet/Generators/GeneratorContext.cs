using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class GeneratorContext
{
    public string DefaultNamespace { get; }
    public string? StripNamespacePefix { get; }
    public TypeMappingConfig TypeMappingConfig { get; }

    private static readonly char[] s_namespaceSeparators = ['.'];

    public GeneratorContext(string defaultNamespace, string? stripNamespacePefix, TypeMappingConfig? typeMappingConfig = null)
    {
        DefaultNamespace = defaultNamespace;
        TypeMappingConfig = typeMappingConfig ?? new TypeMappingConfig();
        if (stripNamespacePefix != null)
            StripNamespacePefix = stripNamespacePefix.EndsWith('.') ? stripNamespacePefix : stripNamespacePefix + ".";
        else
            StripNamespacePefix = null;
    }

    public (string Namespace, string Name) GetNameAndNamespace(string name, GeneratorCategory category)
    {
        name = GetStrippedName(name);
        IEnumerable<string> namespaceSegments = [DefaultNamespace];
        if (category == GeneratorCategory.Model)
            namespaceSegments = namespaceSegments.Append("Model");
        else if (category == GeneratorCategory.Builder)
            namespaceSegments = namespaceSegments.Append("Builder");

        var typeName = name;

        var dotIndex = name.LastIndexOfAny(s_namespaceSeparators);
        if (dotIndex >= 0)
        {
            var namespacePart = name[..dotIndex];
            typeName = name[(dotIndex + 1)..];
            var segments = namespacePart.Split(s_namespaceSeparators, StringSplitOptions.RemoveEmptyEntries);
            namespaceSegments = namespaceSegments.Concat(segments.Select(ToPascalCase));
        }

        return (string.Join(".", namespaceSegments), typeName);
    }

    private string GetStrippedName(string name)
    {
        if (string.IsNullOrEmpty(StripNamespacePefix))
            return name;

        return name.StartsWith(StripNamespacePefix, StringComparison.Ordinal)
            ? name[StripNamespacePefix.Length..]
            : name;
    }

    public static string ToPascalCase(string input)
    {
        var words = input.Split(['-', '_', ' ', '.'], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }

    public string GetCSharpType(IOpenApiSchema schema)
    {
        var schemaName = GetSchemaName(schema);
        if (schemaName != null)
        {
            var (ns, name) = GetNameAndNamespace(schemaName, GeneratorCategory.Model);
            return $"{ns}.{name}";
        }

        var resolved = TypeMappingConfig.Resolve(schema.Type, schema.Format);
        if (resolved != null)
            return resolved;

        return schema.Type switch
        {
            JsonSchemaType.Array when schema.Items != null => $"List<{GetCSharpType(schema.Items)}>",
            JsonSchemaType.Array => "List<object>",
            _ => "object"
        };
    }

    public static string? GetSchemaName(IOpenApiSchema schema)
    {
        if (!string.IsNullOrEmpty(schema.Id))
            return schema.Id;

        if (schema is OpenApiSchemaReference schemaRef)
            return schemaRef.Reference.Id;

        return null;
    }

    public static bool IsInlineObjectSchema(IOpenApiSchema? schema)
    {
        if (schema == null) return false;
        if (GetSchemaName(schema) != null) return false;
        return schema.Type == JsonSchemaType.Object && schema.Properties?.Count > 0;
    }
}
