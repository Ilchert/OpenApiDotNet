namespace PetStore.Builders;

public class ClinicsBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected ClinicsBuilder() { }
    #pragma warning restore CS8618

    public ClinicsBuilder(IOpenApiBuilder parentBuilder)
{
    _parentBuilder = parentBuilder;
}

public string GetPath() => $"{_parentBuilder.GetPath()}/clinics";

                
public IOpenApiClient Client => _parentBuilder.Client;
            
    /// <summary>
    /// Tests: component schema with explicit $id exercising the schema.Id code path in GetSchemaName; array of $ref response body
    /// </summary>
    public virtual async System.Threading.Tasks.Task<System.Collections.Generic.List<PetStore.Models.Clinic>> Get(System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();
        
        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<System.Collections.Generic.List<PetStore.Models.Clinic>>(response.Content, Client.JsonOptions, cancellationToken);
if (deserializedResponse is { } deserializedResponseValue)
    return deserializedResponseValue;
throw new System.InvalidOperationException($"Response from {url} is null");
    }
    
}
