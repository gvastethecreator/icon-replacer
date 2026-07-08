using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconLibraryService
{
    private readonly IconLibraryImporter _importer;
    private readonly IconCatalogService _catalogService;

    public IconLibraryService(
        IconLibraryImporter? importer = null,
        IconCatalogService? catalogService = null)
    {
        _importer = importer ?? new IconLibraryImporter();
        _catalogService = catalogService ?? new IconCatalogService();
    }

    public OperationResult<IconLibraryStatus> EnsureLibrary(IconLibraryPaths paths)
    {
        var catalog = _catalogService.Scan(paths);
        if (!catalog.Succeeded || catalog.Value is null)
        {
            return OperationResult<IconLibraryStatus>.Failure(catalog.Error);
        }

        return OperationResult<IconLibraryStatus>.Success(ToStatus(catalog.Value, paths));
    }

    public OperationResult<IconLibraryStatus> EnsureLibraryFromEnvironment()
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconLibraryStatus>.Failure(paths.Error);
        }

        return EnsureLibrary(paths.Value);
    }

    public OperationResult<IconImportResult> ImportIcon(
        string sourceIconPath,
        IconLibraryPaths paths,
        string? preferredName = null)
    {
        var import = _importer.Import(sourceIconPath, paths, preferredName);
        if (!import.Succeeded || import.Value is null)
        {
            return OperationResult<IconImportResult>.Failure(import.Error);
        }

        var status = EnsureLibrary(paths);
        if (!status.Succeeded || status.Value is null)
        {
            return OperationResult<IconImportResult>.Failure(status.Error);
        }

        return OperationResult<IconImportResult>.Success(new IconImportResult(
            import.Value,
            status.Value));
    }

    public OperationResult<IconBatchImportResult> ImportIcons(
        IReadOnlyList<string> sourceIconPaths,
        IconLibraryPaths paths)
    {
        if (sourceIconPaths is null)
        {
            return OperationResult<IconBatchImportResult>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "Icon paths are required."));
        }

        var items = new List<IconBatchImportItem>();
        foreach (var sourceIconPath in sourceIconPaths)
        {
            var importedBefore = GetImportedFiles(paths);
            var import = _importer.Import(sourceIconPath, paths);
            if (!import.Succeeded || import.Value is null)
            {
                items.Add(new IconBatchImportItem(
                    sourceIconPath,
                    IconBatchImportItemStatus.Failed,
                    ImportedIcon: null,
                    import.Error));
                continue;
            }

            var status = importedBefore.Contains(import.Value.FullPath)
                ? IconBatchImportItemStatus.ReusedExisting
                : IconBatchImportItemStatus.Imported;
            items.Add(new IconBatchImportItem(
                sourceIconPath,
                status,
                import.Value,
                IconReplacerError.None));
        }

        var libraryStatus = EnsureLibrary(paths);
        if (!libraryStatus.Succeeded || libraryStatus.Value is null)
        {
            return OperationResult<IconBatchImportResult>.Failure(libraryStatus.Error);
        }

        return OperationResult<IconBatchImportResult>.Success(new IconBatchImportResult(
            items,
            libraryStatus.Value));
    }

    public OperationResult<IconImportResult> ImportIconFromEnvironment(
        string sourceIconPath,
        string? preferredName = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconImportResult>.Failure(paths.Error);
        }

        return ImportIcon(sourceIconPath, paths.Value, preferredName);
    }

    public OperationResult<IconBatchImportResult> ImportIconsFromEnvironment(
        IReadOnlyList<string> sourceIconPaths)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconBatchImportResult>.Failure(paths.Error);
        }

        return ImportIcons(sourceIconPaths, paths.Value);
    }

    private static IconLibraryStatus ToStatus(IconCatalog catalog, IconLibraryPaths paths)
    {
        return new IconLibraryStatus(
            paths.LibraryRoot,
            paths.ImportedRoot,
            catalog.Categories.Count,
            catalog.Entries.Count,
            catalog.Warnings.Count,
            catalog.Warnings);
    }

    private static HashSet<string> GetImportedFiles(IconLibraryPaths paths)
    {
        if (!Directory.Exists(paths.ImportedRoot))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return Directory.EnumerateFiles(paths.ImportedRoot, "*.ico")
            .Select(Path.GetFullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
