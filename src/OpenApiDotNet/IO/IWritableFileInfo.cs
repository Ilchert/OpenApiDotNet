using Microsoft.Extensions.FileProviders;

namespace OpenApiDotNet.IO;

/// <summary>
/// Extends <see cref="IFileInfo"/> with write capabilities.
/// Represents both files (<see cref="IFileInfo.IsDirectory"/> = false) and
/// directories (<see cref="IFileInfo.IsDirectory"/> = true), consistent
/// with the <see cref="IFileInfo"/> contract.
/// </summary>
internal interface IWritableFileInfo : IFileInfo
{
    /// <summary>
    /// Creates a writable stream to the file. Parent directories are created automatically.
    /// Throws <see cref="InvalidOperationException"/> if <see cref="IFileInfo.IsDirectory"/> is true.
    /// </summary>
    Stream CreateWriteStream();

    /// <summary>
    /// Deletes this entry. For files, deletes the file. For directories, deletes if empty.
    /// No-op if <see cref="IFileInfo.Exists"/> is false.
    /// </summary>
    void Delete();
}
