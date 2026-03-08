using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class ResponseGenerator
{
    public string AsyncResponseType => ResponseType == "void" ? "System.Threading.Tasks.Task" : $"System.Threading.Tasks.Task<{ResponseType}>";
    public string ResponseType { get; }
    public BaseGenerator? NestedClassGenerator { get; }
    public ResponseGenerator(IOpenApiResponse response, string methodName, GeneratorContext context)
    {
        var content = response.Content?.FirstOrDefault();
        if (content?.Value?.Schema is not { } schema)
        {
            ResponseType = "void";
            return;
        }
        if (GeneratorContext.IsInlineObjectSchema(schema))
        {
            ResponseType = $"{methodName}Response";
            NestedClassGenerator = new ObjectGenerator(ResponseType, schema, context);
        }
        else
        {
            ResponseType = context.GetCSharpType(schema).FullName;
        }
    }
}
