using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class BuilderOperationGenerator
{
    private readonly HttpMethod _httpMethod;
    private readonly OpenApiOperation _operation;
    private readonly GeneratorContext _context;

    public string MethodName { get; }
    public string ResponseType { get; }
    public List<BaseGenerator> NestedTypeGenerators { get; } = [];

    private readonly List<string> _requiredParameters = [];
    private readonly List<string> _optionalParameters = [];
    private readonly List<QueryParamInfo> _queryParams = [];
    private string? _bodyParamName;

    public BuilderOperationGenerator(HttpMethod httpMethod, OpenApiOperation operation, GeneratorContext context)
    {
        _httpMethod = httpMethod;
        _operation = operation;
        _context = context;
        MethodName = GeneratorContext.ToPascalCase(httpMethod.Method.ToLowerInvariant());

        ProcessQueryParameters();
        ProcessRequestBody();
        ResponseType = ResolveResponseType();
        _optionalParameters.Add("CancellationToken cancellationToken = default");
    }

    private void ProcessQueryParameters()
    {
        if (_operation.Parameters == null) return;

        foreach (var parameter in _operation.Parameters)
        {
            if (parameter.In == ParameterLocation.Query)
            {
                var paramName = GeneratorContext.ToCamelCase(parameter.Name);
                var paramType = _context.GetCSharpType(parameter.Schema);
                var isRequired = parameter.Required;
                var isCollection = paramType.StartsWith("List<");

                if (isRequired)
                    _requiredParameters.Add($"{paramType} {paramName}");
                else
                    _optionalParameters.Add($"{paramType}? {paramName} = default");

                _queryParams.Add(new(parameter.Name, paramName, paramType, isRequired, isCollection));
            }
        }
    }

    private void ProcessRequestBody()
    {
        if (_operation.RequestBody == null) return;

        var content = _operation.RequestBody.Content.FirstOrDefault();
        if (content.Value?.Schema == null) return;

        var schema = content.Value.Schema;
        string requestBodyType;

        if (GeneratorContext.IsInlineObjectSchema(schema))
        {
            var nestedClassName = $"{MethodName}Request";
            requestBodyType = nestedClassName;
            NestedTypeGenerators.Add(new ObjectGenerator(nestedClassName, schema, _context));
        }
        else
        {
            requestBodyType = _context.GetCSharpType(schema);
        }

        _bodyParamName = _operation.RequestBody.Extensions?.TryGetValue("x-bodyName", out var bodyNameExt) == true
            && bodyNameExt is JsonNodeExtension { Node: { } bodyNameNode }
            ? bodyNameNode.GetValue<string>()
            : null;
        if (string.IsNullOrWhiteSpace(_bodyParamName))
            _bodyParamName = "request";

        _requiredParameters.Add($"{requestBodyType} {_bodyParamName}");
    }

    private string ResolveResponseType()
    {
        var successResponse = _operation.Responses.FirstOrDefault(r => r.Key.StartsWith("2"));
        if (successResponse.Value?.Content?.Any() != true)
            return "void";

        var content = successResponse.Value.Content.FirstOrDefault();
        if (content.Value?.Schema == null)
            return "void";

        var schema = content.Value.Schema;

        if (GeneratorContext.IsInlineObjectSchema(schema))
        {
            var nestedClassName = $"{MethodName}Response";
            NestedTypeGenerators.Add(new ObjectGenerator(nestedClassName, schema, _context));
            return nestedClassName;
        }

        return _context.GetCSharpType(schema);
    }

    public void Write(CodeWriter writer)
    {
        BaseGenerator.WriteSummary(writer, _operation.Summary ?? _operation.Description);

        var parameters = _requiredParameters.Concat(_optionalParameters);
        var returnType = ResponseType == "void" ? "Task" : $"Task<{ResponseType}>";

        writer.WriteLine($"public virtual async {returnType} {MethodName}({string.Join(", ", parameters)})");
        writer.WriteLine("{");
        writer.Indent();

        WriteUrlBuilding(writer);
        writer.WriteLine();
        WriteHttpCall(writer);

        writer.Unindent();
        writer.WriteLine("}");
        writer.WriteLine();

        NestedTypeGenerators.ForEach(g => g.Write(writer));
    }

    private void WriteUrlBuilding(CodeWriter writer)
    {
        if (_queryParams.Count > 0)
        {
            writer.WriteLine("var url = GetPath();");
            writer.WriteLine();
            writer.WriteLine("var queryString = new List<string>();");

            foreach (var param in _queryParams)
            {
                if (param.IsCollection)
                {
                    if (!param.Required)
                    {
                        writer.WriteLine($"if ({param.ParamName} != null)");
                        writer.Indent();
                        writer.WriteLine($"foreach (var item in {param.ParamName})");
                        writer.Indent();
                        writer.WriteLine($"queryString.Add($\"{param.Name}={{Uri.EscapeDataString(item.ToString())}}\");");
                        writer.Unindent();
                        writer.Unindent();
                    }
                    else
                    {
                        writer.WriteLine($"foreach (var item in {param.ParamName})");
                        writer.Indent();
                        writer.WriteLine($"queryString.Add($\"{param.Name}={{Uri.EscapeDataString(item.ToString())}}\");");
                        writer.Unindent();
                    }
                }
                else if (param.Required)
                {
                    writer.WriteLine($"queryString.Add($\"{param.Name}={{Uri.EscapeDataString({param.ParamName}.ToString())}}\");");
                }
                else
                {
                    writer.WriteLine($"if ({param.ParamName} is {{}} {param.ParamName}Value)");
                    writer.Indent();
                    writer.WriteLine($"queryString.Add($\"{param.Name}={{Uri.EscapeDataString({param.ParamName}Value.ToString())}}\");");
                    writer.Unindent();
                }
            }

            writer.WriteLine("if (queryString.Count > 0)");
            writer.Indent();
            writer.WriteLine("url += \"?\" + string.Join(\"&\", queryString);");
            writer.Unindent();
        }
        else
        {
            writer.WriteLine("var url = GetPath();");
        }
    }

    private void WriteHttpCall(CodeWriter writer)
    {
        var hasBody = _bodyParamName != null;

        if (_httpMethod == HttpMethod.Get)
        {
            writer.WriteLine("var response = await Client.HttpClient.GetAsync(url, cancellationToken);");
        }
        else if (_httpMethod == HttpMethod.Post)
        {
            writer.WriteLine(hasBody
                ? $"var response = await Client.HttpClient.PostAsJsonAsync(url, {_bodyParamName}, Client.JsonOptions, cancellationToken);"
                : "var response = await Client.HttpClient.PostAsync(url, null, cancellationToken);");
        }
        else if (_httpMethod == HttpMethod.Put)
        {
            writer.WriteLine(hasBody
                ? $"var response = await Client.HttpClient.PutAsJsonAsync(url, {_bodyParamName}, Client.JsonOptions, cancellationToken);"
                : "var response = await Client.HttpClient.PutAsync(url, null, cancellationToken);");
        }
        else if (_httpMethod == HttpMethod.Delete)
        {
            writer.WriteLine("var response = await Client.HttpClient.DeleteAsync(url, cancellationToken);");
        }
        else if (_httpMethod == HttpMethod.Patch)
        {
            if (hasBody)
            {
                writer.WriteLine($"var content = JsonContent.Create({_bodyParamName}, options: Client.JsonOptions);");
                writer.WriteLine("var response = await Client.HttpClient.PatchAsync(url, content, cancellationToken);");
            }
            else
            {
                writer.WriteLine("var response = await Client.HttpClient.PatchAsync(url, null, cancellationToken);");
            }
        }

        writer.WriteLine("response.EnsureSuccessStatusCode();");

        if (ResponseType != "void")
        {
            writer.WriteLine($"var deserializedResponse = await response.Content.ReadFromJsonAsync<{ResponseType}>(Client.JsonOptions, cancellationToken);");
            writer.WriteLine("if (deserializedResponse is { } deserializedResponseValue)");
            writer.Indent();
            writer.WriteLine("return deserializedResponseValue;");
            writer.Unindent();
            writer.WriteLine("throw new InvalidOperationException($\"Response from {url} is null\");");
        }
    }

    private record QueryParamInfo(string Name, string ParamName, string ParamType, bool Required, bool IsCollection);
}
