using Microsoft.Extensions.FileProviders.Physical;

namespace OpenApiDotNet.IO;

/// <summary>
/// Extends <see cref="PhysicalFileInfo"/> with write capabilities.
/// All <see cref="Microsoft.Extensions.FileProviders.IFileInfo"/> read properties are inherited.
/// </summary>
internal sealed class PhysicalWritableFileInfo : PhysicalFileInfo, IWritableFileInfo
{
    private readonly FileInfo _fileInfo;

    public PhysicalWritableFileInfo(FileInfo fileInfo) : base(fileInfo)
    {
        _fileInfo = fileInfo;
    }

    public Stream CreateWriteStream()
    {
        var directory = _fileInfo.DirectoryName;
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        return new FileStream(_fileInfo.FullName, FileMode.Create, FileAccess.Write, FileShare.None);
    }

    public void Delete()
    {
        if (_fileInfo.Exists)
        {
            _fileInfo.Delete();
        }
        else if (Directory.Exists(_fileInfo.FullName))
        {
            Directory.Delete(_fileInfo.FullName);
        }
    }
}
