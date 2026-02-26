using System.Text.Json.Serialization;

namespace PetStore.Models;

/// <summary>
/// The status of a pet in the store
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
