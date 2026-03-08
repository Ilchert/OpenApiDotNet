namespace PetStore.Builders;

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

    public string GetPath() => $"{_parentBuilder.GetPath()}/pets";


    public IOpenApiClient Client => _parentBuilder.Client;

    public virtual PetStore.Builders.Pets.IdBuilder this[long petId]
    {
        get => new(this, petId);
    }

    public virtual PetStore.Builders.Pets.CountBuilder Count => new(this);

    /// <summary>
    /// Tests: GET operation; optional query params of type int32, string array, and enum $ref; array of $ref response body
    /// </summary>
    public virtual async System.Threading.Tasks.Task<System.Collections.Generic.List<PetStore.Models.Pet>> Get(int? limit = default, System.Collections.Generic.List<string>? tags = default, PetStore.Models.PetStatus? status = default, System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();
        var queryString = new System.Collections.Generic.List<string>();

        if (limit is {} limitValue)
            queryString.Add($"limit={System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(limitValue, Client.JsonOptions).Trim('"'))}");
        if (tags != null)
            foreach (var item in tags)
                queryString.Add($"tags={System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(item, Client.JsonOptions).Trim('"'))}");

        if (status is {} statusValue)
            queryString.Add($"status={System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(statusValue, Client.JsonOptions).Trim('"'))}");
        if (queryString.Count > 0)
            url += "?" + string.Join("&", queryString);

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<System.Collections.Generic.List<PetStore.Models.Pet>>(response.Content, Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new System.InvalidOperationException($"Response from {url} is null");
    }

    /// <summary>
    /// Tests: POST operation; required $ref request body with default body param name; $ref response body
    /// </summary>
    public virtual async System.Threading.Tasks.Task<PetStore.Models.Pet> Post(PetStore.Models.NewPet request, System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await System.Net.Http.Json.HttpClientJsonExtensions.PostAsJsonAsync(Client.HttpClient, url, request, Client.JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<PetStore.Models.Pet>(response.Content, Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new System.InvalidOperationException($"Response from {url} is null");
    }

}
