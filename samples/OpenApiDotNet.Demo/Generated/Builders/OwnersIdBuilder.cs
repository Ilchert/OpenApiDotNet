namespace PetStore.Builders;

public class OwnersIdBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected OwnersIdBuilder() { }
    #pragma warning restore CS8618

    private readonly string _ownerId;

public OwnersIdBuilder(IOpenApiBuilder parentBuilder, string ownerId)
{
    _parentBuilder = parentBuilder;
    _ownerId = ownerId;
}

public string GetPath() => $"{_parentBuilder.GetPath()}/{_ownerId}";
                
public IOpenApiClient Client => _parentBuilder.Client;
            
    public virtual PetStore.Builders.OwnersIdPetsBuilder Pets => new(this);
    
}
