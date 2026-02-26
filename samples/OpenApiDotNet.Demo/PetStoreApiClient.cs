using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetStore;

/// <summary>
/// Concrete implementation of the generated IOpenApiClient interface for the Pet Store API.
/// </summary>
public class PetStoreApiClient : IPetStoreApi
{
    public HttpClient HttpClient { get; }
    public JsonSerializerOptions JsonOptions { get; }

    public PetStoreApiClient(HttpClient httpClient)
    {
        HttpClient = httpClient;
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}
