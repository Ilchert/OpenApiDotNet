using System.Text.Json;

namespace PetStore;

/// <summary>
/// A simple pet store API for testing
/// </summary>
public interface IClient : IBuilder
{
    HttpClient HttpClient { get; }
    JsonSerializerOptions JsonOptions { get; }
    OwnersBuilder Owners { get => new(this); }
    PetsBuilder Pets { get => new(this); }

    IClient IBuilder.Client => this;
    string IBuilder.GetPath() => "";
}
