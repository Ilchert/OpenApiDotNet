using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class BuilderOperationGenerator
{
    private readonly HttpMethod _httpMethod;
    private readonly OpenApiOperation _operation;
    private readonly GeneratorContext _context;
    public string MethodName { get; }
    private readonly List<QueryParameterGenerator> _parameters = [];
    private readonly BodyGenerator? _bodyGenerator;
    private readonly ResponseGenerator _responseGenerator;
    private string? _description;
    public BuilderOperationGenerator(HttpMethod httpMethod, OpenApiOperation operation, GeneratorContext context)
    {
        _description = operation.Summary ?? operation.Description;
        _httpMethod = httpMethod;
        _operation = operation;
        _context = context;
        MethodName = GeneratorContext.ToPascalCase(httpMethod.Method.ToLowerInvariant());

        if (_operation.Parameters != null)
            foreach (var parameter in _operation.Parameters)
                if (parameter.In == ParameterLocation.Query)
                    _parameters.Add(new QueryParameterGenerator(parameter, context));

        if (operation.RequestBody?.Content?.Any() == true)
            _bodyGenerator = new BodyGenerator(operation.RequestBody, MethodName, context);

        if (operation.Responses?.FirstOrDefault(r => r.Key.StartsWith("2")).Value is { } operationResponse2xx)
            _responseGenerator = new ResponseGenerator(operationResponse2xx, context);
        else
            _responseGenerator = new ResponseGenerator(new OpenApiResponse(), context); // void
    }

    public void Write(CodeWriter writer)
    {
        BaseGenerator.WriteSummary(writer, _description);

        var parameters = new List<string>();

        //required body parameter should come before optional query parameters for better usability, even though it's not a strict requirement
        if (_bodyGenerator is { IsRequired: true })
            parameters.Add(_bodyGenerator.ParameterDeclaration);

        // then requred query parameters
        parameters.AddRange(_parameters.Where(p => p.IsRequired).Select(p => p.ParameterDeclaration));

        // then optional body parameter
        if (_bodyGenerator != null)
            parameters.Add(_bodyGenerator.ParameterDeclaration);

        // then optional query parameters
        parameters.AddRange(_parameters.Where(p => !p.IsRequired).Select(p => p.ParameterDeclaration));

        // CanlellationToken is always optional and should come last
        parameters.Add("System.Threading.CancellationToken cancellationToken = default");

        writer.WriteLine($"public virtual async {_responseGenerator.AsyncResponseType} {MethodName}({string.Join(", ", parameters)})");
        writer.WriteLine("{");
        writer.Indent();

        WriteUrlBuilding(writer);
        writer.WriteLine();
        WriteHttpCall(writer);

        writer.Unindent();
        writer.WriteLine("}");
        writer.WriteLine();

        _bodyGenerator?.NestedClassGenerator?.Write(writer);
        _responseGenerator.NestedClassGenerator?.Write(writer);
    }

    private void WriteUrlBuilding(CodeWriter writer)
    {
        if (_parameters.Count == 0)
        {
            writer.WriteLine("var url = GetPath();");
            return;
        }

        writer.WriteLine("""
var url = GetPath();
var queryString = new System.Collections.Generic.List<string>();

""");

        foreach (var param in _parameters)
            param.WriteAddToToQueryString(writer);

        writer.WriteLine("""
if (queryString.Count > 0)
    url += "?" + string.Join("&", queryString);
""");
    }

    private void WriteHttpCall(CodeWriter writer)
    {
        var hasBody = _bodyGenerator != null;

        if (_httpMethod == HttpMethod.Get)
        {
            writer.WriteLine("var response = await Client.HttpClient.GetAsync(url, cancellationToken);");
        }
        else if (_httpMethod == HttpMethod.Post)
        {
            writer.WriteLine(hasBody
                ? $"var response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(Client.HttpClient, url, {_bodyGenerator!.ParameterName}, Client.JsonOptions, cancellationToken);"
                : "var response = await Client.HttpClient.PostAsync(url, null, cancellationToken);");
        }
        else if (_httpMethod == HttpMethod.Put)
        {
            writer.WriteLine(hasBody
                ? $"var response = await System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync(Client.HttpClient, url, {_bodyGenerator!.ParameterName}, Client.JsonOptions, cancellationToken);"
                : "var response = await Client.HttpClient.PutAsync(url, null, cancellationToken);");
        }
        else if (_httpMethod == HttpMethod.Delete)
        {
            writer.WriteLine("var response = await Client.HttpClient.DeleteAsync(url, cancellationToken);");
        }
        else if (_httpMethod == HttpMethod.Patch)
        {
            writer.WriteLine(hasBody
                ? $"var response = await System.Net.Http.Json.HttpClientJsonExtensions.PatchAsJsonAsync(Client.HttpClient, url, {_bodyGenerator!.ParameterName}, Client.JsonOptions, cancellationToken);"
                : "var response = await Client.HttpClient.PatchAsync(url, null, cancellationToken);");
        }

        writer.WriteLine("response.EnsureSuccessStatusCode();");

        if (_responseGenerator.ResponseType != "void")
        {
            writer.WriteLine($$"""
var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<{{_responseGenerator.ResponseType}}>(response.Content, Client.JsonOptions, cancellationToken);
if (deserializedResponse is { } deserializedResponseValue)
    return deserializedResponseValue;
throw new System.InvalidOperationException($"Response from {url} is null");
""");
        }
    }
}

