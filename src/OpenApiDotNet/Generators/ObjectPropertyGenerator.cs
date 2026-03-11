using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class ObjectPropertyGenerator
{
    public string Name { get; set; }
    public GeneratorContext Context { get; }
    public string PropertyName { get; set; }
    public string TypeName { get; set; }
    public string? Description { get; }
    public bool IsRequired { get; }
    public BaseGenerator? NestedPropertyTypeGenerator { get; set; }

    public ObjectPropertyGenerator(string name, IOpenApiSchema schema, string parentName, bool isRequired, GeneratorContext context)
    {
        Name = name;
        Context = context;
        PropertyName = NamingConventions.ToPascalCase(name);
        Description = schema.Description;
        IsRequired = isRequired;

        if (PropertyName == parentName)
            PropertyName += "Value";

        (TypeName, NestedPropertyTypeGenerator) = GetPropertyType(schema, parentName);
    }

    private (string TypeName, BaseGenerator? NestedPropertyTypeGenerator) GetPropertyType(IOpenApiSchema schema, string parentName)
    {
        if (schema.GetSchemaName() != null)
            return (Context.GetCSharpType(schema).FullName, null);

        var nestedTypeName = $"{parentName}{PropertyName}";

        if (schema.Enum != null && schema.Enum.Count > 0)
            return (nestedTypeName, new EnumGenerator(nestedTypeName, schema, Context));

        if (schema.IsInlineObjectSchema())
            return (nestedTypeName, new ObjectGenerator(nestedTypeName, schema, Context));

        return (Context.GetCSharpType(schema).FullName, null);
    }

    public void Write(CodeWriter writer)
    {
        BaseGenerator.WriteSummary(writer, Description);
        writer.WriteLine($$"""
[System.Text.Json.Serialization.JsonPropertyName("{{Name}}")]
public {{(IsRequired ? "required " : "")}}{{TypeName}}{{(IsRequired ? "" : "?")}} {{PropertyName}} { get; set; }
""");
        writer.WriteLine();

        NestedPropertyTypeGenerator?.Write(writer);
    }
}
