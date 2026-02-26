using System.Text.Json.Serialization;

namespace PetStore.Models;

/// <summary>
/// The size category of a pet
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
