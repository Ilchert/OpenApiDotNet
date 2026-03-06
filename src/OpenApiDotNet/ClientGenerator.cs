using Microsoft.OpenApi;
using OpenApiDotNet.Generators;
using System.Text;

namespace OpenApiDotNet;

/// <summary>
/// Generates C# client code from OpenAPI specifications
/// </summary>
public class ClientGenerator
{
    private readonly OpenApiDocument _document;
    private readonly string _namespace;
    private readonly string _outputDirectory;
    private readonly string? _namespacePrefix;
    private readonly string? _clientName;
    private readonly TypeMappingConfig _typeMappingConfig;

    public ClientGenerator(OpenApiDocument document, string namespaceName, string outputDirectory, string? namespacePrefix = null, string? clientName = null, TypeMappingConfig? typeMappingConfig = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _namespace = namespaceName ?? throw new ArgumentNullException(nameof(namespaceName));
        _outputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
        _namespacePrefix = namespacePrefix;
        _clientName = clientName;
        _typeMappingConfig = typeMappingConfig ?? new TypeMappingConfig();
    }

    /// <summary>
    /// Generates all client code including models and builder classes
    /// </summary>
    public void Generate()
    {
        var clientName = GetClientName();
        var context = new GeneratorContext(_namespace, clientName, _namespacePrefix, _typeMappingConfig);

        GenerateModels(context);
        GenerateIOpenApiBuilderInterface();
        GenerateIOpenApiClientInterface();

        var endClient = new EndClientGenerator(_document, context);

        // Write named client interface
        WriteGeneratorToFile(endClient, _namespace, Path.Combine(_outputDirectory, $"{endClient.InterfaceName}.cs"));
        Console.WriteLine($"  Generated {endClient.InterfaceName} interface");

        // Write builders
        var buildersDirectory = Path.Combine(_outputDirectory, "Builders");
        Directory.CreateDirectory(buildersDirectory);
        foreach (var builder in endClient.BuilderGenerators)
        {
            WriteBuilderToFile(builder, Path.Combine(buildersDirectory, $"{builder.BuilderName}.cs"));
            Console.WriteLine($"  Generated builder: {builder.BuilderName}");
        }
    }

    private void GenerateModels(GeneratorContext context)
    {
        var modelsDirectory = Path.Combine(_outputDirectory, "Models");
        Directory.CreateDirectory(modelsDirectory);

        if (_document.Components?.Schemas == null)
            return;

        foreach (var (name, schema) in _document.Components.Schemas)
        {
            BaseGenerator generator;
            if (schema.Enum != null && schema.Enum.Count > 0)
                generator = new EnumGenerator(name, schema, context);
            else
                generator = new ObjectGenerator(name, schema, context);

            var (ns, typeName) = context.GetNameAndNamespace(name, GeneratorCategory.Model);
            var modelsNs = $"{_namespace}.Models";
            var relativePath = ns.Length > modelsNs.Length
                ? ns[(modelsNs.Length + 1)..].Replace('.', Path.DirectorySeparatorChar)
                : "";
            var dir = string.IsNullOrEmpty(relativePath) ? modelsDirectory : Path.Combine(modelsDirectory, relativePath);
            Directory.CreateDirectory(dir);

            WriteGeneratorToFile(generator, ns, Path.Combine(dir, $"{typeName}.cs"));
            Console.WriteLine($"  Generated model: {name}");
        }
    }

    private void GenerateIOpenApiBuilderInterface()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Base interface for all fluent API builders");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public interface IOpenApiBuilder");
        sb.AppendLine("{");
        sb.AppendLine("    IOpenApiClient Client { get; }");
        sb.AppendLine("    string GetPath();");
        sb.AppendLine("}");

        var filePath = Path.Combine(_outputDirectory, "IOpenApiBuilder.cs");
        File.WriteAllText(filePath, sb.ToString());
        Console.WriteLine("  Generated IOpenApiBuilder interface");
    }

    private void GenerateIOpenApiClientInterface()
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.Net.Http;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine();
        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Base interface for all OpenAPI clients");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public interface IOpenApiClient : IOpenApiBuilder");
        sb.AppendLine("{");
        sb.AppendLine("    HttpClient HttpClient { get; }");
        sb.AppendLine("    JsonSerializerOptions JsonOptions { get; }");
        sb.AppendLine();
        sb.AppendLine($"    IOpenApiClient IOpenApiBuilder.Client => this;");
        sb.AppendLine($"    string IOpenApiBuilder.GetPath() => \"\";");
        sb.AppendLine("}");

        var filePath = Path.Combine(_outputDirectory, "IOpenApiClient.cs");
        File.WriteAllText(filePath, sb.ToString());
        Console.WriteLine("  Generated IOpenApiClient interface");
    }

    private static void WriteGeneratorToFile(BaseGenerator generator, string ns, string filePath)
    {
        var writer = new CodeWriter();
        writer.WriteLine($"namespace {ns};");
        writer.WriteLine();
        generator.Write(writer);
        File.WriteAllText(filePath, writer.ToString());
    }

    private void WriteBuilderToFile(BuilderGenerator builder, string filePath)
    {
        var writer = new CodeWriter();
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine("using System.Net.Http;");
        writer.WriteLine("using System.Net.Http.Json;");
        writer.WriteLine("using System.Text.Json;");
        writer.WriteLine("using System.Text.Json.Serialization;");
        writer.WriteLine("using System.Threading;");
        writer.WriteLine("using System.Threading.Tasks;");
        writer.WriteLine();
        writer.WriteLine($"namespace {_namespace};");
        writer.WriteLine();
        builder.Write(writer);
        File.WriteAllText(filePath, writer.ToString());
    }

    private string GetClientName()
    {
        if (!string.IsNullOrEmpty(_clientName))
            return _clientName;

        var title = _document.Info?.Title?.Replace(" ", "").Replace("-", "").Replace("_", "") ?? "Api";
        return $"{title}Client";
    }
}
