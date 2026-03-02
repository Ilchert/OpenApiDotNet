using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class EnumGenerator : BaseGenerator // Add oneOf support
{
    private IOpenApiSchema _schema;

    public override string Namespace { get; }

    public string TypeName { get; }

    public EnumGenerator(string name, IOpenApiSchema schema, GeneratorContext context) : base(context)
    {
        _schema = schema;
        (Namespace, TypeName) = Context.GetNameAndNamespace(name, GeneratorCategory.Model);
        if (_schema.Enum is null)
            throw new InvalidOperationException("Enum schema must have an Enum property.");
    }

    public override void Write(CodeWriter writer)
    {
        WriteSummary(writer, _schema.Description);

        writer.WriteLine($$"""
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum {{TypeName}}
{
""");
        writer.Indent();

        foreach (var enumValue in _schema.Enum!)
        {
            var stringValue = enumValue.ToString();

            var memberName = GeneratorContext.ToPascalCase(stringValue);

            if (memberName != stringValue)
            {
                writer.WriteLine($"[JsonStringEnumMemberName(\"{stringValue}\")]");
            }
            writer.WriteLine($"{memberName},");
            writer.WriteLine();
        }

        writer.WriteLine("}");
        Console.WriteLine($"  Generated enum: {TypeName}");
    }
}
