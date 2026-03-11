#nullable enable

namespace PetStore.Builders;

public partial class OwnersBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected OwnersBuilder() { }
    #pragma warning restore CS8618

    public OwnersBuilder(IOpenApiBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public string GetPath() => $"{_parentBuilder.GetPath()}/owners";


    public IOpenApiClient Client => _parentBuilder.Client;

    public virtual PetStore.Builders.Owners.IdBuilder this[string ownerId]
    {
        get => new(this, ownerId);
    }

}
