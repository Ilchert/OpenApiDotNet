using Microsoft.OpenApi;
using OpenApiDotNet.Generators;
using OpenApiDotNet.IO;
using System.Text;

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
        var generatedFiles = new List<string>();

        GenerateModels(context, generatedFiles);
        GenerateIOpenApiBuilderInterface(generatedFiles);
        GenerateIOpenApiClientInterface(generatedFiles);

        var endClient = new ClientGenerator(_document, context);

        // Write named client interface
        var clientRelativePath = GetRelativePath(endClient.TypeInfo);
        WriteGeneratorToFile(endClient, clientRelativePath);
        generatedFiles.Add(clientRelativePath);
        Console.WriteLine($"  Generated {endClient.TypeInfo.Name} interface");

        // Write builders
        foreach (var builder in endClient.BuilderGenerators)
        {
            var builderRelativePath = GetRelativePath(builder.TypeInfo);
            WriteGeneratorToFile(builder, builderRelativePath);
            generatedFiles.Add(builderRelativePath);
            Console.WriteLine($"  Generated builder: {builder.TypeInfo.Name}");
        }

        return generatedFiles;
    }

    private void GenerateModels(GeneratorContext context, List<string> generatedFiles)
    {
        if (_document.Components?.Schemas == null)
            return;

        foreach (var (name, schema) in _document.Components.Schemas)
        {
            BaseGenerator generator;
            if (schema.Enum?.Count > 0)
                generator = new EnumGenerator(name, schema, context);
            else
                generator = new ObjectGenerator(name, schema, context);

            var relativePath = GetRelativePath(generator.TypeInfo);
            WriteGeneratorToFile(generator, relativePath);
            generatedFiles.Add(relativePath);
            Console.WriteLine($"  Generated model: {name}");
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

    private void GenerateIOpenApiBuilderInterface(List<string> generatedFiles)
    {
        var builderInterface = $$"""
namespace {{_namespace}};
/// <summary>
/// Base interface for all fluent API builders
/// /// </summary>
public interface IOpenApiBuilder
{
    IOpenApiClient Client { get; }
    string GetPath();
}
""";

        var relativePath = "IOpenApiBuilder.cs";
        _output.GetFileInfo(relativePath).WriteAllTextIfChanged(builderInterface);
        generatedFiles.Add(relativePath);
        Console.WriteLine("  Generated IOpenApiBuilder interface");
    }

    private void GenerateIOpenApiClientInterface(List<string> generatedFiles)
    {
        var apiClinet = $$"""
namespace {{_namespace}};
/// <summary>
/// Base interface for all OpenAPI clients
/// </summary>
public interface IOpenApiClient : IOpenApiBuilder
{
    System.Net.Http.HttpClient HttpClient { get; }
    System.Text.Json.JsonSerializerOptions JsonOptions { get; }
    IOpenApiClient IOpenApiBuilder.Client => this;
    string IOpenApiBuilder.GetPath() => "";
}
""";

        var relativePath = "IOpenApiClient.cs";
        _output.GetFileInfo(relativePath).WriteAllTextIfChanged(apiClinet);
        generatedFiles.Add(relativePath);
        Console.WriteLine("  Generated IOpenApiClient interface");
    }
}
