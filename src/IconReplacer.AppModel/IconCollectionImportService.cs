using System.Security.Cryptography;
using System.Text;
using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconCollectionImportService
{
    private readonly IconCollectionService _collectionService;
    private readonly IconLibraryService _libraryService;

    public IconCollectionImportService(
        IconCollectionService? collectionService = null,
        IconLibraryService? libraryService = null)
    {
        _collectionService = collectionService ?? new IconCollectionService();
        _libraryService = libraryService ?? new IconLibraryService();
    }

    public OperationResult<IconCollectionImportResult> ImportIntoCollection(
        string collectionName,
        IReadOnlyList<string> sourceIconPaths,
        IconLibraryPaths paths)
    {
        if (sourceIconPaths is null)
        {
            return OperationResult<IconCollectionImportResult>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "Icon paths are required."));
        }

        var collection = _collectionService.CreateCollection(collectionName, paths);
        if (!collection.Succeeded || collection.Value is null)
        {
            return OperationResult<IconCollectionImportResult>.Failure(collection.Error);
        }

        var items = new List<IconBatchImportItem>();
        foreach (var sourceIconPath in sourceIconPaths)
        {
            var import = ImportIconIntoCollection(sourceIconPath, collection.Value.Collection);
            if (!import.Succeeded || import.Value is null)
            {
                items.Add(new IconBatchImportItem(
                    sourceIconPath,
                    IconBatchImportItemStatus.Failed,
                    ImportedIcon: null,
                    import.Error));
                continue;
            }

            items.Add(new IconBatchImportItem(
                sourceIconPath,
                import.Value.ReusedExisting
                    ? IconBatchImportItemStatus.ReusedExisting
                    : IconBatchImportItemStatus.Imported,
                import.Value.Entry,
                IconReplacerError.None));
        }

        var status = _libraryService.EnsureLibrary(paths);
        if (!status.Succeeded || status.Value is null)
        {
            return OperationResult<IconCollectionImportResult>.Failure(status.Error);
        }

        var refreshedCollection = collection.Value.Collection with
        {
            IconCount = Directory.EnumerateFiles(collection.Value.Collection.FullPath, "*.ico").Count()
        };

        return OperationResult<IconCollectionImportResult>.Success(new IconCollectionImportResult(
            refreshedCollection,
            items,
            status.Value));
    }

    public OperationResult<IconCollectionImportResult> ImportIntoCollectionFromEnvironment(
        string collectionName,
        IReadOnlyList<string> sourceIconPaths)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconCollectionImportResult>.Failure(paths.Error);
        }

        return ImportIntoCollection(collectionName, sourceIconPaths, paths.Value);
    }

    private static OperationResult<CollectionImportEntry> ImportIconIntoCollection(
        string sourceIconPath,
        IconCollectionSummary collection)
    {
        var validation = IconValidator.Validate(sourceIconPath);
        if (!validation.Succeeded || validation.Value is null)
        {
            return OperationResult<CollectionImportEntry>.Failure(validation.Error);
        }

        try
        {
            Directory.CreateDirectory(collection.FullPath);

            var hash = ComputeSha256(validation.Value.FullPath);
            var existing = FindExistingIcon(collection.FullPath, hash);
            if (existing is not null)
            {
                return OperationResult<CollectionImportEntry>.Success(new CollectionImportEntry(
                    ToEntry(collection, existing),
                    ReusedExisting: true));
            }

            var baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(validation.Value.FullPath));
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "icon";
            }

            var destination = GetAvailableDestination(collection.FullPath, baseName, hash);
            File.Copy(validation.Value.FullPath, destination);

            return OperationResult<CollectionImportEntry>.Success(new CollectionImportEntry(
                ToEntry(collection, destination),
                ReusedExisting: false));
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<CollectionImportEntry>.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "The icon could not be imported into the collection.",
                ex.Message));
        }
        catch (IOException ex)
        {
            return OperationResult<CollectionImportEntry>.Failure(new IconReplacerError(
                ErrorCode.Unknown,
                "The icon could not be imported into the collection.",
                ex.Message));
        }
    }

    private static byte[] ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return SHA256.HashData(stream);
    }

    private static string? FindExistingIcon(string collectionPath, byte[] sourceHash)
    {
        var hashPrefix = GetHashPrefix(sourceHash);
        foreach (var file in Directory.EnumerateFiles(collectionPath, $"*-{hashPrefix}.ico")
            .Order(StringComparer.OrdinalIgnoreCase))
        {
            var existingHash = ComputeSha256(file);
            if (existingHash.SequenceEqual(sourceHash))
            {
                return file;
            }
        }

        return null;
    }

    private static string GetAvailableDestination(string collectionPath, string baseName, byte[] sourceHash)
    {
        var hashPrefix = GetHashPrefix(sourceHash);
        var destination = Path.Combine(collectionPath, $"{baseName}-{hashPrefix}.ico");
        if (!File.Exists(destination))
        {
            return destination;
        }

        for (var index = 1; ; index++)
        {
            destination = Path.Combine(collectionPath, $"{baseName}-{hashPrefix}-{index}.ico");
            if (!File.Exists(destination))
            {
                return destination;
            }
        }
    }

    private static string GetHashPrefix(byte[] hash)
    {
        return Convert.ToHexString(hash, 0, 4).ToLowerInvariant();
    }

    private static IconLibraryEntry ToEntry(IconCollectionSummary collection, string iconPath)
    {
        return new IconLibraryEntry(
            Path.GetFileNameWithoutExtension(iconPath),
            Path.GetFullPath(iconPath),
            new IconCategory(collection.Name, collection.FullPath),
            new FileInfo(iconPath).Length);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(invalidChars.Contains(character) ? '-' : character);
        }

        return builder.ToString().Trim(' ', '.', '-');
    }

    private sealed record CollectionImportEntry(
        IconLibraryEntry Entry,
        bool ReusedExisting);
}
