using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using OpenApiDotNet.IO;

namespace OpenApiDotNet.Tests.IO;

internal sealed class InMemoryWritableFileProvider : IWritableFileProvider
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All written files keyed by normalized subpath.
    /// </summary>
    public IReadOnlyDictionary<string, string> Files => _files;

    public IWritableFileInfo GetFileInfo(string subpath)
    {
        var normalized = InMemoryWritableFileInfo.Normalize(subpath);

        if (_directories.Contains(normalized))
            return new InMemoryWritableFileInfo(this, normalized, isDirectory: true);

        return new InMemoryWritableFileInfo(this, normalized);
    }

    IFileInfo IFileProvider.GetFileInfo(string subpath) => GetFileInfo(subpath);

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        var normalized = InMemoryWritableFileInfo.Normalize(subpath);
        var prefix = string.IsNullOrEmpty(normalized) ? "" : normalized + "/";

        var entries = new List<IFileInfo>();

        // Find immediate child files
        foreach (var key in _files.Keys)
        {
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var remainder = key[prefix.Length..];
            if (!remainder.Contains('/'))
                entries.Add(new InMemoryWritableFileInfo(this, key));
        }

        // Find immediate child directories
        var childDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in _files.Keys)
        {
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var remainder = key[prefix.Length..];
            var slashIndex = remainder.IndexOf('/');
            if (slashIndex >= 0)
                childDirs.Add(prefix + remainder[..slashIndex]);
        }

        foreach (var dir in childDirs)
            entries.Add(new InMemoryWritableFileInfo(this, dir, isDirectory: true));

        return new InMemoryDirectoryContents(entries, _directories.Contains(normalized) || entries.Count > 0);
    }

    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    internal bool FileExists(string normalizedPath) => _files.ContainsKey(normalizedPath);

    internal bool DirectoryExists(string normalizedPath)
    {
        if (_directories.Contains(normalizedPath))
            return true;

        var prefix = string.IsNullOrEmpty(normalizedPath) ? "" : normalizedPath + "/";
        return _files.Keys.Any(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    internal bool TryGetContent(string normalizedPath, out string content) =>
        _files.TryGetValue(normalizedPath, out content!);

    internal void SetContent(string normalizedPath, string content)
    {
        _files[normalizedPath] = content;

        // Ensure parent directories are tracked
        var parent = Path.GetDirectoryName(normalizedPath)?.Replace('\\', '/');
        while (!string.IsNullOrEmpty(parent))
        {
            _directories.Add(parent);
            parent = Path.GetDirectoryName(parent)?.Replace('\\', '/');
        }
    }

    internal void RemoveFile(string normalizedPath) => _files.Remove(normalizedPath);

    internal void RemoveDirectory(string normalizedPath) => _directories.Remove(normalizedPath);

    private sealed class InMemoryDirectoryContents : IDirectoryContents
    {
        private readonly List<IFileInfo> _entries;

        public InMemoryDirectoryContents(List<IFileInfo> entries, bool exists)
        {
            _entries = entries;
            Exists = exists;
        }

        public bool Exists { get; }

        public IEnumerator<IFileInfo> GetEnumerator() => _entries.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
