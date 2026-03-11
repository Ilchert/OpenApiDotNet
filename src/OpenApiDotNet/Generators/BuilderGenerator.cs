namespace OpenApiDotNet.Generators;

internal class BuilderGenerator : BaseGenerator
{
    public override GeneratedTypeInfo TypeInfo { get; }
    public bool IsParameter { get; }
    public string SegmentName { get; }
    public string? ParameterType { get; }
    public string? ParameterCamelName { get; }
    public string? ParameterFieldName { get; }
    public List<BuilderPropertyGenerator> Properties { get; } = [];
    public List<BuilderOperationGenerator> Operations { get; } = [];
    public BuilderGenerator(PathSegmentNode node, GeneratorContext context) : base(context)
    {
        TypeInfo = node.BuilderName;
        IsParameter = node.IsParameter;
        SegmentName = node.SegmentName;

        if (node.IsParameter)
        {
            ParameterType = node.ParameterSchema != null ? context.GetCSharpType(node.ParameterSchema).FullName : "string";
            ParameterCamelName = NamingConventions.ToCamelCase(node.ParameterName ?? "id");
            var rawCamelName = ParameterCamelName.TrimStart('@');
            ParameterFieldName = $"_{rawCamelName}";
        }

        foreach (var (_, child) in node.Children)
            Properties.Add(BuilderPropertyGenerator.Create(child, context));

        foreach (var (method, operation) in node.Operations)
            Operations.Add(new BuilderOperationGenerator(method, operation, context));
    }

    public override void Write(CodeWriter writer)
    {
        writer.WriteLine($$"""
public partial class {{TypeInfo.Name}} : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected {{TypeInfo.Name}}() { }
    #pragma warning restore CS8618

""");
        writer.Indent();

        if (IsParameter)
        {
            var pathExpression = ParameterType == "string"
                ? $"{{System.Uri.EscapeDataString({ParameterFieldName})}}"
                : $"{{System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize({ParameterFieldName}, Client.JsonOptions).Trim('\"'))}}";

            writer.WriteLine($$"""
private readonly {{ParameterType}} {{ParameterFieldName}};

public {{TypeInfo.Name}}(IOpenApiBuilder parentBuilder, {{ParameterType}} {{ParameterCamelName}})
{
    _parentBuilder = parentBuilder;
    {{ParameterFieldName}} = {{ParameterCamelName}};
}

public string GetPath() => $"{_parentBuilder.GetPath()}/{{pathExpression}}";
""");
        }
        else
        {
            writer.WriteLine($$"""
public {{TypeInfo.Name}}(IOpenApiBuilder parentBuilder)
{
    _parentBuilder = parentBuilder;
}

public string GetPath() => $"{_parentBuilder.GetPath()}/{{SegmentName}}";

""");
        }

        writer.WriteLine("""

public IOpenApiClient Client => _parentBuilder.Client;

""");

        Properties.ForEach(p => p.Write(writer));
        Operations.ForEach(op => op.Write(writer));

        writer.Unindent();
        writer.WriteLine("}");
    }
}
