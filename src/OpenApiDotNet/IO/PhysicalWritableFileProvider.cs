using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace OpenApiDotNet.IO;

/// <summary>
/// A <see cref="PhysicalFileProvider"/> extended with write capabilities.
/// Inherits all read operations (GetDirectoryContents, Watch, Dispose) from the base class.
/// Shadows <see cref="PhysicalFileProvider.GetFileInfo"/> to return <see cref="IWritableFileInfo"/>.
/// </summary>
internal sealed class PhysicalWritableFileProvider : PhysicalFileProvider, IWritableFileProvider
{
    public PhysicalWritableFileProvider(string root) : base(root) { }

    public new IWritableFileInfo GetFileInfo(string subpath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(Root, subpath));
        return new PhysicalWritableFileInfo(new FileInfo(fullPath));
    }
}
