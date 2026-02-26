using Moq;
using System.Net.Http.Json;

namespace OpenApiDotNet.Tests;

public class MockTests
{
    [Fact]
    public async Task Test()
    {
        var t = new Mock<IClient>();
        t.Setup(p => p.Pets[123].ListPets(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Pet> { new Pet() });
        var result = await t.Object.Pets[123].ListPets(default);

    }


}

public interface IBuilder
{
    public IClient Client { get; }
    public string GetPath();
}

public class PetsBuilder : IBuilder
{
    private readonly IBuilder _parentBuilder;

#pragma warning disable CS8618
    protected PetsBuilder() // for mocking purposes only, not intended for public use
#pragma warning restore CS8618
    {
    }

    public PetsBuilder(IBuilder parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    public virtual PetsIdBuilder this[long petId]
    {
        get => new(_parentBuilder, petId);

    }
    public IClient Client => _parentBuilder.Client;
    public string GetPath() => "pets";
}

public class PetsIdBuilder : IBuilder
{
    private readonly IBuilder _builder;
    private readonly long _petId;

    public PetsIdBuilder()
    {
    }
    public PetsIdBuilder(IBuilder builder, long petId)
    {
        _builder = builder;
        _petId = petId;
    }

    public IClient Client => _builder.Client;

    public string GetPath() => $"{_builder.GetPath()}/{_petId}";

    public virtual PhotosBuilder Photos => new PhotosBuilder(this);

    public virtual async Task<List<Pet>> ListPets(CancellationToken cancellationToken) // Get
    {
        var url = GetPath();
        return await Client.HttpClient.GetFromJsonAsync<List<Pet>>(url, cancellationToken) ?? throw new InvalidOperationException("Null response"); ;
    }
}

public class PhotosBuilder(IBuilder parentBuilder) : IBuilder
{

    public PhotosIdBuilder this[long photoId]
    {
        get => new PhotosIdBuilder(this, photoId);
    }
    public IClient Client => parentBuilder.Client;
    public string GetPath() => "photos";
}

public class PhotosIdBuilder(IBuilder builder, long photoId) : IBuilder
{
    public IClient Client => builder.Client;
    public string GetPath() => $"{builder.GetPath()}/{photoId}";
    public async Task<List<Pet>> GetPetPhoto(CancellationToken cancellationToken)
    {
        return null!;
    }
}
public interface IClient : IBuilder
{

    public PetsBuilder Pets { get => new(this); }
    public HttpClient HttpClient { get; }


}



public class Pet { }