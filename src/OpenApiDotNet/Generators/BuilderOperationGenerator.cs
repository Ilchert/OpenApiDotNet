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
        MethodName = NamingConventions.ToPascalCase(httpMethod.Method.ToLowerInvariant());

        if (_operation.Parameters != null)
            foreach (var parameter in _operation.Parameters)
                if (parameter.In == ParameterLocation.Query)
                    _parameters.Add(new QueryParameterGenerator(parameter, context));

        if (operation.RequestBody?.Content?.Any() == true)
            _bodyGenerator = new BodyGenerator(operation.RequestBody, MethodName, context);

        if (operation.Responses?.FirstOrDefault(r => r.Key.StartsWith("2")).Value is { } operationResponse2xx)
            _responseGenerator = new ResponseGenerator(operationResponse2xx, MethodName, context);
        else
            _responseGenerator = new ResponseGenerator(new OpenApiResponse(), MethodName, context); // void
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
        if (_bodyGenerator is { IsRequired: false })
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
