using System.Text;
using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconCollectionService
{
    private readonly IconCatalogService _catalogService;
    private readonly IconLibraryService _libraryService;

    public IconCollectionService(
        IconCatalogService? catalogService = null,
        IconLibraryService? libraryService = null)
    {
        _catalogService = catalogService ?? new IconCatalogService();
        _libraryService = libraryService ?? new IconLibraryService(catalogService: _catalogService);
    }

    public OperationResult<IReadOnlyList<IconCollectionSummary>> ListCollections(IconLibraryPaths paths)
    {
        var catalog = _catalogService.Scan(paths);
        if (!catalog.Succeeded || catalog.Value is null)
        {
            return OperationResult<IReadOnlyList<IconCollectionSummary>>.Failure(catalog.Error);
        }

        var summaries = catalog.Value.Categories
            .Select(category => ToSummary(
                category.Name,
                category.FullPath,
                catalog.Value.Entries.Count(entry => entry.Category?.Name == category.Name)))
            .OrderBy(summary => summary.IsImportedCollection)
            .ThenBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return OperationResult<IReadOnlyList<IconCollectionSummary>>.Success(summaries);
    }

    public OperationResult<IReadOnlyList<IconCollectionSummary>> ListCollectionsFromEnvironment()
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IReadOnlyList<IconCollectionSummary>>.Failure(paths.Error);
        }

        return ListCollections(paths.Value);
    }

    public OperationResult<IconCollectionCreateResult> CreateCollection(
        string requestedName,
        IconLibraryPaths paths)
    {
        var sanitizedName = SanitizeCollectionName(requestedName);
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return OperationResult<IconCollectionCreateResult>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "A collection name is required."));
        }

        var destination = Path.GetFullPath(Path.Combine(paths.LibraryRoot, sanitizedName));
        var libraryRoot = EnsureTrailingSeparator(Path.GetFullPath(paths.LibraryRoot));
        if (!destination.StartsWith(libraryRoot, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<IconCollectionCreateResult>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The collection path must stay inside the Icon Library.",
                destination));
        }

        var created = false;
        try
        {
            Directory.CreateDirectory(paths.LibraryRoot);
            if (!Directory.Exists(destination))
            {
                Directory.CreateDirectory(destination);
                created = true;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<IconCollectionCreateResult>.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "The icon collection could not be created.",
                ex.Message));
        }
        catch (IOException ex)
        {
            return OperationResult<IconCollectionCreateResult>.Failure(new IconReplacerError(
                ErrorCode.Unknown,
                "The icon collection could not be created.",
                ex.Message));
        }

        var status = _libraryService.EnsureLibrary(paths);
        if (!status.Succeeded || status.Value is null)
        {
            return OperationResult<IconCollectionCreateResult>.Failure(status.Error);
        }

        var iconCount = Directory.EnumerateFiles(destination, "*.ico").Count();
        return OperationResult<IconCollectionCreateResult>.Success(new IconCollectionCreateResult(
            ToSummary(sanitizedName, destination, iconCount),
            created,
            status.Value));
    }

    public OperationResult<IconCollectionCreateResult> CreateCollectionFromEnvironment(string requestedName)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconCollectionCreateResult>.Failure(paths.Error);
        }

        return CreateCollection(requestedName, paths.Value);
    }

    private static IconCollectionSummary ToSummary(string name, string fullPath, int iconCount)
    {
        return new IconCollectionSummary(
            name,
            Path.GetFullPath(fullPath),
            iconCount,
            string.Equals(name, IconLibraryPaths.ImportedFolderName, StringComparison.OrdinalIgnoreCase));
    }

    private static string SanitizeCollectionName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (invalidChars.Contains(character))
            {
                builder.Append('-');
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Trim(' ', '.', '-');
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
