using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class BodyGenerator
{
    public string ParameterDeclaration => IsRequired ? $"{ParameterType} {ParameterName}" : $"{ParameterType}? {ParameterName} = default";
    public string ParameterName { get; }
    public string ParameterType { get; }
    public bool IsRequired { get; }
    public BaseGenerator? NestedClassGenerator { get; }

    public BodyGenerator(IOpenApiRequestBody requestBody, string methodName, GeneratorContext context)
    {
        var content = requestBody.Content?.FirstOrDefault();
        if (content?.Value?.Schema is not { } schema)
            throw new InvalidOperationException();

        if (schema.IsInlineObjectSchema())
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
