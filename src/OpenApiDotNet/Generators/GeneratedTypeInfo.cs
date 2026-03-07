namespace OpenApiDotNet.Generators;

internal record GeneratedTypeInfo(string Namespace, string Name)
{
    public string FullName => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";

    public static GeneratedTypeInfo FromFullyQualified(string fullyQualifiedName)
    {
        var lastDot = fullyQualifiedName.LastIndexOf('.');
        if (lastDot < 0)
            return new GeneratedTypeInfo(string.Empty, fullyQualifiedName);

        return new GeneratedTypeInfo(fullyQualifiedName[..lastDot], fullyQualifiedName[(lastDot + 1)..]);
    }
}
