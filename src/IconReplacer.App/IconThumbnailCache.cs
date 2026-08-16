using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

namespace IconReplacer.App;

internal sealed class IconThumbnailCache
{
    private const int MaxEntries = 256;

    private readonly Dictionary<CacheKey, CachedThumbnail> _images = new(CacheKeyComparer.Instance);
    private readonly Dictionary<CacheKey, PendingThumbnail> _loading = new(CacheKeyComparer.Instance);
    private readonly Queue<CacheIdentity> _insertionOrder = new();

    public async Task<BitmapImage?> GetAsync(string path, int requestedDecodePixelWidth)
    {
        var key = new CacheKey(path, NormalizeDecodePixelWidth(requestedDecodePixelWidth));
        if (!TryGetFileVersion(path, out var version))
        {
            Remove(path);
            return null;
        }

        if (_images.TryGetValue(key, out var cached) && cached.Version == version)
        {
            return cached.Bitmap;
        }

        _images.Remove(key);
        if (_loading.TryGetValue(key, out var pending) && pending.Version == version)
        {
            return await pending.Task;
        }

        var loading = LoadAsync(key, version);
        _loading[key] = new PendingThumbnail(version, loading);
        try
        {
            return await loading;
        }
        finally
        {
            if (_loading.TryGetValue(key, out var current) &&
                ReferenceEquals(current.Task, loading))
            {
                _loading.Remove(key);
            }
        }
    }

    private async Task<BitmapImage?> LoadAsync(CacheKey key, FileVersion version)
    {
        try
        {
            var bitmap = new BitmapImage
            {
                DecodePixelWidth = key.DecodePixelWidth
            };

            await using var stream = new FileStream(
                key.Path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4096,
                useAsync: true);
            using var randomAccessStream = stream.AsRandomAccessStream();
            await bitmap.SetSourceAsync(randomAccessStream);

            if (!TryGetFileVersion(key.Path, out var currentVersion) || currentVersion != version)
            {
                return null;
            }

            Add(key, version, bitmap);
            return bitmap;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool TryGetFileVersion(string path, out FileVersion version)
    {
        version = default;
        try
        {
            var file = new FileInfo(path);
            if (!file.Exists)
            {
                return false;
            }

            version = new FileVersion(file.Length, file.LastWriteTimeUtc.Ticks);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static int NormalizeDecodePixelWidth(int requestedDecodePixelWidth) =>
        requestedDecodePixelWidth switch
        {
            <= 64 => 64,
            <= 128 => 128,
            _ => 256
        };

    private void Remove(string path)
    {
        foreach (var key in _images.Keys
                     .Where(candidate => string.Equals(
                         candidate.Path,
                         path,
                         StringComparison.OrdinalIgnoreCase))
                     .ToArray())
        {
            _images.Remove(key);
        }
    }

    private void Add(CacheKey key, FileVersion version, BitmapImage bitmap)
    {
        while (_images.Count >= MaxEntries && _insertionOrder.TryDequeue(out var oldest))
        {
            if (_images.TryGetValue(oldest.Key, out var cached) &&
                cached.Version == oldest.Version)
            {
                _images.Remove(oldest.Key);
            }
        }

        _images[key] = new CachedThumbnail(version, bitmap);
        _insertionOrder.Enqueue(new CacheIdentity(key, version));
    }

    private readonly record struct FileVersion(long Length, long LastWriteTimeUtcTicks);

    private readonly record struct CacheKey(string Path, int DecodePixelWidth);

    private readonly record struct CacheIdentity(CacheKey Key, FileVersion Version);

    private sealed record CachedThumbnail(FileVersion Version, BitmapImage Bitmap);

    private sealed record PendingThumbnail(FileVersion Version, Task<BitmapImage?> Task);

    private sealed class CacheKeyComparer : IEqualityComparer<CacheKey>
    {
        public static CacheKeyComparer Instance { get; } = new();

        public bool Equals(CacheKey left, CacheKey right) =>
            left.DecodePixelWidth == right.DecodePixelWidth &&
            string.Equals(left.Path, right.Path, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(CacheKey key) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(key.Path),
                key.DecodePixelWidth);
    }
}
