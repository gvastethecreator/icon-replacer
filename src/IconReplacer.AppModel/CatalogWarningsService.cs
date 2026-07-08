using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class CatalogWarningsService
{
    private readonly IconCatalogService _catalogService;

    public CatalogWarningsService(IconCatalogService? catalogService = null)
    {
        _catalogService = catalogService ?? new IconCatalogService();
    }

    public OperationResult<CatalogWarningsSnapshot> GetWarnings(IconLibraryPaths paths)
    {
        var catalog = _catalogService.Scan(paths);
        if (!catalog.Succeeded || catalog.Value is null)
        {
            return OperationResult<CatalogWarningsSnapshot>.Failure(catalog.Error);
        }

        var warnings = catalog.Value.Warnings
            .Select(warning => CatalogWarningItem.FromWarning(warning, catalog.Value.LibraryRoot))
            .OrderBy(warning => warning.CategoryName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(warning => warning.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return OperationResult<CatalogWarningsSnapshot>.Success(new CatalogWarningsSnapshot(
            catalog.Value.LibraryRoot,
            catalog.Value.Entries.Count,
            warnings.Length,
            warnings,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<CatalogWarningsSnapshot> GetWarningsFromEnvironment()
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<CatalogWarningsSnapshot>.Failure(paths.Error);
        }

        return GetWarnings(paths.Value);
    }
}
