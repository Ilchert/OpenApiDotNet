using System.Text.Json.Nodes;

namespace OpenApiDotNet.Generators;

internal class StringEnumMember
{
    public string Name { get; }
    public string EnumMemberName { get; }

    public StringEnumMember(JsonNode jsonNode)
    {
        Name = jsonNode.ToString();
        EnumMemberName = GeneratorContext.ToPascalCase(Name);
    }

    public void Write(CodeWriter writer)
    {
        writer.WriteLine($"[System.Text.Json.Serialization.JsonConverter.JsonStringEnumMemberName(\"{Name}\")]");
        writer.WriteLine($"{EnumMemberName},");
        writer.WriteLine();
    }
}
