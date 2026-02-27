using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PetStore;

public class PetsIdBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;
    private readonly long _petId;

#pragma warning disable CS8618
    protected PetsIdBuilder() { }
#pragma warning restore CS8618

    public PetsIdBuilder(IOpenApiBuilder parentBuilder, long petId)
    {
        _parentBuilder = parentBuilder;
        _petId = petId;
    }

    public IOpenApiClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/{_petId}";

    public virtual PhotosBuilder Photos => new(this);

    /// <summary>
    /// Tests: GET operation; single int64 path parameter; $ref response body
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

    /// <summary>
    /// Tests: PUT operation; x-bodyName extension overriding default body parameter name; $ref request and response bodies
    /// </summary>
    public virtual async Task<PetStore.Models.Pet> Put(PetStore.Models.NewPet updatedPet, CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.PutAsJsonAsync(url, updatedPet, Client.JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await response.Content.ReadFromJsonAsync<PetStore.Models.Pet>(Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new InvalidOperationException($"Response from {url} is null");
    }

    /// <summary>
    /// Tests: DELETE operation; single int64 path parameter; void return (no response body)
    /// </summary>
    public virtual async Task Delete(CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.DeleteAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

}
