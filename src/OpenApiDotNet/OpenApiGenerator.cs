using Microsoft.OpenApi;
using OpenApiDotNet.Generators;
using OpenApiDotNet.IO;

namespace OpenApiDotNet;

/// <summary>
/// Generates C# client code from OpenAPI specifications
/// </summary>
internal class OpenApiGenerator
{
    private readonly OpenApiDocument _document;
    private readonly string _namespace;
    private readonly IWritableFileProvider _output;
    private readonly string? _namespacePrefix;
    private readonly string _clientName;
    private readonly TypeMappingConfig _typeMappingConfig;

    public OpenApiGenerator(OpenApiDocument document, string namespaceName, IWritableFileProvider output, string? namespacePrefix = null, string? clientName = null, TypeMappingConfig? typeMappingConfig = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _namespace = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _namespacePrefix = namespacePrefix;
        _clientName = clientName ?? GetDefaultClientName(document);
        _typeMappingConfig = typeMappingConfig ?? new TypeMappingConfig();
    }

    private static string GetDefaultClientName(OpenApiDocument document)
    {
        var title = document.Info?.Title?.Replace(" ", "").Replace("-", "").Replace("_", "") ?? "Api";
        return $"{title}Client";
    }

    /// <summary>
    /// Generates all client code including models and builder classes
    /// </summary>
    /// <returns>A list of relative paths (relative to the output directory) of all generated files</returns>
    public List<string> Generate()
    {
        var context = new GeneratorContext(_namespace, _clientName, _namespacePrefix, _typeMappingConfig);
        var generators = CollectGenerators(context);

        var generatedFiles = new List<string>();
        foreach (var generator in generators)
        {
            var relativePath = GetRelativePath(generator.TypeInfo);
            WriteGeneratorToFile(generator, relativePath);
            generatedFiles.Add(relativePath);
        }

        return generatedFiles;
    }

    private List<BaseGenerator> CollectGenerators(GeneratorContext context)
    {
        var generators = new List<BaseGenerator>();

        CollectModelGenerators(context, generators);

        generators.Add(new OpenApiBuilderInterfaceGenerator(context));
        generators.Add(new OpenApiClientInterfaceGenerator(context));

        var clientGenerator = new ClientGenerator(_document, context);
        generators.Add(clientGenerator);

        foreach (var node in PathTreeBuilder.GetAllNodes(clientGenerator.PathTreeRoot))
            generators.Add(new BuilderGenerator(node, context));

        return generators;
    }

    private void CollectModelGenerators(GeneratorContext context, List<BaseGenerator> generators)
    {
        if (_document.Components?.Schemas == null)
            return;

        foreach (var (name, schema) in _document.Components.Schemas)
        {
            if (schema.Enum?.Count > 0)
                generators.Add(new EnumGenerator(name, schema, context));
            else
                generators.Add(new ObjectGenerator(name, schema, context));
        }
    }

    private string GetRelativePath(GeneratedTypeInfo typeInfo)
    {
        var relativePath = typeInfo.FullName.Replace('.', Path.DirectorySeparatorChar) + ".cs";
        var nsPrefix = _namespace.Replace('.', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (relativePath.StartsWith(nsPrefix))
            relativePath = relativePath[nsPrefix.Length..];

        return relativePath;
    }

    private void WriteGeneratorToFile(BaseGenerator generator, string relativePath)
    {
        var writer = new CodeWriter();
        generator.WriteWithNamespace(writer);
        _output.GetFileInfo(relativePath).WriteAllTextIfChanged(writer.ToString());
    }
}
