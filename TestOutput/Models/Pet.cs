using System.Text.Json.Serialization;
using NodaTime;

namespace TestClient.Models;

public class Pet
{
    /// <summary>
    /// Unique identifier for the pet
    /// </summary>
    [JsonPropertyName("id")]
    public long Id { get; set; }

    /// <summary>
    /// Name of the pet
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Tag for the pet
    /// </summary>
    [JsonPropertyName("tag")]
    public string? Tag { get; set; }

    /// <summary>
    /// Birth date of the pet
    /// </summary>
    [JsonPropertyName("birthDate")]
    public LocalDate? BirthDate { get; set; }

    /// <summary>
    /// When the pet was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public Instant? CreatedAt { get; set; }

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

}
