using System.Text;

namespace OpenApiDotNet.IO;

internal static class WritableFileInfoExtensions
{
    public static void WriteAllText(this IWritableFileInfo fileInfo, string contents)
    {
        using var stream = fileInfo.CreateWriteStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        writer.Write(contents);
    }

    /// <summary>
    /// Writes the contents only if the file does not exist or its current content differs.
    /// Returns true if the file was written, false if skipped (unchanged).
    /// </summary>
    public static bool WriteAllTextIfChanged(this IWritableFileInfo fileInfo, string contents)
    {
        if (fileInfo.Exists && !fileInfo.IsDirectory)
        {
            using var readStream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(readStream, Encoding.UTF8);
            var existing = reader.ReadToEnd();

            if (string.Equals(existing, contents, StringComparison.Ordinal))
                return false;
        }

        fileInfo.WriteAllText(contents);
        return true;
    }
}
