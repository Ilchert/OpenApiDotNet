#nullable enable

namespace PetStore.Builders.Owners.Id.Pets;

public partial class IdBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected IdBuilder() { }
    #pragma warning restore CS8618

    private readonly long _petId;

    public IdBuilder(IOpenApiBuilder parentBuilder, long petId)
    {
        _parentBuilder = parentBuilder;
        _petId = petId;
    }

    public string GetPath() => $"{_parentBuilder.GetPath()}/{System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(_petId, Client.JsonOptions).Trim('"'))}";

    public IOpenApiClient Client => _parentBuilder.Client;

    /// <summary>
    /// Tests: nested resource path with two path parameters of different types (string ownerId, int64 petId); $ref response body
    /// </summary>
    public virtual async System.Threading.Tasks.Task<PetStore.Models.Pet> Get(System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<PetStore.Models.Pet>(response.Content, Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new System.InvalidOperationException($"Response from {url} is null");
    }

}
