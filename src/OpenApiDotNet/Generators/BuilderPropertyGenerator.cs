namespace OpenApiDotNet.Generators;

internal abstract class BuilderPropertyGenerator
{
    public GeneratedTypeInfo BuilderTypeInfo { get; }

    public BuilderPropertyGenerator(PathSegmentNode node, GeneratorContext context)
    {
        BuilderTypeInfo = context.GetNameAndNamespace(node.BuilderName, GeneratorCategory.Builder);
    }

    public static BuilderPropertyGenerator Create(PathSegmentNode node, GeneratorContext context) =>
        node.IsParameter
            ? new ParameterBuilderPropertyGenerator(node, context)
            : new StaticBuilderPropertyGenerator(node, context);

    public abstract void Write(CodeWriter writer);
}
