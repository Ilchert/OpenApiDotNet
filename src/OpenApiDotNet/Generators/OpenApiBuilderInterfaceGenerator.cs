namespace OpenApiDotNet.Generators;

internal class OpenApiBuilderInterfaceGenerator : BaseGenerator
{
    public override GeneratedTypeInfo TypeInfo { get; }

    public OpenApiBuilderInterfaceGenerator(GeneratorContext context) : base(context)
    {
        TypeInfo = new GeneratedTypeInfo(context.DefaultNamespace, "IOpenApiBuilder");
    }

    public override void Write(CodeWriter writer)
    {
        writer.WriteLine("""
/// <summary>
/// Base interface for all fluent API builders
/// </summary>
public interface IOpenApiBuilder
{
    IOpenApiClient Client { get; }
    string GetPath();
}
""");
    }
}
