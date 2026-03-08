using Microsoft.Extensions.FileProviders.Physical;

namespace OpenApiDotNet.IO;

/// <summary>
/// Extends <see cref="PhysicalFileInfo"/> with write capabilities.
/// All <see cref="Microsoft.Extensions.FileProviders.IFileInfo"/> read properties are inherited.
/// </summary>
internal sealed class PhysicalWritableFileInfo(FileInfo fileInfo) : PhysicalFileInfo(fileInfo), IWritableFileInfo
{
    public Stream CreateWriteStream()
    {
        var directory = fileInfo.DirectoryName;
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        return new FileStream(fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
    }

    public void Delete()
    {
        if (fileInfo.Exists)
        {
            fileInfo.Delete();
        }
        else if (Directory.Exists(fileInfo.FullName))
        {
            Directory.Delete(fileInfo.FullName);
        }
    }
}
