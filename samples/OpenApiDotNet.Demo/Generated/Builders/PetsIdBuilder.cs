namespace PetStore.Builders;

public class PetsIdBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected PetsIdBuilder() { }
    #pragma warning restore CS8618

    private readonly long _petId;

public PetsIdBuilder(IOpenApiBuilder parentBuilder, long petId)
{
    _parentBuilder = parentBuilder;
    _petId = petId;
}

public string GetPath() => $"{_parentBuilder.GetPath()}/{_petId}";
                
public IOpenApiClient Client => _parentBuilder.Client;
            
    public virtual PetStore.Builders.PhotosBuilder Photos => new(this);
    
    /// <summary>
    /// Tests: GET operation; single int64 path parameter; $ref response body
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
    
    /// <summary>
    /// Tests: PUT operation; x-bodyName extension overriding default body parameter name; $ref request and response bodies
    /// </summary>
    public virtual async System.Threading.Tasks.Task<PetStore.Models.Pet> Put(PetStore.Models.NewPet updatedPet, System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();
        
        var response = await System.Net.Http.Json.HttpClientJsonExtensions.PutAsJsonAsync(Client.HttpClient, url, updatedPet, Client.JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<PetStore.Models.Pet>(response.Content, Client.JsonOptions, cancellationToken);
if (deserializedResponse is { } deserializedResponseValue)
    return deserializedResponseValue;
throw new System.InvalidOperationException($"Response from {url} is null");
    }
    
    /// <summary>
    /// Tests: DELETE operation; single int64 path parameter; void return (no response body)
    /// </summary>
    public virtual async System.Threading.Tasks.Task Delete(System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();
        
        var response = await Client.HttpClient.DeleteAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
    
}
