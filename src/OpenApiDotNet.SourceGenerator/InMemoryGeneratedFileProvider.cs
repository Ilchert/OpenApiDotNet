using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using OpenApiDotNet.IO;

namespace OpenApiDotNet.SourceGenerator;

internal sealed class InMemoryGeneratedFileProvider : IWritableFileProvider
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);

    public string Root => "/";

    public IReadOnlyDictionary<string, string> Files => _files;

    public IWritableFileInfo GetFileInfo(string subpath) => new InMemoryGeneratedFileInfo(this, Normalize(subpath));

    IFileInfo IFileProvider.GetFileInfo(string subpath) => GetFileInfo(subpath);

    public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;

    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    internal bool TryGetContent(string subpath, out string content) => _files.TryGetValue(Normalize(subpath), out content!);

    internal void SetContent(string subpath, string content) => _files[Normalize(subpath)] = content;

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');

    private sealed class InMemoryGeneratedFileInfo(InMemoryGeneratedFileProvider provider, string subpath) : IWritableFileInfo
    {
        public bool Exists => provider._files.ContainsKey(subpath);

        public long Length => provider.TryGetContent(subpath, out var content)
            ? Encoding.UTF8.GetByteCount(content)
            : -1;

        public string? PhysicalPath => null;

        public string Name => Path.GetFileName(subpath);

        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;

        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            if (!provider.TryGetContent(subpath, out var content))
                throw new FileNotFoundException($"Generated file not found: {subpath}");

            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        public Stream CreateWriteStream() => new GeneratedWriteStream(provider, subpath);

        public void Delete() => provider._files.Remove(subpath);

        private sealed class GeneratedWriteStream(InMemoryGeneratedFileProvider provider, string subpath) : MemoryStream
        {
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    provider.SetContent(subpath, Encoding.UTF8.GetString(ToArray()));

                base.Dispose(disposing);
            }
        }
    }
}
