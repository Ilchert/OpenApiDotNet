using System.Net.Http.Json;
using System.Text.Json;
using NodaTime;
using TestClient.Models;

namespace TestClient;

/// <summary>
/// A simple pet store API for testing
/// </summary>
public class PetStoreAPIClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public PetStoreAPIClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _jsonOptions = JsonConfiguration.CreateOptions();
    }

    /// <summary>
    /// List all pets
    /// </summary>
    public async Task<List<Pet>> ListPetsAsync(int? limit, List<string>? tags, string? status, CancellationToken cancellationToken = default)
    {
        var url = "/pets";

        // Build query string with URL-encoded parameters
        var queryString = new List<string>();
        if (limit != null)
            queryString.Add($"limit={Uri.EscapeDataString(limit.ToString())}");
        if (tags != null)
            queryString.Add($"tags={Uri.EscapeDataString(tags.ToString())}");
        if (status != null)
            queryString.Add($"status={Uri.EscapeDataString(status.ToString())}");
        if (queryString.Any())
            url += "?" + string.Join("&", queryString);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Pet>>(_jsonOptions, cancellationToken) ?? throw new InvalidOperationException("Response was null");
    }

    /// <summary>
    /// Create a pet
    /// </summary>
    public async Task<Pet> CreatePetAsync(NewPet request, CancellationToken cancellationToken = default)
    {
        var url = "/pets";

        var response = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Pet>(_jsonOptions, cancellationToken) ?? throw new InvalidOperationException("Response was null");
    }

    /// <summary>
    /// Get a pet by ID
    /// </summary>
    public async Task<Pet> GetPetByIdAsync(long petId, CancellationToken cancellationToken = default)
    {
        // Build path with URL-encoded parameters
        var url = $"/pets/{Uri.EscapeDataString(petId.ToString())}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Pet>(_jsonOptions, cancellationToken) ?? throw new InvalidOperationException("Response was null");
    }

    /// <summary>
    /// Delete a pet
    /// </summary>
    public async Task<void> DeletePetAsync(long petId, CancellationToken cancellationToken = default)
    {
        // Build path with URL-encoded parameters
        var url = $"/pets/{Uri.EscapeDataString(petId.ToString())}";

        var response = await _httpClient.DeleteAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Get a specific photo of a pet
    /// </summary>
    public async Task<object> GetPetPhotoAsync(long petId, Guid photoId, CancellationToken cancellationToken = default)
    {
        // Build path with URL-encoded parameters
        var url = $"/pets/{Uri.EscapeDataString(petId.ToString())}/photos/{Uri.EscapeDataString(photoId.ToString())}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<object>(_jsonOptions, cancellationToken) ?? throw new InvalidOperationException("Response was null");
    }

    /// <summary>
    /// Get a specific pet for an owner
    /// </summary>
    public async Task<Pet> GetOwnerPetAsync(string ownerId, long petId, CancellationToken cancellationToken = default)
    {
        // Build path with URL-encoded parameters
        var url = $"/owners/{Uri.EscapeDataString(ownerId.ToString())}/pets/{Uri.EscapeDataString(petId.ToString())}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Pet>(_jsonOptions, cancellationToken) ?? throw new InvalidOperationException("Response was null");
    }

}
