namespace PetStore.Builders;

public partial class SearchBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected SearchBuilder() { }
    #pragma warning restore CS8618

    public SearchBuilder(IOpenApiBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public string GetPath() => $"{_parentBuilder.GetPath()}/search";


    public IOpenApiClient Client => _parentBuilder.Client;

    /// <summary>
    /// Tests: query parameter with untyped schema (maps to System.Object)
    /// </summary>
    public virtual async System.Threading.Tasks.Task<System.Collections.Generic.List<PetStore.Models.Pet>> Get(object filter, System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();
        var queryString = new System.Collections.Generic.List<string>();

        queryString.Add($"filter={System.Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(filter, Client.JsonOptions).Trim('"'))}");
        if (queryString.Count > 0)
            url += "?" + string.Join("&", queryString);

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<System.Collections.Generic.List<PetStore.Models.Pet>>(response.Content, Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new System.InvalidOperationException($"Response from {url} is null");
    }

}
