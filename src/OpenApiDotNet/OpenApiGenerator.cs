using Microsoft.OpenApi;
using OpenApiDotNet.Generators;
using System.Text;

namespace OpenApiDotNet;

/// <summary>
/// Generates C# client code from OpenAPI specifications
/// </summary>
public class OpenApiGenerator
{
    private readonly OpenApiDocument _document;
    private readonly string _namespace;
    private readonly string _outputDirectory;
    private readonly string? _namespacePrefix;
    private readonly string _clientName;
    private readonly TypeMappingConfig _typeMappingConfig;

    public OpenApiGenerator(OpenApiDocument document, string namespaceName, string outputDirectory, string? namespacePrefix = null, string? clientName = null, TypeMappingConfig? typeMappingConfig = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _namespace = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
        _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
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
    public void Generate()
    {
        Directory.CreateDirectory(_outputDirectory);
        var context = new GeneratorContext(_namespace, _clientName, _namespacePrefix, _typeMappingConfig);

        GenerateModels(context);
        GenerateIOpenApiBuilderInterface();
        GenerateIOpenApiClientInterface();

        var endClient = new ClientGenerator(_document, context);

        // Write named client interface
        WriteGeneratorToFile(endClient, GetOutputFilePath(endClient.TypeInfo));
        Console.WriteLine($"  Generated {endClient.TypeInfo.Name} interface");

        // Write builders
        foreach (var builder in endClient.BuilderGenerators)
        {
            WriteGeneratorToFile(builder, GetOutputFilePath(builder.TypeInfo));
            Console.WriteLine($"  Generated builder: {builder.TypeInfo.Name}");
        }
    }

    private void GenerateModels(GeneratorContext context)
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

            WriteGeneratorToFile(generator, GetOutputFilePath(generator.TypeInfo));
            Console.WriteLine($"  Generated model: {name}");
        }
    }
    private string GetOutputFilePath(GeneratedTypeInfo typeInfo)
    {
        var relativePath = typeInfo.FullName.Replace('.', Path.DirectorySeparatorChar) + ".cs";
        var nsPrefix = _namespace.Replace('.', Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (relativePath.StartsWith(nsPrefix))
            relativePath = relativePath[nsPrefix.Length..];

        return Path.Combine(_outputDirectory, relativePath);
    }

    private static void WriteGeneratorToFile(BaseGenerator generator, string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        var writer = new CodeWriter();
        generator.WriteWithNamespace(writer);
        File.WriteAllText(filePath, writer.ToString());
    }

    private void GenerateIOpenApiBuilderInterface()
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

        var filePath = Path.Combine(_outputDirectory, "IOpenApiBuilder.cs");
        File.WriteAllText(filePath, builderInterface);
        Console.WriteLine("  Generated IOpenApiBuilder interface");
    }

    private void GenerateIOpenApiClientInterface()
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

        var filePath = Path.Combine(_outputDirectory, "IOpenApiClient.cs");
        File.WriteAllText(filePath, apiClinet);
        Console.WriteLine("  Generated IOpenApiClient interface");
    }
}
