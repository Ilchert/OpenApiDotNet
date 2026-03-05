namespace OpenApiDotNet.Generators;

internal class BuilderGenerator : BaseGenerator
{
    public override string Namespace { get; }
    public string BuilderName { get; }
    public bool IsParameter { get; }
    public string SegmentName { get; }
    public string? ParameterType { get; }
    public string? ParameterCamelName { get; }
    public string? ParameterFieldName { get; }
    public List<BuilderPropertyGenerator> Properties { get; } = [];
    public List<BuilderOperationGenerator> Operations { get; } = [];



    public BuilderGenerator(PathSegmentNode node, GeneratorContext context) : base(context)
    {
        (Namespace, BuilderName) = Context.GetNameAndNamespace(node.BuilderName, GeneratorCategory.Builder);
        IsParameter = node.IsParameter;
        SegmentName = node.SegmentName;

        if (node.IsParameter)
        {
            ParameterType = node.ParameterSchema != null ? context.GetCSharpType(node.ParameterSchema) : "string";
            ParameterCamelName = GeneratorContext.ToCamelCase(node.ParameterName ?? "id");
            ParameterFieldName = $"_{ParameterCamelName}";
        }

        foreach (var (_, child) in node.Children)
            Properties.Add(BuilderPropertyGenerator.Create(child, context));

        foreach (var (method, operation) in node.Operations)
            Operations.Add(new BuilderOperationGenerator(method, operation, context));
    }

    public override void Write(CodeWriter writer)
    {
        writer.WriteLine($"public class {BuilderName} : IOpenApiBuilder");
        writer.WriteLine("{");
        writer.Indent();

        writer.WriteLine("private readonly IOpenApiBuilder _parentBuilder;");
        if (IsParameter)
            writer.WriteLine($"private readonly {ParameterType} {ParameterFieldName};");
        writer.WriteLine();

        writer.WriteLine("#pragma warning disable CS8618");
        writer.WriteLine($"protected {BuilderName}() {{ }}");
        writer.WriteLine("#pragma warning restore CS8618");
        writer.WriteLine();

        if (IsParameter)
        {
            writer.WriteLine($"public {BuilderName}(IOpenApiBuilder parentBuilder, {ParameterType} {ParameterCamelName})");
            writer.WriteLine("{");
            writer.Indent();
            writer.WriteLine("_parentBuilder = parentBuilder;");
            writer.WriteLine($"{ParameterFieldName} = {ParameterCamelName};");
            writer.Unindent();
            writer.WriteLine("}");
        }
        else
        {
            writer.WriteLine($"public {BuilderName}(IOpenApiBuilder parentBuilder)");
            writer.WriteLine("{");
            writer.Indent();
            writer.WriteLine("_parentBuilder = parentBuilder;");
            writer.Unindent();
            writer.WriteLine("}");
        }
        writer.WriteLine();

        writer.WriteLine("public IOpenApiClient Client => _parentBuilder.Client;");
        if (IsParameter)
            writer.WriteLine($"public string GetPath() => $\"{{_parentBuilder.GetPath()}}/{{{ParameterFieldName}}}\";");
        else
            writer.WriteLine($"public string GetPath() => $\"{{_parentBuilder.GetPath()}}/{SegmentName}\";");
        writer.WriteLine();

        Properties.ForEach(p => p.Write(writer));

        Operations.ForEach(op => op.Write(writer));

        writer.Unindent();
        writer.WriteLine("}");
    }
}
