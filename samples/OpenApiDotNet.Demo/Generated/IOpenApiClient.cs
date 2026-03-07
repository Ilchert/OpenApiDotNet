namespace PetStore;

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
