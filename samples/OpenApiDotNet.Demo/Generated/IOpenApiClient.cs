using System.Text.Json;

namespace PetStore;

/// <summary>
/// Base interface for all OpenAPI clients
/// </summary>
public interface IOpenApiClient : IOpenApiBuilder
{
    HttpClient HttpClient { get; }
    JsonSerializerOptions JsonOptions { get; }

    IOpenApiClient IOpenApiBuilder.Client => this;
    string IOpenApiBuilder.GetPath() => "";
}
