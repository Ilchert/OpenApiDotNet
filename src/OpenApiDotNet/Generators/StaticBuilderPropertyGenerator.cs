namespace OpenApiDotNet.Generators;

internal class StaticBuilderPropertyGenerator(PathSegmentNode node, GeneratorContext context) : BuilderPropertyGenerator(node, context)
{
    public string PropertyName { get; } = GeneratorContext.ToPascalCase(node.SegmentName);

    public override void Write(CodeWriter writer)
    {
        writer.WriteLine($"public virtual {BuilderTypeInfo.FullName} {PropertyName} => new(this);");
        writer.WriteLine();
    }
}
