namespace PetStore.Builders;

public class PhotosBuilder : IOpenApiBuilder
{
    private readonly IOpenApiBuilder _parentBuilder;

    #pragma warning disable CS8618
    protected PhotosBuilder() { }
    #pragma warning restore CS8618

    public PhotosBuilder(IOpenApiBuilder parentBuilder)
{
    _parentBuilder = parentBuilder;
}

public string GetPath() => $"{_parentBuilder.GetPath()}/photos";

                
public IOpenApiClient Client => _parentBuilder.Client;
            
    public virtual PetStore.Builders.PhotosIdBuilder this[System.Guid photoId]
    {
        get => new(this, photoId);
    }
    
}
