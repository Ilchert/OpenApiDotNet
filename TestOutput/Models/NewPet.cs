using System.Text.Json.Serialization;
using NodaTime;

namespace TestClient.Models;

public class NewPet
{
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

}
