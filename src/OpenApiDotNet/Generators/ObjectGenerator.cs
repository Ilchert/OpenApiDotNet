using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class ObjectGenerator : BaseGenerator
{
    private readonly IOpenApiSchema _schema;

    public override string Namespace { get; }

    public string TypeName { get; }

    public string? Description { get; }

    public List<ObjectPropertyGenerator> Properties { get; }

    public ObjectGenerator(string name, IOpenApiSchema schema, GeneratorContext context) : base(context)
    {
        _schema = schema;
        (Namespace, TypeName) = Context.GetNameAndNamespace(name, GeneratorCategory.Model);
        if (_schema.Type is not JsonSchemaType.Object)
            throw new InvalidOperationException("Schema is not of type Object.");

        Description = _schema.Description;
        Properties = _schema.Properties?.Select(p => new ObjectPropertyGenerator(p.Key, p.Value, TypeName, _schema.Required?.Contains(p.Key) ?? false, Context)).ToList() ?? [];
    }

    public override void Write(CodeWriter writer)
    {
        WriteSummary(writer, _schema.Description);

        writer.WriteLine($"public class {TypeName}");
        writer.WriteLine("{");
        writer.Indent();
        Properties.ForEach(p => p.Write(writer));
        writer.Unindent();
        writer.WriteLine("}");
    }
}
