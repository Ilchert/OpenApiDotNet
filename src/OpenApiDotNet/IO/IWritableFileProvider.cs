using Microsoft.Extensions.FileProviders;

namespace OpenApiDotNet.IO;

/// <summary>
/// An <see cref="IFileProvider"/> that returns <see cref="IWritableFileInfo"/>
/// supporting write operations for both file and directory entries.
/// </summary>
internal interface IWritableFileProvider : IFileProvider
{
    new IWritableFileInfo GetFileInfo(string subpath);
}
