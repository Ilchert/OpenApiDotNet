using System.Text.Json.Serialization;

namespace PetStore.Models;

/// <summary>
/// Tests: component object schema; required properties (id: int64, name: string); optional properties of all scalar types (string, date, date-time, bool, double); optional enum $ref properties (PetStatus, PetSize)
/// </summary>
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
    /// Tests: component-level string enum; all member names map directly to PascalCase without JsonStringEnumMemberName (available, pending, sold)
    /// </summary>
    [JsonPropertyName("status")]
    public PetStore.Models.PetStatus? Status { get; set; }

    /// <summary>
    /// Tests: component-level string enum with a hyphenated member name (extra-large) that requires JsonStringEnumMemberName to preserve the wire value
    /// </summary>
    [JsonPropertyName("size")]
    public PetStore.Models.PetSize? Size { get; set; }

}
