using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class EndClientGenerator : BaseGenerator
{
    public override string Namespace { get; }
    public string ClientName { get; }
    public string InterfaceName { get; }
    public string? Description { get; }
    public List<BuilderGenerator> BuilderGenerators { get; }
    public List<BuilderPropertyGenerator> Properties { get; } = [];
    public List<BuilderOperationGenerator> Operations { get; } = [];

    public EndClientGenerator(OpenApiDocument document, GeneratorContext context) : base(context)
    {
        Namespace = context.DefaultNamespace;
        ClientName = context.ClinetName;
        InterfaceName = $"I{ClientName}";
        Description = document.Info?.Description ?? document.Info?.Title;

        var root = PathTreeBuilder.Build(document.Paths);
        BuilderGenerators = [];


        foreach (var (_, child) in root.Children)
            Properties.Add(BuilderPropertyGenerator.Create(child, context));

        foreach (var (method, operation) in root.Operations)
            Operations.Add(new BuilderOperationGenerator(method, operation, context));

        CollectBuilders(root, context);
    }

    private void CollectBuilders(PathSegmentNode node, GeneratorContext context)
    {
        foreach (var (_, child) in node.Children)
        {
            BuilderGenerators.Add(new BuilderGenerator(child, context));
            CollectBuilders(child, context);
        }
    }

    public override void Write(CodeWriter writer)
    {
        WriteSummary(writer, Description);

        writer.WriteLine($"public interface {InterfaceName} : IOpenApiClient");
        writer.WriteLine("{");
        writer.Indent();

        Properties.ForEach(p => p.Write(writer));
        Operations.ForEach(op => op.Write(writer));

        writer.Unindent();
        writer.WriteLine("}");
    }
}
