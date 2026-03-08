using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal record GeneratorContext(
    string DefaultNamespace,
    string ClientName,
    string? StripNamespacePrefix,
    TypeMappingConfig? TypeMappingConfig = null)
{
    private static readonly char[] s_namespaceSeparators = ['.'];

    public string? StripNamespacePrefix { get; } = StripNamespacePrefix switch
    {
        null => null,
        _ when StripNamespacePrefix.EndsWith('.') => StripNamespacePrefix,
        _ => StripNamespacePrefix + "."
    };

    public TypeMappingConfig TypeMappingConfig { get; } = TypeMappingConfig ?? new TypeMappingConfig();

    public GeneratedTypeInfo GetNameAndNamespace(string name, GeneratorCategory category)
    {
        name = GetStrippedName(name);
        IEnumerable<string> namespaceSegments = [DefaultNamespace];
        if (category == GeneratorCategory.Model)
            namespaceSegments = namespaceSegments.Append("Models");
        else if (category == GeneratorCategory.Builder)
            namespaceSegments = namespaceSegments.Append("Builders");

        var typeName = name;

        var dotIndex = name.LastIndexOfAny(s_namespaceSeparators);
        if (dotIndex >= 0)
        {
            var namespacePart = name[..dotIndex];
            typeName = name[(dotIndex + 1)..];
            var segments = namespacePart.Split(s_namespaceSeparators, StringSplitOptions.RemoveEmptyEntries);
            namespaceSegments = namespaceSegments.Concat(segments.Select(NamingConventions.ToPascalCase));
        }

        return new GeneratedTypeInfo(string.Join(".", namespaceSegments), typeName);
    }

    private string GetStrippedName(string name)
    {
        if (string.IsNullOrEmpty(StripNamespacePrefix))
            return name;

        return name.StartsWith(StripNamespacePrefix, StringComparison.Ordinal)
            ? name[StripNamespacePrefix.Length..]
            : name;
    }

    public GeneratedTypeInfo GetCSharpType(IOpenApiSchema schema)
    {
        var schemaName = schema.GetSchemaName();
        if (schemaName != null)
            return GetNameAndNamespace(schemaName, GeneratorCategory.Model);

        var resolved = TypeMappingConfig.Resolve(schema.Type, schema.Format);
        if (resolved != null)
            return GeneratedTypeInfo.FromFullyQualified(resolved);

        return schema.Type switch
        {
            JsonSchemaType.Array when schema.Items != null => new GeneratedTypeInfo("System.Collections.Generic", $"List<{GetCSharpType(schema.Items).FullName}>"),
            JsonSchemaType.Array => new GeneratedTypeInfo("System.Collections.Generic", "List<object>"),
            _ => new GeneratedTypeInfo(string.Empty, "object")
        };
    }
}
