using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PetStore;

public class ReportsBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

#pragma warning disable CS8618
    protected ReportsBuilder() { }
#pragma warning restore CS8618

    public ReportsBuilder(IOpenApiBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public IOpenApiClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/reports";

    /// <summary>
    /// Tests: inline object request body (with required property and inline enum property) generating a nested request class inside the builder; inline object response body generating a nested response class inside the builder
    /// </summary>
    public virtual async Task<PostResponse> Post(PostRequest request, CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.PostAsJsonAsync(url, request, Client.JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await response.Content.ReadFromJsonAsync<PostResponse>(Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new InvalidOperationException($"Response from {url} is null");
    }

    public class PostRequest
    {
        [JsonPropertyName("title")]
        public required string Title { get; set; }

        [JsonPropertyName("severity")]
        public string? Severity { get; set; }

    }

    public class PostResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

    }

}
