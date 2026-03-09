using Microsoft.Extensions.FileProviders;

namespace OpenApiDotNet.IO;

/// <summary>
/// An <see cref="IFileProvider"/> that returns <see cref="IWritableFileInfo"/>
/// supporting write operations for both file and directory entries.
/// </summary>
internal interface IWritableFileProvider : IFileProvider
{
    /// <summary>
    /// Gets the root path of this provider, used for relative path computation.
    /// </summary>
    string Root { get; }

    new IWritableFileInfo GetFileInfo(string subpath);
}
