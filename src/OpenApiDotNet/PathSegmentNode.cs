using Microsoft.OpenApi;
using OpenApiDotNet.Generators;

namespace OpenApiDotNet;

/// <summary>
/// Represents a segment in the API path tree. Each node corresponds to a static segment
/// (e.g., "pets") or a parameterized segment (e.g., "{petId}").
/// </summary>
internal class PathSegmentNode
{
    public string SegmentName { get; set; } = "";
    public bool IsParameter { get; set; }
    public string? ParameterName { get; set; }
    public IOpenApiSchema? ParameterSchema { get; set; }
    public Dictionary<string, PathSegmentNode> Children { get; } = new();
    public List<(HttpMethod Method, OpenApiOperation Operation)> Operations { get; } = [];
    public GeneratedTypeInfo BuilderName { get; set; } = new("", "");
}
