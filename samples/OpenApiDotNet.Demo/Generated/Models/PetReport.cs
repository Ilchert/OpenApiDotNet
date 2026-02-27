using System.Text.Json.Serialization;

namespace PetStore.Models;

/// <summary>
/// Tests: component object schema with an inline enum property (priority) generating a nested enum type inside the model class; inline object property (location) generating a nested class inside the model class
/// </summary>
public class PetReport
{
    /// <summary>
    /// ID of the reported pet
    /// </summary>
    [JsonPropertyName("petId")]
    public long? PetId { get; set; }

    /// <summary>
    /// Priority level of the report
    /// </summary>
    [JsonPropertyName("priority")]
    public PetReportPriority? Priority { get; set; }

    /// <summary>
    /// Location where the pet was found
    /// </summary>
    [JsonPropertyName("location")]
    public PetReportLocation? Location { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PetReportPriority
    {
        [JsonStringEnumMemberName("low")]
        Low,

        [JsonStringEnumMemberName("medium")]
        Medium,

        [JsonStringEnumMemberName("high")]
        High,

    }

    public class PetReportLocation
    {
        [JsonPropertyName("latitude")]
        public double? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; set; }

    }

}
