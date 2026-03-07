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
        if (IsCollection)
        {
            if (!IsRequired)
            {
                writer.WriteLine($$"""
if ({{ParameterName}} != null)
    foreach (var item in {{ParameterName}})
        queryString.Add($"{{Name}}={System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(item, Client.JsonOptions).Trim('"'))}");

""");
            }
            else
            {
                writer.WriteLine($$"""
foreach (var item in {{ParameterName}})
    queryString.Add($"{{Name}}={System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(item, Client.JsonOptions).Trim('"'))}");

""");
            }
        }
        else if (IsRequired)
        {
            writer.WriteLine($$"""queryString.Add($"{{Name}}={System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize({{ParameterName}}, Client.JsonOptions).Trim('"'))}");""");
        }
        else
        {
            writer.WriteLine($$"""
if ({{ParameterName}} is {} {{ParameterName}}Value)
    queryString.Add($"{{Name}}={System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize({{ParameterName}}Value, Client.JsonOptions).Trim('"'))}");
""");
        }
    }
}
