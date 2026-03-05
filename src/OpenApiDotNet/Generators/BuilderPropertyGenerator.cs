namespace OpenApiDotNet.Generators;

internal abstract class BuilderPropertyGenerator(string builderName)
{
    public string BuilderName { get; } = builderName;

    public static BuilderPropertyGenerator Create(PathSegmentNode node, GeneratorContext context) =>
        node.IsParameter
            ? new ParameterBuilderPropertyGenerator(node, context)
            : new StaticBuilderPropertyGenerator(node, context);

    public abstract void Write(CodeWriter writer);
}
