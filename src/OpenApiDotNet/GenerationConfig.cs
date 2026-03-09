using System.Text.Json.Serialization;

namespace OpenApiDotNet;

public class GenerationConfig
{
    public const string FileName = ".openapidotnet.json";

    [JsonPropertyName("openApiFile")]
    [JsonConverter(typeof(UnifiedPathJsonConverter))]
    public string OpenApiFile { get; set; } = string.Empty;

    [JsonPropertyName("outputDirectory")]
    [JsonConverter(typeof(UnifiedPathJsonConverter))]
    public string OutputDirectory { get; set; } = string.Empty;

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = string.Empty;

    [JsonPropertyName("overlayFiles")]
    [JsonConverter(typeof(UnifiedPathListJsonConverter))]
    public List<string> OverlayFiles { get; set; } = [];

    [JsonPropertyName("namespacePrefix")]
    public string? NamespacePrefix { get; set; }

    [JsonPropertyName("clientName")]
    public string? ClientName { get; set; }

    [JsonPropertyName("typeMappings")]
    public Dictionary<string, string>? TypeMappings { get; set; }

    [JsonPropertyName("generatedFiles")]
    [JsonConverter(typeof(UnifiedPathListJsonConverter))]
    public List<string>? GeneratedFiles { get; set; }
}
