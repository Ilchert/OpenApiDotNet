using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetStore;

public class PhotosIdBuilder : IBuilder
{
    private readonly IBuilder _parentBuilder;
    private readonly Guid _photoId;

#pragma warning disable CS8618
    protected PhotosIdBuilder() { }
#pragma warning restore CS8618

    public PhotosIdBuilder(IBuilder parentBuilder, Guid photoId)
    {
        _parentBuilder = parentBuilder;
        _photoId = photoId;
    }

    public IClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/{_photoId}";

    /// <summary>
    /// Get a specific photo of a pet
    /// </summary>
    public virtual async Task<GetResponse> Get(CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GetResponse>(Client.JsonOptions, cancellationToken) ?? throw new InvalidOperationException("Response was null");
    }

    public class GetResponse
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("caption")]
        public string? Caption { get; set; }

    }

}
