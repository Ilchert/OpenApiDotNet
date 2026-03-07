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
        var context = new GeneratorContext(_namespace, _clientName, _namespacePrefix, _typeMappingConfig);

        GenerateModels(context);
        GenerateIOpenApiBuilderInterface();
        GenerateIOpenApiClientInterface();

        var endClient = new ClientGenerator(_document, context);

        // Write named client interface
        WriteGeneratorToFile(endClient, Path.Combine(_outputDirectory, $"{endClient.TypeInfo.Name}.cs"));
        Console.WriteLine($"  Generated {endClient.TypeInfo.Name} interface");

        // Write builders
        var buildersDirectory = Path.Combine(_outputDirectory, "Builders");
        foreach (var builder in endClient.BuilderGenerators)
        {
            WriteGeneratorToFile(builder, Path.Combine(buildersDirectory, $"{builder.TypeInfo.Name}.cs"));
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

            // trim namespace prefix from model file path if specified and present in the model's namespace
            var modelFile = generator.TypeInfo.FullName.Replace('.', Path.DirectorySeparatorChar) + ".cs";
            if (modelFile.StartsWith(_namespace + Path.DirectorySeparatorChar))
                modelFile = modelFile[(_namespace.Length + 1)..];

            WriteGeneratorToFile(generator, Path.Combine(_outputDirectory, modelFile));
            Console.WriteLine($"  Generated model: {name}");
        }
    }
    private static void WriteGeneratorToFile(BaseGenerator generator, string filePath)
    {
        Directory.CreateDirectory(Path.GetFullPath(filePath));
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
