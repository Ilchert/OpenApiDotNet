using System.Text;
using OpenApiDotNet.IO;

namespace OpenApiDotNet.Tests.IO;

internal sealed class InMemoryWritableFileInfo : IWritableFileInfo
{
    private readonly InMemoryWritableFileProvider _provider;
    private readonly string _subpath;
    private readonly bool _isDirectory;

    public InMemoryWritableFileInfo(InMemoryWritableFileProvider provider, string subpath, bool isDirectory = false)
    {
        _provider = provider;
        _subpath = Normalize(subpath);
        _isDirectory = isDirectory;
    }

    public bool Exists => _isDirectory
        ? _provider.DirectoryExists(_subpath)
        : _provider.FileExists(_subpath);

    public long Length => !_isDirectory && _provider.TryGetContent(_subpath, out var content)
        ? Encoding.UTF8.GetByteCount(content)
        : -1;

    public string? PhysicalPath => null;

    public string Name => Path.GetFileName(_subpath);

    public DateTimeOffset LastModified => DateTimeOffset.UtcNow;

    public bool IsDirectory => _isDirectory;

    public Stream CreateReadStream()
    {
        if (_isDirectory)
            throw new InvalidOperationException("Cannot create a read stream for a directory.");

        if (!_provider.TryGetContent(_subpath, out var content))
            throw new FileNotFoundException($"File not found: {_subpath}");

        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }

    public Stream CreateWriteStream()
    {
        if (_isDirectory)
            throw new InvalidOperationException("Cannot create a write stream for a directory.");

        return new InMemoryWriteStream(_provider, _subpath);
    }

    public void Delete()
    {
        if (_isDirectory)
            _provider.RemoveDirectory(_subpath);
        else
            _provider.RemoveFile(_subpath);
    }

    internal static string Normalize(string path) =>
        path.Replace('\\', '/').TrimStart('/');

    /// <summary>
    /// A <see cref="MemoryStream"/> that flushes its content back to the provider on dispose.
    /// </summary>
    private sealed class InMemoryWriteStream : MemoryStream
    {
        private readonly InMemoryWritableFileProvider _provider;
        private readonly string _subpath;

        public InMemoryWriteStream(InMemoryWritableFileProvider provider, string subpath)
        {
            _provider = provider;
            _subpath = subpath;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var content = Encoding.UTF8.GetString(ToArray());
                _provider.SetContent(_subpath, content);
            }

            base.Dispose(disposing);
        }
    }
}
