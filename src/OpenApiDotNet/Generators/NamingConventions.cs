namespace OpenApiDotNet.Generators;

internal static class NamingConventions
{
    public static string ToPascalCase(string input)
    {
        var words = input.Split(['-', '_', ' ', '.'], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));
    }

    public static string ToCamelCase(string input)
    {
        var pascal = ToPascalCase(input);
        if (string.IsNullOrEmpty(pascal)) return pascal;
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }
}
