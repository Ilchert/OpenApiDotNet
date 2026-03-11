using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class QueryParameterGenerator
{
    public string ParameterDeclaration => IsRequired ? $"{ParameterType} {ParameterName}" : $"{ParameterType}? {ParameterName} = default";
    public string ParameterName { get; }
    public string ParameterType { get; }
    public string Name { get; }
    public bool IsRequired { get; }
    public bool IsCollection { get; }
    private string ElementType { get; }
    public QueryParameterGenerator(IOpenApiParameter openApiParameter, GeneratorContext context)
    {
        Name = openApiParameter.Name ?? throw new InvalidOperationException("Parameter name is null");
        ParameterName = NamingConventions.ToCamelCase(openApiParameter.Name);
        var schema = openApiParameter.Schema ?? throw new InvalidOperationException("Parameter schema is null");
        ParameterType = context.GetCSharpType(schema).FullName;
        IsRequired = openApiParameter.Required;
        IsCollection = schema.Type == JsonSchemaType.Array;
        ElementType = IsCollection && schema.Items != null
            ? context.GetCSharpType(schema.Items).FullName
            : ParameterType;
    }

    private string FormatValueExpression(string valueName) =>
        ElementType == "string"
            ? $"System.Uri.EscapeDataString({valueName})"
            : $"System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize({valueName}, Client.JsonOptions).Trim('\"'))";
    public void WriteAddToToQueryString(CodeWriter writer)
    {
        var itemExpression = FormatValueExpression("item");

        if (IsCollection)
        {
            if (!IsRequired)
            {
                writer.WriteLine($$"""
if ({{ParameterName}} != null)
    foreach (var item in {{ParameterName}})
        queryString.Add($"{{Name}}={{{itemExpression}}}");

""");
            }
            else
            {
                writer.WriteLine($$"""
foreach (var item in {{ParameterName}})
    queryString.Add($"{{Name}}={{{itemExpression}}}");

""");
            }
        }
        else if (IsRequired)
        {
            var valueExpression = FormatValueExpression(ParameterName);
            writer.WriteLine($$"""queryString.Add($"{{Name}}={{{valueExpression}}}");""");
        }
        else
        {
            var valueExpression = FormatValueExpression($"{ParameterName}Value");
            writer.WriteLine($$"""
if ({{ParameterName}} is {} {{ParameterName}}Value)
    queryString.Add($"{{Name}}={{{valueExpression}}}");
""");
        }
    }
}
