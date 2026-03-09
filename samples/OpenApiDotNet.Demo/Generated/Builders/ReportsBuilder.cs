namespace PetStore.Builders;

public partial class ReportsBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected ReportsBuilder() { }
    #pragma warning restore CS8618

    public ReportsBuilder(IOpenApiBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public string GetPath() => $"{_parentBuilder.GetPath()}/reports";


    public IOpenApiClient Client => _parentBuilder.Client;

    /// <summary>
    /// Tests: inline object request body (with required property and inline enum property) generating a nested request class inside the builder; inline object response body generating a nested response class inside the builder
    /// </summary>
    public virtual async System.Threading.Tasks.Task<PostResponse> Post(PostRequest request, System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(Client.HttpClient, url, request, Client.JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<PostResponse>(response.Content, Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new System.InvalidOperationException($"Response from {url} is null");
    }

    public partial class PostRequest
    {
        /// <summary>
        /// Report title
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public required string Title { get; set; }

        /// <summary>
        /// Severity level
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("severity")]
        public PostRequestSeverity? Severity { get; set; }

        /// <summary>
        /// Severity level
        /// </summary>
        [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
        public enum PostRequestSeverity
        {
            [System.Text.Json.Serialization.JsonStringEnumMemberName("low")]
            Low,

            [System.Text.Json.Serialization.JsonStringEnumMemberName("medium")]
            Medium,

            [System.Text.Json.Serialization.JsonStringEnumMemberName("high")]
            High,

        }
    }
    public partial class PostResponse
    {
        /// <summary>
        /// Report ID
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Report status
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string? Status { get; set; }

    }
}
