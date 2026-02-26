using System.Text.Json.Serialization;

namespace PetStore.Models;

public class NewPet
{
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

}
