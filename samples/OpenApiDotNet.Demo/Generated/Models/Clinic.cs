namespace PetStore.Models;

/// <summary>
/// Tests: component object schema with explicit $id; exercises the schema.Id code path in GetSchemaName
/// </summary>
public class Clinic
{
    /// <summary>
    /// Name of the clinic
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
public required string Name { get; set; }
    
    /// <summary>
    /// Address of the clinic
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("address")]
public string? Address { get; set; }
    
}
