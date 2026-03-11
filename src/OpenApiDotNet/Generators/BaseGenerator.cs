namespace OpenApiDotNet.Generators;

internal abstract class BaseGenerator
{
    protected readonly GeneratorContext Context;

    public abstract GeneratedTypeInfo TypeInfo { get; }

    public BaseGenerator(GeneratorContext context)
    {
        Context = context;
    }

    public void WriteNamespace(CodeWriter writer)
    {
        writer.WriteLine($"namespace {TypeInfo.Namespace};");
        writer.WriteLine();
    }

    public void WriteWithNamespace(CodeWriter writer)
    {
        writer.WriteLine("#nullable enable");
        writer.WriteLine();
        writer.WriteLine($"namespace {TypeInfo.Namespace};");
        writer.WriteLine();
        Write(writer);
    }

    public static void WriteSummary(CodeWriter writer, string? summary)
    {
        if (string.IsNullOrEmpty(summary))
            return;
        
        writer.WriteLine("/// <summary>");
        foreach (var line in summary.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            writer.WriteLine($"/// {EscapeXmlComment(line)}");
        writer.WriteLine("/// </summary>");
    }

    private static string EscapeXmlComment(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    abstract public void Write(CodeWriter writer);
}
