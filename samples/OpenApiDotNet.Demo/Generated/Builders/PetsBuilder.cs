using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// List all pets
    /// </summary>
    public virtual async Task<List<PetStore.Models.Pet>> Get(int? limit = default, List<string>? tags = default, PetStore.Models.PetStatus? status = default, CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var queryString = new List<string>();
        if (limit != null)
            queryString.Add($"limit={Uri.EscapeDataString(limit.ToString())}");
        if (tags != null)
            foreach (var item in tags)
                queryString.Add($"tags={Uri.EscapeDataString(item.ToString())}");
        if (status != null)
            queryString.Add($"status={Uri.EscapeDataString(status.ToString())}");
        if (queryString.Count > 0)
            url += "?" + string.Join("&", queryString);

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<PetStore.Models.Pet>>(Client.JsonOptions, cancellationToken) ?? throw new InvalidOperationException("Response was null");
    }

    /// <summary>
    /// Create a pet
    /// </summary>
    public virtual async Task<PetStore.Models.Pet> Post(PetStore.Models.NewPet request, CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.PostAsJsonAsync(url, request, Client.JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PetStore.Models.Pet>(Client.JsonOptions, cancellationToken) ?? throw new InvalidOperationException("Response was null");
    }

}
