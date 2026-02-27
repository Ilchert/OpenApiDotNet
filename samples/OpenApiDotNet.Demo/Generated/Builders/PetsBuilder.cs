using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PetStore;

public class PetsBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

#pragma warning disable CS8618
    protected PetsBuilder() { }
#pragma warning restore CS8618

    public PetsBuilder(IOpenApiBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public IOpenApiClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/pets";

    public virtual PetsIdBuilder this[long petId]
    {
        get => new(this, petId);
    }

    public virtual CountBuilder Count => new(this);

    /// <summary>
    /// Tests: GET operation; optional query params of type int32, string array, and enum $ref; array of $ref response body
    /// </summary>
    public virtual async Task<List<PetStore.Models.Pet>> Get(int? limit = default, List<string>? tags = default, PetStore.Models.PetStatus? status = default, CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var queryString = new List<string>();
        if (limit is {} limitValue)
            queryString.Add($"limit={Uri.EscapeDataString(limitValue.ToString())}");
        if (tags != null)
            foreach (var item in tags)
                queryString.Add($"tags={Uri.EscapeDataString(item.ToString())}");
        if (status is {} statusValue)
            queryString.Add($"status={Uri.EscapeDataString(statusValue.ToString())}");
        if (queryString.Count > 0)
            url += "?" + string.Join("&", queryString);

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await response.Content.ReadFromJsonAsync<List<PetStore.Models.Pet>>(Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new InvalidOperationException($"Response from {url} is null");
    }

    /// <summary>
    /// Tests: POST operation; required $ref request body with default body param name; $ref response body
    /// </summary>
    public virtual async Task<PetStore.Models.Pet> Post(PetStore.Models.NewPet request, CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.PostAsJsonAsync(url, request, Client.JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await response.Content.ReadFromJsonAsync<PetStore.Models.Pet>(Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new InvalidOperationException($"Response from {url} is null");
    }

}
