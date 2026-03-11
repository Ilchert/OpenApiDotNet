#nullable enable

namespace PetStore.Models;

/// <summary>
/// Tests: component-level string enum; all member names map directly to PascalCase without JsonStringEnumMemberName (available, pending, sold)
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum PetStatus
{
    [System.Text.Json.Serialization.JsonStringEnumMemberName("available")]
    Available,

    [System.Text.Json.Serialization.JsonStringEnumMemberName("pending")]
    Pending,

    [System.Text.Json.Serialization.JsonStringEnumMemberName("sold")]
    Sold,

}
