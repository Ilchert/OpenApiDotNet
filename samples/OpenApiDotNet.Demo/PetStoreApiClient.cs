using System.Text.Json;

namespace PetStore;

/// <summary>
/// Concrete implementation of the generated IClient interface for the Pet Store API.
/// </summary>
public class PetStoreApiClient : IClient
{
    public HttpClient HttpClient { get; }
    public JsonSerializerOptions JsonOptions { get; }

    public PetStoreApiClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
        JsonOptions = JsonConfiguration.CreateOptions();
    }
}
