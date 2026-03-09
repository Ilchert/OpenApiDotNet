namespace PetStore;

/// <summary>
/// Base interface for all fluent API builders
/// </summary>
public interface IOpenApiBuilder
{
    IOpenApiClient Client { get; }
    string GetPath();
}
