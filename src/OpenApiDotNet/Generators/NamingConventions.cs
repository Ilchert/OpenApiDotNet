namespace OpenApiDotNet.Generators;

internal static class NamingConventions
{
    private static readonly HashSet<string> s_csharpKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
        "checked", "class", "const", "continue", "decimal", "default", "delegate", "do",
        "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
        "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
        "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
        "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
        "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while"
    ];

    public static string ToPascalCase(string input)
    {
        var words = input.Split(['-', '_', ' ', '.'], StringSplitOptions.RemoveEmptyEntries);
        var result = string.Concat(words.Select(w => char.ToUpperInvariant(w[0]) + w[1..]));

        if (result.Length == 0)
            return "_unnamed";

        if (char.IsDigit(result[0]))
            result = "_" + result;

        return result;
    }

    public static string ToCamelCase(string input)
    {
        var pascal = ToPascalCase(input);
        if (string.IsNullOrEmpty(pascal)) return pascal;
        var camel = char.ToLowerInvariant(pascal[0]) + pascal[1..];
        return EscapeIfKeyword(camel);
    }

    public static string EscapeIfKeyword(string name) =>
        s_csharpKeywords.Contains(name) ? $"@{name}" : name;
}
