using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PetStore;

public class PhotosBuilder : IBuilder
{
    private readonly IBuilder _parentBuilder;

#pragma warning disable CS8618
    protected PhotosBuilder() { }
#pragma warning restore CS8618

    public PhotosBuilder(IBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public IClient Client => _parentBuilder.Client;
    public string GetPath() => $"{_parentBuilder.GetPath()}/photos";

    public virtual PhotosIdBuilder this[Guid photoId]
    {
        get => new(this, photoId);
    }

}
