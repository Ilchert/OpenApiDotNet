using System.Text.Json.Nodes;

namespace OpenApiDotNet.Generators;

internal class StringEnumMember
{
    public string Name { get; }
    public string EnumMemberName { get; set; }

    public StringEnumMember(JsonNode jsonNode)
    {
        Name = jsonNode.ToString();
        EnumMemberName = NamingConventions.ToPascalCase(Name);
    }

    public void Write(CodeWriter writer)
    {
        if (Name != EnumMemberName)
            writer.WriteLine($"[System.Text.Json.Serialization.JsonStringEnumMemberName(\"{Name}\")]");
        writer.WriteLine($"{EnumMemberName},");
        writer.WriteLine();
    }
}
