using Microsoft.OpenApi;
using OpenApiDotNet.Generators;

namespace OpenApiDotNet;

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

        foreach (var pathEntry in paths)
        {
            var pathKey = pathEntry.Key;
            var pathItem = pathEntry.Value;
            var current = root;

            foreach (var segment in pathKey.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!current.Children.TryGetValue(segment, out var child))
                {
                    var isParam = segment.Length > 1 && segment[0] == '{' && segment[segment.Length - 1] == '}';
                    child = new PathSegmentNode
                    {
                        SegmentName = segment,
                        IsParameter = isParam,
                        ParameterName = isParam ? segment.Substring(1, segment.Length - 2) : null
                    };
                    current.Children[segment] = child;
                }
                current = child;
            }

            // Resolve parameter schema for the terminal node from operation definitions
            if (current is { IsParameter: true, ParameterName: not null, ParameterSchema: null }
                && pathItem.Operations is not null)
            {
                current.ParameterSchema = pathItem.Operations
                    .SelectMany(op => op.Value.Parameters ?? [])
                    .FirstOrDefault(p => p.In == ParameterLocation.Path && p.Name == current.ParameterName)
                    ?.Schema;
            }

            // Attach operations to the terminal node
            if (pathItem.Operations is not null)
            {
                foreach (var operationEntry in pathItem.Operations)
                {
                    current.Operations.Add((operationEntry.Key, operationEntry.Value));
                }
            }
        }

        ResolveBuilderNames(root, $"{context.DefaultNamespace}.Builders");
        return root;
    }

    /// <summary>
    /// Resolves builder class names for every node in the tree using dot-separated
    /// hierarchical names. Each path segment contributes a namespace part so that
    /// short class names (e.g. <c>IdBuilder</c>) live in distinct namespaces.
    /// </summary>
    private static void ResolveBuilderNames(PathSegmentNode node, string currentNamespace)
    {
        foreach (var childEntry in node.Children)
        {
            var child = childEntry.Value;
            string shortName;
            string childNamespace;

            if (child.IsParameter)
            {
                shortName = "IdBuilder";
                childNamespace = $"{currentNamespace}.Id";
            }
            else
            {
                var pascalSegment = NamingConventions.ToPascalCase(child.SegmentName);
                shortName = $"{pascalSegment}Builder";
                childNamespace = $"{currentNamespace}.{pascalSegment}";
            }

            child.BuilderName = new GeneratedTypeInfo(currentNamespace, shortName);
            ResolveBuilderNames(child, childNamespace);
        }
    }

    /// <summary>
    /// Returns all non-root nodes in the tree in depth-first order.
    /// </summary>
    public static IEnumerable<PathSegmentNode> GetAllNodes(PathSegmentNode root)
    {
        foreach (var childEntry in root.Children)
        {
            var child = childEntry.Value;
            yield return child;
            foreach (var descendant in GetAllNodes(child))
                yield return descendant;
        }
    }
}
