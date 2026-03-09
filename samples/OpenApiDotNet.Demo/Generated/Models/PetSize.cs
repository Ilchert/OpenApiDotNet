namespace PetStore.Models;

/// <summary>
/// Tests: component-level string enum with a hyphenated member name (extra-large) that requires JsonStringEnumMemberName to preserve the wire value
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
public enum PetSize
{
    [System.Text.Json.Serialization.JsonStringEnumMemberName("small")]
    Small,

    [System.Text.Json.Serialization.JsonStringEnumMemberName("medium")]
    Medium,

    [System.Text.Json.Serialization.JsonStringEnumMemberName("large")]
    Large,

    [System.Text.Json.Serialization.JsonStringEnumMemberName("extra-large")]
    ExtraLarge,

}
