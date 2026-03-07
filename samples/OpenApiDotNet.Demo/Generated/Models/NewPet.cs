namespace PetStore.Models;

/// <summary>
/// Tests: component object schema used as a $ref request body; one required string property; optional string and date properties
/// </summary>
public class NewPet
{
    /// <summary>
    /// Name of the pet
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
public required string Name { get; set; }
    
    /// <summary>
    /// Tag for the pet
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tag")]
public string? Tag { get; set; }
    
    /// <summary>
    /// Birth date of the pet
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("birthDate")]
public NodaTime.LocalDate? BirthDate { get; set; }
    
}
