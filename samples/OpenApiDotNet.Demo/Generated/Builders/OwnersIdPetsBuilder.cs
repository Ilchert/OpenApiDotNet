namespace PetStore.Builders;

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

public string GetPath() => $"{_parentBuilder.GetPath()}/pets";

                
public IOpenApiClient Client => _parentBuilder.Client;
            
    public virtual PetStore.Builders.OwnersIdPetsIdBuilder this[long petId]
    {
        get => new(this, petId);
    }
    
}
