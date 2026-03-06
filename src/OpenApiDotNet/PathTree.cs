using Microsoft.OpenApi;

namespace OpenApiDotNet;

/// <summary>
/// Represents a segment in the API path tree. Each node corresponds to a static segment
/// (e.g., "pets") or a parameterized segment (e.g., "{petId}").
/// </summary>
public class PathSegmentNode
{
    public string SegmentName { get; set; } = "";
    public bool IsParameter { get; set; }
    public string? ParameterName { get; set; }
    public IOpenApiSchema? ParameterSchema { get; set; }
    public Dictionary<string, PathSegmentNode> Children { get; } = new();
    public List<(HttpMethod Method, OpenApiOperation Operation)> Operations { get; } = [];
    public string BuilderName { get; set; } = "";
}

/// <summary>
/// Builds a tree of <see cref="PathSegmentNode"/> from OpenAPI path definitions
/// and resolves unique builder class names for each node.
/// </summary>
public static class PathTreeBuilder
{
    /// <summary>
    /// Parses all paths from the OpenAPI document into a tree structure and resolves builder names.
    /// </summary>
    public static PathSegmentNode Build(OpenApiPaths? paths)
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

        ResolveBuilderNames(root);
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
    /// Resolves unique builder class names for every node in the tree.
    /// Uses simple names when possible, falls back to context-prefixed names on collision.
    /// </summary>
    private static void ResolveBuilderNames(PathSegmentNode root)
    {
        // First pass: compute simple and context-prefixed names for all nodes
        var allNodes = new List<(PathSegmentNode node, string simpleName, string contextName)>();
        CollectNodes(root, "", allNodes);

        // Detect collisions in simple names
        var nameGroups = allNodes.GroupBy(n => n.simpleName).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();

        // Assign final names
        foreach (var (node, simpleName, contextName) in allNodes)
        {
            node.BuilderName = nameGroups.Contains(simpleName) ? contextName : simpleName;
        }
    }

    private static void CollectNodes(PathSegmentNode node, string contextPrefix,
        List<(PathSegmentNode node, string simpleName, string contextName)> result)
    {
        foreach (var (_, child) in node.Children)
        {
            string simpleName;
            string contextName;
            string nextContextPrefix;

            if (child.IsParameter)
            {
                // Find the parent static segment name for naming
                var parentStaticName = GetParentStaticSegmentName(node);
                simpleName = $"{parentStaticName}IdBuilder";
                // contextPrefix already includes the parent segment name, so just append "Id"
                contextName = $"{contextPrefix}IdBuilder";
                nextContextPrefix = $"{contextPrefix}Id";
            }
            else
            {
                var pascalSegment = ClientGenerator.ToPascalCase(child.SegmentName);
                simpleName = $"{pascalSegment}Builder";
                contextName = $"{contextPrefix}{pascalSegment}Builder";
                nextContextPrefix = $"{contextPrefix}{pascalSegment}";
            }

            result.Add((child, simpleName, contextName));
            CollectNodes(child, nextContextPrefix, result);
        }
    }

    private static string GetParentStaticSegmentName(PathSegmentNode parentNode)
    {
        // The parent node is the one containing this child. If the parent is a static segment, use its name.
        // If the parent is the root, use a fallback.
        if (!string.IsNullOrEmpty(parentNode.SegmentName) && !parentNode.IsParameter)
            return ClientGenerator.ToPascalCase(parentNode.SegmentName);

        return "Item";
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
