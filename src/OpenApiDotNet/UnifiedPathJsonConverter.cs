using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenApiDotNet;

internal class UnifiedPathJsonConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString()?.Replace('/', Path.DirectorySeparatorChar);

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Replace('\\', '/'));
}

internal class UnifiedPathListJsonConverter : JsonConverter<List<string>>
{
    private static readonly UnifiedPathJsonConverter s_pathConverter = new();

    public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var list = new List<string>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            var value = s_pathConverter.Read(ref reader, typeof(string), options);
            if (value is not null)
                list.Add(value);
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
            s_pathConverter.Write(writer, item, options);
        writer.WriteEndArray();
    }
}
