using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    /// Get a pet by ID
    /// </summary>
    public virtual async Task<PetStore.Models.Pet> Get(CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PetStore.Models.Pet>(Client.JsonOptions, cancellationToken) ?? throw new InvalidOperationException("Response was null");
    }

    /// <summary>
    /// Delete a pet
    /// </summary>
    public virtual async Task Delete(CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.DeleteAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

}
