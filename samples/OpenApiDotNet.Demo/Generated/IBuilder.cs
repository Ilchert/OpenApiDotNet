namespace PetStore;

/// <summary>
/// Base interface for all fluent API builders
/// </summary>
public interface IBuilder
{
    IClient Client { get; }
    string GetPath();
}
