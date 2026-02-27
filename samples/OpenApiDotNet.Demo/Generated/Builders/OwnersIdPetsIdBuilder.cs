using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PetStore;

public class OwnersIdPetsIdBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;
    private readonly long _petId;

#pragma warning disable CS8618
    protected OwnersIdPetsIdBuilder() { }
#pragma warning restore CS8618

    public OwnersIdPetsIdBuilder(IOpenApiBuilder parentBuilder, long petId)
    {
        _parentBuilder = parentBuilder;
        _petId = petId;
    }

    public IOpenApiClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/{_petId}";

    /// <summary>
    /// Tests: nested resource path with two path parameters of different types (string ownerId, int64 petId); $ref response body
    /// </summary>
    public virtual async Task<PetStore.Models.Pet> Get(CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await response.Content.ReadFromJsonAsync<PetStore.Models.Pet>(Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new InvalidOperationException($"Response from {url} is null");
    }

}
