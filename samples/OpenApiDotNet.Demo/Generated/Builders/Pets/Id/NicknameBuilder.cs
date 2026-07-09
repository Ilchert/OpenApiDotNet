#nullable enable

namespace PetStore.Builders.Pets.Id;

public partial class NicknameBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected NicknameBuilder() { }
    #pragma warning restore CS8618

    public NicknameBuilder(IOpenApiBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public string GetPath() => $"{_parentBuilder.GetPath()}/nickname";


    public IOpenApiClient Client => _parentBuilder.Client;

    /// <summary>
    /// Tests: nullable response type (type includes null); returns nullable string without throwing on null
    /// </summary>
    public virtual async System.Threading.Tasks.Task<string?> Get(System.Threading.CancellationToken cancellationToken = default)
    {
        var url = GetPath();

        var response = await Client.HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await System.Net.Http.Json.HttpContentJsonExtensions.ReadFromJsonAsync<string>(response.Content, Client.JsonOptions, cancellationToken);
    }

}
