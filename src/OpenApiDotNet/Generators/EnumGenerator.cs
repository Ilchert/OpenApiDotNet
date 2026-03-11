using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class EnumGenerator : BaseGenerator // Add oneOf support
{
    public override GeneratedTypeInfo TypeInfo { get; }

    public string? Description { get; }

    public List<StringEnumMember> EnumMembers { get; }

    public EnumGenerator(string name, IOpenApiSchema schema, GeneratorContext context) : base(context)
    {
        TypeInfo = Context.GetNameAndNamespace(name, GeneratorCategory.Model);
        if (schema.Enum is null)
            throw new InvalidOperationException("Enum schema must have an Enum property.");
        Description = schema.Description;
        EnumMembers = schema.Enum.Select(enumValue => new StringEnumMember(enumValue)).ToList();
        DisambiguateDuplicateNames();
    }

    private void DisambiguateDuplicateNames()
    {
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in EnumMembers)
        {
            if (!usedNames.Add(member.EnumMemberName))
            {
                var suffix = 2;
                while (!usedNames.Add($"{member.EnumMemberName}{suffix}"))
                    suffix++;
                member.EnumMemberName = $"{member.EnumMemberName}{suffix}";
            }
        }
    }

    public override void Write(CodeWriter writer)
    {
        WriteSummary(writer, Description);

        writer.WriteLine($$"""
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum {{TypeInfo.Name}}
{
""");
        writer.Indent();

        EnumMembers.ForEach(m => m.Write(writer));

        writer.Unindent();
        writer.WriteLine("}");
    }
}
