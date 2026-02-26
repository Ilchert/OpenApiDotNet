using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime.Serialization.SystemTextJson;

namespace PetStore;

/// <summary>
/// JSON serialization configuration with NodaTime support
/// </summary>
public static class JsonConfiguration
{
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        // Add string enum converter for enum serialization
        options.Converters.Add(new JsonStringEnumConverter());

        // Configure NodaTime converters for date/time types
        options.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);

        return options;
    }
}
