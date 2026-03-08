using Microsoft.OpenApi;

namespace OpenApiDotNet.Generators;

internal class ClientGenerator : BaseGenerator
{
    public override GeneratedTypeInfo TypeInfo { get; }
    public string ClientName { get; }
    public string? Description { get; }
    public PathSegmentNode PathTreeRoot { get; }
    public List<BuilderPropertyGenerator> Properties { get; } = [];
    public List<BuilderOperationGenerator> Operations { get; } = [];

    public ClientGenerator(OpenApiDocument document, GeneratorContext context) : base(context)
    {
        ClientName = context.ClientName;
        TypeInfo = new GeneratedTypeInfo(context.DefaultNamespace, $"I{ClientName}");
        Description = document.Info?.Description ?? document.Info?.Title;

        PathTreeRoot = PathTreeBuilder.Build(document.Paths, context);

        // root level operations and properties are added directly to the client interface
        foreach (var (_, child) in PathTreeRoot.Children)
            Properties.Add(BuilderPropertyGenerator.Create(child, context));

        foreach (var (method, operation) in PathTreeRoot.Operations)
            Operations.Add(new BuilderOperationGenerator(method, operation, context));
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
