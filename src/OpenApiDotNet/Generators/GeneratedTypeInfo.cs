namespace OpenApiDotNet.Generators;

internal record GeneratedTypeInfo(string Namespace, string Name)
{
    public string FullName => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";

    public static GeneratedTypeInfo FromFullyQualified(string fullyQualifiedName)
    {
        var lastDot = fullyQualifiedName.LastIndexOf('.');
        if (lastDot < 0)
            return new GeneratedTypeInfo(string.Empty, fullyQualifiedName);

        return new GeneratedTypeInfo(
            fullyQualifiedName.Substring(0, lastDot),
            fullyQualifiedName.Substring(lastDot + 1));
    }
}
