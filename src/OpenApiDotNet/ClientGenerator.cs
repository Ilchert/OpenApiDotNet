using Microsoft.OpenApi;
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
    private readonly HashSet<string> _generatedModels = new();
    private readonly HashSet<string> _subNamespaces = new();
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
    /// Generates all client code including models, builder classes, and JSON configuration
    /// </summary>
    public void Generate()
    {
        GenerateModels();

        var pathTree = PathTreeBuilder.Build(_document.Paths);
        GenerateIBuilderInterface();
        GenerateIClientInterface(pathTree);
        GenerateBuilders(pathTree);

        GenerateJsonConfiguration();
    }

    private void GenerateModels()
    {
        var modelsDirectory = Path.Combine(_outputDirectory, "Models");
        Directory.CreateDirectory(modelsDirectory);

        if (_document.Components?.Schemas == null)
            return;

        // Discover all sub-namespaces first
        foreach (var schema in _document.Components.Schemas)
        {
            var (additionalNamespace, _) = DecomposeName(StripNamespacePrefix(schema.Key));
            if (!string.IsNullOrEmpty(additionalNamespace))
                _subNamespaces.Add(additionalNamespace);
        }

        foreach (var schema in _document.Components.Schemas)
        {
            GenerateModel(schema.Key, schema.Value, modelsDirectory);
        }
    }

    private void GenerateModel(string name, IOpenApiSchema schema, string directory)
    {
        if (_generatedModels.Contains(name))
            return;

        _generatedModels.Add(name);

        // Detect enum schemas and generate enum instead of class
        if (schema.Enum != null && schema.Enum.Count > 0)
        {
            GenerateEnum(name, schema, directory);
            return;
        }

        var (additionalNamespace, typeName) = DecomposeName(StripNamespacePrefix(name));
        var fullNamespace = string.IsNullOrEmpty(additionalNamespace)
            ? $"{_namespace}.Models"
            : $"{_namespace}.Models.{additionalNamespace}";

        var sb = new StringBuilder();
        sb.AppendLine("using System.Text.Json.Serialization;");

        // Add using statements for all model sub-namespaces
        foreach (var subNs in _subNamespaces.OrderBy(s => s))
        {
            var fullSubNamespace = $"{_namespace}.Models.{subNs}";
            if (fullSubNamespace != fullNamespace)
                sb.AppendLine($"using {fullSubNamespace};");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {fullNamespace};");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(schema.Description))
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// {EscapeXmlComment(schema.Description)}");
            sb.AppendLine("/// </summary>");
        }

        sb.AppendLine($"public class {typeName}");
        sb.AppendLine("{");

        if (schema.Properties != null)
        {
            foreach (var property in schema.Properties)
            {
                var propertyName = ToPascalCase(property.Key);
                var propertyType = GetCSharpType(property.Value);
                var isRequired = schema.Required?.Contains(property.Key) ?? false;

                if (!string.IsNullOrEmpty(property.Value.Description))
                {
                    sb.AppendLine("    /// <summary>");
                    sb.AppendLine($"    /// {EscapeXmlComment(property.Value.Description)}");
                    sb.AppendLine("    /// </summary>");
                }

                sb.AppendLine($"    [JsonPropertyName(\"{property.Key}\")]");
                sb.AppendLine($"    public {(isRequired ? "required " : "")}{propertyType}{(isRequired ? "" : "?")} {propertyName} {{ get; set; }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        var targetDirectory = string.IsNullOrEmpty(additionalNamespace)
            ? directory
            : Path.Combine(directory, additionalNamespace.Replace('.', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(targetDirectory);

        var filePath = Path.Combine(targetDirectory, $"{typeName}.cs");
        File.WriteAllText(filePath, sb.ToString());
        Console.WriteLine($"  Generated model: {name}");
    }

    private void GenerateEnum(string name, IOpenApiSchema schema, string directory)
    {
        var (additionalNamespace, typeName) = DecomposeName(StripNamespacePrefix(name));
        var fullNamespace = string.IsNullOrEmpty(additionalNamespace)
            ? $"{_namespace}.Models"
            : $"{_namespace}.Models.{additionalNamespace}";

        var sb = new StringBuilder();
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();
        sb.AppendLine($"namespace {fullNamespace};");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(schema.Description))
        {
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// {EscapeXmlComment(schema.Description)}");
            sb.AppendLine("/// </summary>");
        }

        sb.AppendLine("[JsonConverter(typeof(JsonStringEnumConverter))]");
        sb.AppendLine($"public enum {typeName}");
        sb.AppendLine("{");

        foreach (var enumValue in schema.Enum)
        {
            var stringValue = enumValue.ToString();

            var memberName = ToPascalCase(stringValue);

            if (memberName != stringValue)
            {
                sb.AppendLine($"    [JsonStringEnumMemberName(\"{stringValue}\")]");
            }
            sb.AppendLine($"    {memberName},");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        var targetDirectory = string.IsNullOrEmpty(additionalNamespace)
            ? directory
            : Path.Combine(directory, additionalNamespace.Replace('.', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(targetDirectory);

        var filePath = Path.Combine(targetDirectory, $"{typeName}.cs");
        File.WriteAllText(filePath, sb.ToString());
        Console.WriteLine($"  Generated enum: {name}");
    }

    private void GenerateIBuilderInterface()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Base interface for all fluent API builders");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public interface IBuilder");
        sb.AppendLine("{");
        sb.AppendLine("    IClient Client { get; }");
        sb.AppendLine("    string GetPath();");
        sb.AppendLine("}");

        var filePath = Path.Combine(_outputDirectory, "IBuilder.cs");
        File.WriteAllText(filePath, sb.ToString());
        Console.WriteLine("  Generated IBuilder interface");
    }

    private void GenerateIClientInterface(PathSegmentNode pathTree)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine();
        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine();

        var clientName = GetClientName();

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// {EscapeXmlComment(_document.Info.Description ?? _document.Info.Title ?? "API Client")}");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public interface IClient : IBuilder");
        sb.AppendLine("{");
        sb.AppendLine("    HttpClient HttpClient { get; }");
        sb.AppendLine("    JsonSerializerOptions JsonOptions { get; }");

        // Add navigation properties for top-level static segments
        foreach (var (_, child) in pathTree.Children.OrderBy(c => c.Key))
        {
            if (!child.IsParameter)
            {
                sb.AppendLine($"    {child.BuilderName} {ToPascalCase(child.SegmentName)} {{ get => new(this); }}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"    IClient IBuilder.Client => this;");
        sb.AppendLine($"    string IBuilder.GetPath() => \"\";");
        sb.AppendLine("}");

        var filePath = Path.Combine(_outputDirectory, "IClient.cs");
        File.WriteAllText(filePath, sb.ToString());
        Console.WriteLine("  Generated IClient interface");
    }

    private void GenerateBuilders(PathSegmentNode root)
    {
        var buildersDirectory = Path.Combine(_outputDirectory, "Builders");
        Directory.CreateDirectory(buildersDirectory);

        foreach (var node in PathTreeBuilder.GetAllNodes(root))
        {
            GenerateBuilderClass(node, buildersDirectory);
        }
    }

    private void GenerateBuilderClass(PathSegmentNode node, string directory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.Net.Http.Json;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine($"using {_namespace}.Models;");

        foreach (var subNs in _subNamespaces.OrderBy(s => s))
        {
            sb.AppendLine($"using {_namespace}.Models.{subNs};");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine();

        var builderName = node.BuilderName;

        if (node.IsParameter)
        {
            GenerateParameterBuilderBody(sb, node, builderName);
        }
        else
        {
            GenerateStaticBuilderBody(sb, node, builderName);
        }

        var filePath = Path.Combine(directory, $"{builderName}.cs");
        File.WriteAllText(filePath, sb.ToString());
        Console.WriteLine($"  Generated builder: {builderName}");
    }

    private void GenerateStaticBuilderBody(StringBuilder sb, PathSegmentNode node, string builderName)
    {
        sb.AppendLine($"public class {builderName} : IBuilder");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IBuilder _parentBuilder;");
        sb.AppendLine();

        // Protected parameterless constructor for mocking
        sb.AppendLine("#pragma warning disable CS8618");
        sb.AppendLine($"    protected {builderName}() {{ }}");
        sb.AppendLine("#pragma warning restore CS8618");
        sb.AppendLine();

        sb.AppendLine($"    public {builderName}(IBuilder parentBuilder)");
        sb.AppendLine("    {");
        sb.AppendLine("        _parentBuilder = parentBuilder;");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    public IClient Client => _parentBuilder.Client;");
        sb.AppendLine($"    public string GetPath() => $\"{{_parentBuilder.GetPath()}}/{node.SegmentName}\";");
        sb.AppendLine();

        // Indexer for parameterized child
        foreach (var (_, child) in node.Children)
        {
            if (child.IsParameter)
            {
                var paramType = child.ParameterSchema != null ? GetCSharpType(child.ParameterSchema) : "string";
                var paramName = ToCamelCase(child.ParameterName ?? "id");
                sb.AppendLine($"    public virtual {child.BuilderName} this[{paramType} {paramName}]");
                sb.AppendLine("    {");
                sb.AppendLine($"        get => new(this, {paramName});");
                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        // Navigation properties for static children
        foreach (var (_, child) in node.Children.OrderBy(c => c.Key))
        {
            if (!child.IsParameter)
            {
                sb.AppendLine($"    public {child.BuilderName} {ToPascalCase(child.SegmentName)} => new(this);");
                sb.AppendLine();
            }
        }

        // Operations
        foreach (var (method, operation) in node.Operations)
        {
            GenerateBuilderOperation(sb, method, operation);
        }

        sb.AppendLine("}");
    }

    private void GenerateParameterBuilderBody(StringBuilder sb, PathSegmentNode node, string builderName)
    {
        var paramType = node.ParameterSchema != null ? GetCSharpType(node.ParameterSchema) : "string";
        var paramName = ToCamelCase(node.ParameterName ?? "id");
        var fieldName = $"_{paramName}";

        sb.AppendLine($"public class {builderName} : IBuilder");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IBuilder _parentBuilder;");
        sb.AppendLine($"    private readonly {paramType} {fieldName};");
        sb.AppendLine();

        // Protected parameterless constructor for mocking
        sb.AppendLine("#pragma warning disable CS8618");
        sb.AppendLine($"    protected {builderName}() {{ }}");
        sb.AppendLine("#pragma warning restore CS8618");
        sb.AppendLine();

        sb.AppendLine($"    public {builderName}(IBuilder parentBuilder, {paramType} {paramName})");
        sb.AppendLine("    {");
        sb.AppendLine("        _parentBuilder = parentBuilder;");
        sb.AppendLine($"        {fieldName} = {paramName};");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    public IClient Client => _parentBuilder.Client;");
        sb.AppendLine($"    public string GetPath() => $\"{{_parentBuilder.GetPath()}}/{{{fieldName}}}\";");
        sb.AppendLine();

        // Indexer for parameterized child
        foreach (var (_, child) in node.Children)
        {
            if (child.IsParameter)
            {
                var childParamType = child.ParameterSchema != null ? GetCSharpType(child.ParameterSchema) : "string";
                var childParamName = ToCamelCase(child.ParameterName ?? "id");
                sb.AppendLine($"    public virtual {child.BuilderName} this[{childParamType} {childParamName}]");
                sb.AppendLine("    {");
                sb.AppendLine($"        get => new(this, {childParamName});");
                sb.AppendLine("    }");
                sb.AppendLine();
            }
        }

        // Navigation properties for static children
        foreach (var (_, child) in node.Children.OrderBy(c => c.Key))
        {
            if (!child.IsParameter)
            {
                sb.AppendLine($"    public {child.BuilderName} {ToPascalCase(child.SegmentName)} => new(this);");
                sb.AppendLine();
            }
        }

        // Operations
        foreach (var (method, operation) in node.Operations)
        {
            GenerateBuilderOperation(sb, method, operation);
        }

        sb.AppendLine("}");
    }

    private void GenerateBuilderOperation(StringBuilder sb, HttpMethod httpMethod, OpenApiOperation operation)
    {
        var methodName = ToPascalCase($"{httpMethod}".ToLowerInvariant());

        var nestedClasses = new StringBuilder();

        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// {EscapeXmlComment(operation.Summary ?? operation.Description ?? methodName)}");
        sb.AppendLine("    /// </summary>");

        var requiredParameters = new List<string>();
        var optionalParameters = new List<string>();
        var queryParams = new List<(string name, string paramName, string paramType, bool required, bool isCollection)>();
        string? requestBodyType = null;

        // Only include query parameters (path params are handled by the builder chain)
        if (operation.Parameters != null)
        {
            foreach (var parameter in operation.Parameters)
            {
                if (parameter.In == ParameterLocation.Query)
                {
                    var paramName = ToCamelCase(parameter.Name);
                    var paramType = GetCSharpType(parameter.Schema);
                    var isRequired = parameter.Required;
                    var isCollection = paramType.StartsWith("List<");
                    if (isRequired)
                        requiredParameters.Add($"{paramType} {paramName}");
                    else
                        optionalParameters.Add($"{paramType}? {paramName} = default");
                    queryParams.Add((parameter.Name, paramName, paramType, isRequired, isCollection));
                }
            }
        }

        if (operation.RequestBody != null)
        {
            var content = operation.RequestBody.Content.FirstOrDefault();
            var bodySchemaName = content.Value.Schema != null ? GetSchemaName(content.Value.Schema) : null;
            if (bodySchemaName != null)
            {
                requestBodyType = GetTypeName(bodySchemaName);
                requiredParameters.Add($"{requestBodyType} request");
            }
            else if (IsInlineObjectSchema(content.Value?.Schema))
            {
                var nestedClassName = $"{methodName}Request";
                requestBodyType = nestedClassName;
                GenerateNestedClass(nestedClasses, nestedClassName, content.Value!.Schema!);
                requiredParameters.Add($"{requestBodyType} request");
            }
        }

        var responseType = GetResponseType(operation);
        var responseSchema = GetSuccessResponseSchema(operation);
        if (IsInlineObjectSchema(responseSchema))
        {
            var nestedClassName = $"{methodName}Response";
            responseType = nestedClassName;
            GenerateNestedClass(nestedClasses, nestedClassName, responseSchema!);
        }

        optionalParameters.Add("CancellationToken cancellationToken = default");

        var parameters = requiredParameters.Concat(optionalParameters).ToList();

        sb.AppendLine($"    public virtual async Task{(responseType == "void" ? "" : $"<{responseType}>")} {methodName}({string.Join(", ", parameters)})");
        sb.AppendLine("    {");

        // URL building: start from GetPath(), add query string if needed
        if (queryParams.Count > 0)
        {
            sb.AppendLine("        var url = GetPath();");
            sb.AppendLine();
            sb.AppendLine("        var queryString = new List<string>();");

            foreach (var param in queryParams)
            {
                if (param.isCollection)
                {
                    if (!param.required)
                    {
                        sb.AppendLine($"        if ({param.paramName} != null)");
                        sb.AppendLine($"            foreach (var item in {param.paramName})");
                        sb.AppendLine($"                queryString.Add($\"{param.name}={{Uri.EscapeDataString(item.ToString())}}\");");
                    }
                    else
                    {
                        sb.AppendLine($"        foreach (var item in {param.paramName})");
                        sb.AppendLine($"            queryString.Add($\"{param.name}={{Uri.EscapeDataString(item.ToString())}}\");");
                    }
                }
                else if (param.required)
                {
                    sb.AppendLine($"        queryString.Add($\"{param.name}={{Uri.EscapeDataString({param.paramName}.ToString())}}\");");
                }
                else
                {
                    sb.AppendLine($"        if ({param.paramName} != null)");
                    sb.AppendLine($"            queryString.Add($\"{param.name}={{Uri.EscapeDataString({param.paramName}.ToString())}}\");");
                }
            }

            sb.AppendLine("        if (queryString.Count > 0)");
            sb.AppendLine("            url += \"?\" + string.Join(\"&\", queryString);");
        }
        else
        {
            sb.AppendLine("        var url = GetPath();");
        }

        sb.AppendLine();

        // HTTP call
        GenerateBuilderHttpCall(sb, httpMethod, responseType, requestBodyType != null);

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.Append(nestedClasses);
    }

    private static void GenerateBuilderHttpCall(StringBuilder sb, HttpMethod operationType, string responseType, bool hasRequestBody)
    {
        if (operationType == HttpMethod.Get)
        {
            sb.AppendLine("        var response = await Client.HttpClient.GetAsync(url, cancellationToken);");
            sb.AppendLine("        response.EnsureSuccessStatusCode();");
            if (responseType != "void")
            {
                sb.AppendLine($"        return await response.Content.ReadFromJsonAsync<{responseType}>(Client.JsonOptions, cancellationToken) ?? throw new InvalidOperationException(\"Response was null\");");
            }
        }
        else if (operationType == HttpMethod.Post)
        {
            if (hasRequestBody)
            {
                sb.AppendLine("        var response = await Client.HttpClient.PostAsJsonAsync(url, request, Client.JsonOptions, cancellationToken);");
            }
            else
            {
                sb.AppendLine("        var response = await Client.HttpClient.PostAsync(url, null, cancellationToken);");
            }
            sb.AppendLine("        response.EnsureSuccessStatusCode();");
            if (responseType != "void")
            {
                sb.AppendLine($"        return await response.Content.ReadFromJsonAsync<{responseType}>(Client.JsonOptions, cancellationToken) ?? throw new InvalidOperationException(\"Response was null\");");
            }
        }
        else if (operationType == HttpMethod.Put)
        {
            if (hasRequestBody)
            {
                sb.AppendLine("        var response = await Client.HttpClient.PutAsJsonAsync(url, request, Client.JsonOptions, cancellationToken);");
            }
            else
            {
                sb.AppendLine("        var response = await Client.HttpClient.PutAsync(url, null, cancellationToken);");
            }
            sb.AppendLine("        response.EnsureSuccessStatusCode();");
            if (responseType != "void")
            {
                sb.AppendLine($"        return await response.Content.ReadFromJsonAsync<{responseType}>(Client.JsonOptions, cancellationToken) ?? throw new InvalidOperationException(\"Response was null\");");
            }
        }
        else if (operationType == HttpMethod.Delete)
        {
            sb.AppendLine("        var response = await Client.HttpClient.DeleteAsync(url, cancellationToken);");
            sb.AppendLine("        response.EnsureSuccessStatusCode();");
        }
        else if (operationType == HttpMethod.Patch)
        {
            if (hasRequestBody)
            {
                sb.AppendLine("        var content = JsonContent.Create(request, options: Client.JsonOptions);");
                sb.AppendLine("        var response = await Client.HttpClient.PatchAsync(url, content, cancellationToken);");
            }
            else
            {
                sb.AppendLine("        var response = await Client.HttpClient.PatchAsync(url, null, cancellationToken);");
            }
            sb.AppendLine("        response.EnsureSuccessStatusCode();");
            if (responseType != "void")
            {
                sb.AppendLine($"        return await response.Content.ReadFromJsonAsync<{responseType}>(Client.JsonOptions, cancellationToken) ?? throw new InvalidOperationException(\"Response was null\");");
            }
        }
    }

    private void GenerateJsonConfiguration()
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine("using NodaTime.Serialization.SystemTextJson;");
        sb.AppendLine();
        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// JSON serialization configuration with NodaTime support");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class JsonConfiguration");
        sb.AppendLine("{");
        sb.AppendLine("    public static JsonSerializerOptions CreateOptions()");
        sb.AppendLine("    {");
        sb.AppendLine("        var options = new JsonSerializerOptions");
        sb.AppendLine("        {");
        sb.AppendLine("            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,");
        sb.AppendLine("            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,");
        sb.AppendLine("            PropertyNameCaseInsensitive = true");
        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        // Add string enum converter for enum serialization");
        sb.AppendLine("        options.Converters.Add(new JsonStringEnumConverter());");
        sb.AppendLine();
        sb.AppendLine("        // Configure NodaTime converters for date/time types");
        sb.AppendLine("        options.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);");
        sb.AppendLine();
        sb.AppendLine("        return options;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        var filePath = Path.Combine(_outputDirectory, "JsonConfiguration.cs");
        File.WriteAllText(filePath, sb.ToString());
        Console.WriteLine("  Generated JSON configuration");
    }

    private string GetResponseType(OpenApiOperation operation)
    {
        var successResponse = operation.Responses.FirstOrDefault(r => r.Key.StartsWith("2"));
        if (successResponse.Value?.Content?.Any() == true)
        {
            var content = successResponse.Value.Content.FirstOrDefault();
            var respSchemaName = content.Value.Schema != null ? GetSchemaName(content.Value.Schema) : null;
            if (respSchemaName != null)
            {
                return GetTypeName(respSchemaName);
            }
            if (content.Value?.Schema != null)
            {
                return GetCSharpType(content.Value.Schema);
            }
        }
        return "void";
    }

    private static IOpenApiSchema? GetSuccessResponseSchema(OpenApiOperation operation)
    {
        var successResponse = operation.Responses.FirstOrDefault(r => r.Key.StartsWith("2"));
        if (successResponse.Value?.Content?.Any() == true)
        {
            return successResponse.Value.Content.FirstOrDefault().Value?.Schema;
        }
        return null;
    }

    private static bool IsInlineObjectSchema(IOpenApiSchema? schema)
    {
        if (schema == null) return false;
        if (GetSchemaName(schema) != null) return false;
        return schema.Type == JsonSchemaType.Object && schema.Properties?.Count > 0;
    }

    private void GenerateNestedClass(StringBuilder sb, string className, IOpenApiSchema schema)
    {
        sb.AppendLine($"    public class {className}");
        sb.AppendLine("    {");

        if (schema.Properties != null)
        {
            foreach (var property in schema.Properties)
            {
                var propertyName = ToPascalCase(property.Key);
                var propertyType = GetCSharpType(property.Value);
                var isRequired = schema.Required?.Contains(property.Key) ?? false;

                sb.AppendLine($"        [JsonPropertyName(\"{property.Key}\")]");
                sb.AppendLine($"        public {(isRequired ? "required " : "")}{propertyType}{(isRequired ? "" : "?")} {propertyName} {{ get; set; }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private string GetClientName()
    {
        if (!string.IsNullOrEmpty(_clientName))
            return _clientName;

        var title = _document.Info?.Title?.Replace(" ", "").Replace("-", "").Replace("_", "") ?? "Api";
        return $"{title}Client";
    }

    public string GetCSharpType(IOpenApiSchema schema)
    {
        var schemaName = GetSchemaName(schema);
        if (schemaName != null)
        {
            return GetTypeName(schemaName);
        }

        // Try to resolve from type mapping config
        var resolved = _typeMappingConfig.Resolve(schema.Type, schema.Format);
        if (resolved != null)
            return resolved;

        return schema.Type switch
        {
            JsonSchemaType.Array when schema.Items != null => $"List<{GetCSharpType(schema.Items)}>",
            JsonSchemaType.Array => "List<object>",
            _ => "object"
        };
    }

    public static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var words = input.Split(new[] { '-', '_', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }

    public static string ToCamelCase(string input)
    {
        var pascal = ToPascalCase(input);
        if (string.IsNullOrEmpty(pascal)) return pascal;
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    /// <summary>
    /// Strips the configured namespace prefix from a schema name.
    /// For example, with prefix "Commerce", "Commerce.Order" becomes "Order".
    /// </summary>
    private string StripNamespacePrefix(string name)
    {
        if (string.IsNullOrEmpty(_namespacePrefix))
            return name;

        var prefix = _namespacePrefix.EndsWith('.') ? _namespacePrefix : _namespacePrefix + ".";

        return name.StartsWith(prefix, StringComparison.Ordinal)
            ? name[prefix.Length..]
            : name;
    }

    /// <summary>
    /// Decomposes a potentially dotted schema name into namespace segments and a type name.
    /// For example, "Pet.Status" becomes ("Pet", "Status").
    /// </summary>
    private static (string additionalNamespace, string typeName) DecomposeName(string name)
    {
        var dotIndex = name.LastIndexOf('.');
        if (dotIndex < 0)
            return ("", name);

        var namespacePart = name[..dotIndex];
        var typeName = name[(dotIndex + 1)..];

        var segments = namespacePart.Split('.');
        var pascalSegments = segments.Select(ToPascalCase);
        return (string.Join(".", pascalSegments), typeName);
    }

    /// <summary>
    /// Extracts the type name (last segment) from a potentially dotted schema name.
    /// </summary>
    private static string GetTypeName(string name)
    {
        var dotIndex = name.LastIndexOf('.');
        return dotIndex < 0 ? name : name[(dotIndex + 1)..];
    }

    /// <summary>
    /// Extracts the schema name from an IOpenApiSchema, handling both direct Id and OpenApiSchemaReference.
    /// </summary>
    private static string? GetSchemaName(IOpenApiSchema schema)
    {
        if (!string.IsNullOrEmpty(schema.Id))
            return schema.Id;

        if (schema is OpenApiSchemaReference schemaRef)
            return schemaRef.Reference.Id;

        return null;
    }

    private static string EscapeXmlComment(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}
