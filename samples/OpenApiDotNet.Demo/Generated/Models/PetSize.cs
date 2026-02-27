using System.Text.Json.Serialization;

namespace PetStore.Models;

/// <summary>
/// Tests: component-level string enum with a hyphenated member name (extra-large) that requires JsonStringEnumMemberName to preserve the wire value
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PetSize
{
    [JsonStringEnumMemberName("small")]
    Small,

    [JsonStringEnumMemberName("medium")]
    Medium,

    [JsonStringEnumMemberName("large")]
    Large,

    [JsonStringEnumMemberName("extra-large")]
    ExtraLarge,

}
