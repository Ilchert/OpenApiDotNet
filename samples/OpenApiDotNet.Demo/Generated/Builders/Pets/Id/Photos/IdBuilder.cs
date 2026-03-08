namespace PetStore.Builders.Pets.Id.Photos;

public class IdBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected IdBuilder() { }
    #pragma warning restore CS8618

    private readonly System.Guid _photoId;

    public IdBuilder(IOpenApiBuilder parentBuilder, System.Guid photoId)
    {
        _parentBuilder = parentBuilder;
        _photoId = photoId;
    }

    public string GetPath() => $"{_parentBuilder.GetPath()}/{_photoId}";

    public IOpenApiClient Client => _parentBuilder.Client;

    /// <summary>
    /// Tests: multiple path parameters (int64 + uuid); inline object response schema generating a nested class inside the builder
    /// </summary>
    public virtual async System.Threading.Tasks.Task<GetResponse> Get(System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<GetResponse>(response.Content, Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new System.InvalidOperationException($"Response from {url} is null");
    }

    public class GetResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string? Url { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("caption")]
        public string? Caption { get; set; }

    }
}
