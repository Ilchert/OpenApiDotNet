namespace PetStore.Models;

/// <summary>
/// Tests: component object schema with an inline enum property (priority) generating a nested enum type inside the model class; inline object property (location) generating a nested class inside the model class
/// </summary>
public class PetReport
{
    /// <summary>
    /// ID of the reported pet
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("petId")]
    public long? PetId { get; set; }

    /// <summary>
    /// Priority level of the report
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("priority")]
    public PetReportPriority? Priority { get; set; }

    /// <summary>
    /// Priority level of the report
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
    public enum PetReportPriority
    {
        [System.Text.Json.Serialization.JsonStringEnumMemberName("low")]
        Low,

        [System.Text.Json.Serialization.JsonStringEnumMemberName("medium")]
        Medium,

        [System.Text.Json.Serialization.JsonStringEnumMemberName("high")]
        High,

    }
    /// <summary>
    /// Location where the pet was found
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("location")]
    public PetReportLocation? Location { get; set; }

    /// <summary>
    /// Location where the pet was found
    /// </summary>
    public class PetReportLocation
    {
        [System.Text.Json.Serialization.JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

    }
}
