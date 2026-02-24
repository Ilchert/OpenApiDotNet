using System.Text.Json.Serialization;

namespace OpenApiDotNet;

public class GenerationConfig
{
    public const string FileName = ".openapidotnet.json";

    [JsonPropertyName("openApiFile")]
    public string OpenApiFile { get; set; } = string.Empty;

    [JsonPropertyName("outputDirectory")]
    public string OutputDirectory { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [JsonPropertyName("overlayFiles")]
    public List<string> OverlayFiles { get; set; } = [];
}
