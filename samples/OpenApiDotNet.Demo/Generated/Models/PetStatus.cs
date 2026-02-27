using System.Text.Json.Serialization;

namespace PetStore.Models;

/// <summary>
/// Tests: component-level string enum; all member names map directly to PascalCase without JsonStringEnumMemberName (available, pending, sold)
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PetStatus
{
    [JsonStringEnumMemberName("available")]
    Available,

    [JsonStringEnumMemberName("pending")]
    Pending,

    [JsonStringEnumMemberName("sold")]
    Sold,

}
