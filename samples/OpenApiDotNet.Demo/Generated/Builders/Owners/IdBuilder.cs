namespace PetStore.Builders.Owners;

public partial class IdBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected IdBuilder() { }
    #pragma warning restore CS8618

    private readonly string _ownerId;

    public IdBuilder(IOpenApiBuilder parentBuilder, string ownerId)
    {
        _parentBuilder = parentBuilder;
        _ownerId = ownerId;
    }

    public string GetPath() => $"{_parentBuilder.GetPath()}/{_ownerId}";

    public IOpenApiClient Client => _parentBuilder.Client;

    public virtual PetStore.Builders.Owners.Id.PetsBuilder Pets => new(this);

}
