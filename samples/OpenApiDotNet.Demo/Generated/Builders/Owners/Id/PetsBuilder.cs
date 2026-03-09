namespace PetStore.Builders.Owners.Id;

public partial class PetsBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected PetsBuilder() { }
    #pragma warning restore CS8618

    public PetsBuilder(IOpenApiBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public string GetPath() => $"{_parentBuilder.GetPath()}/pets";


    public IOpenApiClient Client => _parentBuilder.Client;

    public virtual PetStore.Builders.Owners.Id.Pets.IdBuilder this[long petId]
    {
        get => new(this, petId);
    }

}
