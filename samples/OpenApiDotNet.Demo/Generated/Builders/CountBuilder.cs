using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PetStore;

public class CountBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

#pragma warning disable CS8618
    protected CountBuilder() { }
#pragma warning restore CS8618

    public CountBuilder(IOpenApiBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public IOpenApiClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/count";

    /// <summary>
    /// Tests: value-type return (int32); required reference-type query param (string); required value-type query param (int32); optional value-type query param (bool)
    /// </summary>
    public virtual async Task<int> Get(string species, int minAge, bool? vaccinated = default, CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var queryString = new List<string>();
        queryString.Add($"species={Uri.EscapeDataString(species.ToString())}");
        queryString.Add($"minAge={Uri.EscapeDataString(minAge.ToString())}");
        if (vaccinated is {} vaccinatedValue)
            queryString.Add($"vaccinated={Uri.EscapeDataString(vaccinatedValue.ToString())}");
        if (queryString.Count > 0)
            url += "?" + string.Join("&", queryString);

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var deserializedResponse = await response.Content.ReadFromJsonAsync<int>(Client.JsonOptions, cancellationToken);
        if (deserializedResponse is { } deserializedResponseValue)
            return deserializedResponseValue;
        throw new InvalidOperationException($"Response from {url} is null");
    }

}