internal class BodyGenerator
{
    public string ParameterDeclaration => IsRequired ? $"{ParameterType} {ParameterName}" : $"{ParameterType}? {ParameterName} = default";
    public string ParameterName { get; }
    public string ParameterType { get; } // use GeneratedTypeInfo?
    public bool IsRequired { get; }
    public BaseGenerator? NestedClassGenerator { get; }

    public BodyGenerator(IOpenApiRequestBody requestBody, string methodName, GeneratorContext context)
    {
        var content = requestBody.Content?.FirstOrDefault();
        if (content?.Value?.Schema is not { } schema)
            throw new InvalidOperationException();

        if (GeneratorContext.IsInlineObjectSchema(schema))
        {
            ParameterType = $"{methodName}Request";
            NestedClassGenerator = new ObjectGenerator(ParameterType, schema, context);
        }
        else
        {
            ParameterType = context.GetCSharpType(schema).FullName;
        }

        ParameterName = "request";
        if (requestBody.Extensions?.TryGetValue("x-bodyName", out var bodyNameExt) == true
            && bodyNameExt is JsonNodeExtension { Node: { } bodyNameNode })
            ParameterName = bodyNameNode.GetValue<string>();
        IsRequired = requestBody.Required;
    }
}

internal class QueryParameterGenerator
{
    public string ParameterDeclaration => IsRequired ? $"{ParameterType} {ParameterName}" : $"{ParameterType}? {ParameterName} = default";
    public string ParameterName { get; }
    public string ParameterType { get; }
    public string Name { get; }
    public bool IsRequired { get; }
    public bool IsCollection { get; }
    public QueryParameterGenerator(IOpenApiParameter openApiParameter, GeneratorContext context)
    {
        Name = openApiParameter.Name ?? throw new InvalidOperationException("Parameter name is null");
        ParameterName = GeneratorContext.ToCamelCase(openApiParameter.Name);
        ParameterType = context.GetCSharpType(openApiParameter.Schema ?? throw new InvalidOperationException("Parameter schema is null")).FullName;
        IsRequired = openApiParameter.Required;
        IsCollection = openApiParameter.Schema.Type == JsonSchemaType.Array;
    }
    public void WriteAddToToQueryString(CodeWriter writer)
    {
        // added item.ToString()! to avoid NRT warnings, since ToString() can return null for boxed Nullable
        if (IsCollection)
        {
            if (!IsRequired)
            {
                writer.WriteLine($$"""
if ({{ParameterName}} != null)
    foreach (var item in {{ParameterName}})
        queryString.Add($"{{Name}}={System.Uri.EscapeDataString(item.ToString() ?? "null")}");

""");
            }
            else
            {
                writer.WriteLine($$"""
foreach (var item in {{ParameterName}})
    queryString.Add($"{{Name}}={System.Uri.EscapeDataString(item.ToString() ?? "null")}");

""");
            }
        }
        else if (IsRequired)
        {
            writer.WriteLine($$"""queryString.Add($"{{Name}}={System.Uri.EscapeDataString({{ParameterName}}.ToString() ?? "null")}");""");
        }
        else
        {
            writer.WriteLine($$"""
if ({{ParameterName}} is {} {{ParameterName}}Value)
    queryString.Add($"{{Name}}={System.Uri.EscapeDataString({{ParameterName}}Value.ToString() ?? "null")}");
""");
        }
    }
}

internal class ResponseGenerator
{
    public string AsyncResponseType => ResponseType == "void" ? "System.Threading.Tasks.Task" : $"System.Threading.Tasks.Task<{ResponseType}>";
    public string ResponseType { get; }
    public BaseGenerator? NestedClassGenerator { get; }
    public ResponseGenerator(IOpenApiResponse response, GeneratorContext context)
    {
        var content = response.Content?.FirstOrDefault();
        if (content?.Value?.Schema is not { } schema)
        {
            ResponseType = "void";
            return;
        }
        if (GeneratorContext.IsInlineObjectSchema(schema))
        {
            ResponseType = $"Response";
            NestedClassGenerator = new ObjectGenerator(ResponseType, schema, context);
        }
        else
        {
            ResponseType = context.GetCSharpType(schema).FullName;
        }
    }
    public void WriteDeserialization(CodeWriter writer)
    {
        if (ResponseType != "void")
        {
            writer.WriteLine($$"""
var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<{{ResponseType}}>(response.Content, Client.JsonOptions, cancellationToken);
if (deserializedResponse is { } deserializedResponseValue)
    return deserializedResponseValue;
throw new System.InvalidOperationException($"Response from {url} is null");
""");
        }
    }
}
