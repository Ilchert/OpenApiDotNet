using System.Reflection.Metadata;
using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class ObjectGenerator : BaseGenerator
{
    private IOpenApiSchema _schema;

    public override string Namespace { get; }

    public string TypeName { get; }

    public ObjectGenerator(string name, IOpenApiSchema schema, GeneratorContext context) : base(context)
    {
        _schema = schema;
        (Namespace, TypeName) = Context.GetNameAndNamespace(name, GeneratorCategory.Model);
        if (_schema.Type is not JsonSchemaType.Object)
            throw new InvalidOperationException("Schema is not of type Object.");
    }

    public override void Write(CodeWriter writer)
    {
        WriteSummary(writer, _schema.Description);

        writer.WriteLine($$"""
public class {{TypeName}}
{
""");
        writer.WriteLine("{");
        if (_schema.Properties != null)
        {
            foreach (var property in _schema.Properties)
            {
                var propertyName = GeneratorContext.ToPascalCase(property.Key);
                var propertyType = GetModelPropertyType(property.Value, propertyName, nestedTypes, $"{typeName}{propertyName}");
                var isRequired = schema.Required?.Contains(property.Key) ?? false;
                
                WriteSummary(writer, property.Value.Description);

                sb.AppendLine($"    [JsonPropertyName(\"{property.Key}\")]");
                sb.AppendLine($"    public {(isRequired ? "required " : "")}{propertyType}{(isRequired ? "" : "?")} {propertyName} {{ get; set; }}");
                sb.AppendLine();
            }
        }
    }
}
