#nullable enable

namespace PetStore.Builders.Pets;

public partial class CountBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected CountBuilder() { }
    #pragma warning restore CS8618

    public CountBuilder(IOpenApiBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public string GetPath() => $"{_parentBuilder.GetPath()}/count";


    public IOpenApiClient Client => _parentBuilder.Client;

    /// <summary>
    /// Tests: value-type return (int32); required reference-type query param (string); required value-type query param (int32); optional value-type query param (bool)
    /// </summary>
    public virtual async System.Threading.Tasks.Task<int> Get(string species, int minAge, bool? vaccinated = default, System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();
        var queryString = new System.Collections.Generic.List<string>();

        queryString.Add($"species={System.Uri.EscapeDataString(species)}");
        queryString.Add($"minAge={System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(minAge, Client.JsonOptions).Trim('"'))}");
        if (vaccinated is {} vaccinatedValue)
            queryString.Add($"vaccinated={System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(vaccinatedValue, Client.JsonOptions).Trim('"'))}");
        if (queryString.Count > 0)
            url += "?" + string.Join("&", queryString);

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<int>(response.Content, Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new System.InvalidOperationException($"Response from {url} is null");
    }

}
