using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetStore;

public class OwnersIdPetsBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

#pragma warning disable CS8618
    protected OwnersIdPetsBuilder() { }
#pragma warning restore CS8618

    public OwnersIdPetsBuilder(IOpenApiBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public IOpenApiClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/pets";

    public virtual OwnersIdPetsIdBuilder this[long petId]
    {
        get => new(this, petId);
    }

}
