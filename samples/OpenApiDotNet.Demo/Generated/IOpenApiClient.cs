using System.Text.Json;

namespace PetStore;

/// <summary>
/// A simple pet store API for testing
/// </summary>
public interface IOpenApiClient : IOpenApiBuilder
{
    HttpClient HttpClient { get; }
    JsonSerializerOptions JsonOptions { get; }
    OwnersBuilder Owners { get => new(this); }
    PetsBuilder Pets { get => new(this); }

    IOpenApiClient IOpenApiBuilder.Client => this;
    string IOpenApiBuilder.GetPath() => "";
}
