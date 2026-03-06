namespace OpenApiDotNet.Generators;

internal record GeneratedTypeInfo(string Namespace, string Name)
{
    public string FullName => $"{Namespace}.{Name}";
}
