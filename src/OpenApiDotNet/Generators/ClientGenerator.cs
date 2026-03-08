using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class ClientGenerator : BaseGenerator
{
    public override GeneratedTypeInfo TypeInfo { get; }
    public string ClientName { get; }
    public string? Description { get; }
    public List<BuilderGenerator> BuilderGenerators { get; }
    public List<BuilderPropertyGenerator> Properties { get; } = [];
    public List<BuilderOperationGenerator> Operations { get; } = [];

    public ClientGenerator(OpenApiDocument document, GeneratorContext context) : base(context)
    {
        ClientName = context.ClinetName;
        TypeInfo = new GeneratedTypeInfo(context.DefaultNamespace, $"I{ClientName}");
        Description = document.Info?.Description ?? document.Info?.Title;

        var root = PathTreeBuilder.Build(document.Paths, context);
        BuilderGenerators = [];

        // root level operations and properties are added directly to the client interface, they will be implemented by the root builder
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

        writer.WriteLine($"public interface {TypeInfo.Name} : IOpenApiClient");
        writer.WriteLine("{");
        writer.Indent();

        Properties.ForEach(p => p.Write(writer));
        Operations.ForEach(op => op.Write(writer));

        writer.Unindent();
        writer.WriteLine("}");
    }
}
