namespace OpenApiDotNet.Generators;

internal class ParameterBuilderPropertyGenerator(PathSegmentNode node, GeneratorContext context) : BuilderPropertyGenerator(node.BuilderName)
{
    public string ParamType { get; } = node.ParameterSchema != null ? context.GetCSharpType(node.ParameterSchema) : "string";
    public string ParamName { get; } = GeneratorContext.ToCamelCase(node.ParameterName ?? "id");

    public override void Write(CodeWriter writer)
    {
        writer.WriteLine($"public virtual {BuilderName} this[{ParamType} {ParamName}]");
        writer.WriteLine("{");
        writer.Indent();
        writer.WriteLine($"get => new(this, {ParamName});");
        writer.Unindent();
        writer.WriteLine("}");
        writer.WriteLine();
    }
}
