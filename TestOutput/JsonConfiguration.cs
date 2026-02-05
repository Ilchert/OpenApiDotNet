using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace TestClient;

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

        // Configure NodaTime converters for date/time types
        options.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

        return options;
    }
}
