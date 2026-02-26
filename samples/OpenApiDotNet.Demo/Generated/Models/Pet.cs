using System.Text.Json.Serialization;

namespace PetStore.Models;

public class Pet
{
    /// <summary>
    /// Unique identifier for the pet
    /// </summary>
    [JsonPropertyName("id")]
    public required long Id { get; set; }

    /// <summary>
    /// Name of the pet
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Tag for the pet
    /// </summary>
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    /// <summary>
    /// Birth date of the pet
    /// </summary>
    [JsonPropertyName("birthDate")]
    public NodaTime.LocalDate? BirthDate { get; set; }

    /// <summary>
    /// When the pet was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public NodaTime.Instant? CreatedAt { get; set; }

    /// <summary>
    /// Whether the pet is vaccinated
    /// </summary>
    [JsonPropertyName("vaccinated")]
    public bool? Vaccinated { get; set; }

    /// <summary>
    /// Weight in kg
    /// </summary>
    [JsonPropertyName("weight")]
    public double? Weight { get; set; }

    /// <summary>
    /// The status of a pet in the store
    /// </summary>
    [JsonPropertyName("status")]
    public PetStore.Models.PetStatus? Status { get; set; }

    /// <summary>
    /// The size category of a pet
    /// </summary>
    [JsonPropertyName("size")]
    public PetStore.Models.PetSize? Size { get; set; }

}
