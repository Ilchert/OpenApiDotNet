using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PetStore;

public class PhotosIdBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;
    private readonly Guid _photoId;

#pragma warning disable CS8618
    protected PhotosIdBuilder() { }
#pragma warning restore CS8618

    public PhotosIdBuilder(IOpenApiBuilder parentBuilder, Guid photoId)
    {
        _parentBuilder = parentBuilder;
        _photoId = photoId;
    }

    public IOpenApiClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/{_photoId}";

    /// <summary>
    /// Tests: multiple path parameters (int64 + uuid); inline object response schema generating a nested class inside the builder
    /// </summary>
    public virtual async Task<GetResponse> Get(CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await response.Content.ReadFromJsonAsync<GetResponse>(Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new InvalidOperationException($"Response from {url} is null");
    }

    public class GetResponse
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("caption")]
        public string? Caption { get; set; }

    }

}
