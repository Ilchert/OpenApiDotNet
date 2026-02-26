using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetStore;

public class OwnersIdBuilder : IBuilder
{
    private readonly IBuilder _parentBuilder;
    private readonly string _ownerId;

#pragma warning disable CS8618
    protected OwnersIdBuilder() { }
#pragma warning restore CS8618

    public OwnersIdBuilder(IBuilder parentBuilder, string ownerId)
    {
        _parentBuilder = parentBuilder;
        _ownerId = ownerId;
    }

    public IClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/{_ownerId}";

    public OwnersIdPetsBuilder Pets => new(this);

}
