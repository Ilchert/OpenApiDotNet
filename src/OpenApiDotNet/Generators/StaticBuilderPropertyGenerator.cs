namespace OpenApiDotNet.Generators;

internal class StaticBuilderPropertyGenerator(PathSegmentNode node, GeneratorContext context) : BuilderPropertyGenerator(node.BuilderName)
{
    public string PropertyName { get; } = GeneratorContext.ToPascalCase(node.SegmentName);

    public override void Write(CodeWriter writer)
    {
        writer.WriteLine($"public virtual {BuilderName} {PropertyName} => new(this);");
        writer.WriteLine();
    }
}
