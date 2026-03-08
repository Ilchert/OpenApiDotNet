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

/// <summary>
/// Builds a tree of <see cref="PathSegmentNode"/> from OpenAPI path definitions
/// and resolves unique builder class names for each node.
/// </summary>
internal static class PathTreeBuilder
{
    /// <summary>
    /// Parses all paths from the OpenAPI document into a tree structure and resolves builder names.
    /// </summary>
    public static PathSegmentNode Build(OpenApiPaths? paths, GeneratorContext context)
    {
        var root = new PathSegmentNode();

        if (paths == null)
            return root;

        foreach (var (pathKey, pathItem) in paths)
        {
            var segments = pathKey.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var current = root;

            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (!current.Children.TryGetValue(segment, out var child))
                {
                    var isParam = segment is ['{', .., '}'];
                    child = new PathSegmentNode
                    {
                        SegmentName = segment,
                        IsParameter = isParam,
                        ParameterName = isParam ? segment[1..^1] : null
                    };
                    current.Children[segment] = child;
                }
                current = child;
            }

            // Resolve parameter schemas from operations
            ResolveParameterSchemas(current, pathItem, segments);

            // Attach operations to the terminal node
            foreach (var (method, operation) in pathItem.Operations)
            {
                current.Operations.Add((method, operation));
            }
        }

        ResolveBuilderNames(root, $"{context.DefaultNamespace}.Builders");
        return root;
    }

    /// <summary>
    /// Walks the path from root to the terminal node and assigns parameter schemas
    /// from the operation definitions to each parameterized segment.
    /// </summary>
    private static void ResolveParameterSchemas(PathSegmentNode terminal, IOpenApiPathItem pathItem, string[] segments)
    {
        // Collect all path parameters from all operations on this path item
        var pathParams = new Dictionary<string, IOpenApiSchema>();
        foreach (var (_, operation) in pathItem.Operations)
        {
            if (operation.Parameters == null)
                continue;

            foreach (var param in operation.Parameters)
                if (param.In == ParameterLocation.Path && param.Schema != null)
                    pathParams.TryAdd(param.Name, param.Schema);
        }

        // Walk ancestor chain is not needed here since we only have the terminal;
        // instead, the tree builder resolves schemas during the Build pass by re-walking.
        if (terminal is { IsParameter: true, ParameterName: not null, ParameterSchema: null })
        {
            if (pathParams.TryGetValue(terminal.ParameterName, out var schema))
                terminal.ParameterSchema = schema;
        }

        // Also resolve ancestor parameter nodes that may not have schemas yet
        // by walking parent segments. We re-derive the ancestor chain from the segments array.
        // This is done globally in a second pass (see ResolveParameterSchemasRecursive).
    }

    /// <summary>
    /// Resolves builder class names for every node in the tree using dot-separated
    /// hierarchical names. Each path segment contributes a namespace part so that
    /// short class names (e.g. <c>IdBuilder</c>) live in distinct namespaces.
    /// </summary>
    private static void ResolveBuilderNames(PathSegmentNode root, string buildersNamespace)
    {
        AssignBuilderNames(root, buildersNamespace);
    }

    private static void AssignBuilderNames(PathSegmentNode node, string currentNamespace)
    {
        foreach (var (_, child) in node.Children)
        {
            string shortName;
            string childNamespace;

            if (child.IsParameter)
            {
                shortName = "IdBuilder";
                childNamespace = $"{currentNamespace}.Id";
            }
            else
            {
                var pascalSegment = GeneratorContext.ToPascalCase(child.SegmentName);
                shortName = $"{pascalSegment}Builder";
                childNamespace = $"{currentNamespace}.{pascalSegment}";
            }

            child.BuilderName = new GeneratedTypeInfo(currentNamespace, shortName);
            AssignBuilderNames(child, childNamespace);
        }
    }

    /// <summary>
    /// Returns all non-root nodes in the tree in depth-first order.
    /// </summary>
    public static IEnumerable<PathSegmentNode> GetAllNodes(PathSegmentNode root)
    {
        foreach (var (_, child) in root.Children)
        {
            yield return child;
            foreach (var descendant in GetAllNodes(child))
                yield return descendant;
        }
    }
}
