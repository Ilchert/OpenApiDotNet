using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class EnumGenerator : BaseGenerator // Add oneOf support
{
    public override string Namespace { get; }

    public string TypeName { get; }

    public string? Description { get; }

    public List<StringEnumMember> EnumMembers { get; }

    public EnumGenerator(string name, IOpenApiSchema schema, GeneratorContext context) : base(context)
    {
        (Namespace, TypeName) = Context.GetNameAndNamespace(name, GeneratorCategory.Model);
        if (schema.Enum is null)
            throw new InvalidOperationException("Enum schema must have an Enum property.");
        Description = schema.Description;
        EnumMembers = schema.Enum.Select(enumValue => new StringEnumMember(enumValue)).ToList();
    }

    public override void Write(CodeWriter writer)
    {
        WriteSummary(writer, Description);

        writer.WriteLine($$"""
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum {{TypeName}}
{
""");
        writer.Indent();

        EnumMembers.ForEach(m => m.Write(writer));

        writer.Unindent();
        writer.WriteLine("}");
    }
}
