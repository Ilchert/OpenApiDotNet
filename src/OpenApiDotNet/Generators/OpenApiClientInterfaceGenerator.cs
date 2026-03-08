namespace OpenApiDotNet.Generators;

internal class OpenApiClientInterfaceGenerator : BaseGenerator
{
    public override GeneratedTypeInfo TypeInfo { get; }

    public OpenApiClientInterfaceGenerator(GeneratorContext context) : base(context)
    {
        TypeInfo = new GeneratedTypeInfo(context.DefaultNamespace, "IOpenApiClient");
    }

    public override void Write(CodeWriter writer)
    {
        writer.WriteLine("""
/// <summary>
/// Base interface for all OpenAPI clients
/// </summary>
public interface IOpenApiClient : IOpenApiBuilder
{
    System.Net.Http.HttpClient HttpClient { get; }
    System.Text.Json.JsonSerializerOptions JsonOptions { get; }
    IOpenApiClient IOpenApiBuilder.Client => this;
    string IOpenApiBuilder.GetPath() => "";
}
""");
    }
}
