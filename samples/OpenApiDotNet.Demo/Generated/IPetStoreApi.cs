namespace PetStore;

/// <summary>
/// A simple pet store API for testing
/// </summary>
public interface IPetStoreApi : IOpenApiClient
{
    OwnersBuilder Owners { get => new(this); }
    PetsBuilder Pets { get => new(this); }
    PhotosBuilder Photos { get => new(this); }
}
