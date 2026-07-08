using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class DashboardService
{
    private readonly IconCatalogService _catalogService;

    public DashboardService(IconCatalogService? catalogService = null)
    {
        _catalogService = catalogService ?? new IconCatalogService();
    }

    public OperationResult<DashboardSnapshot> GetSnapshot(IconLibraryPaths paths)
    {
        var catalog = _catalogService.Scan(paths);
        if (!catalog.Succeeded || catalog.Value is null)
        {
            return OperationResult<DashboardSnapshot>.Failure(catalog.Error);
        }

        var restoreRecords = new RestoreRecordStore(paths.RestoreStateFile).List();
        if (!restoreRecords.Succeeded || restoreRecords.Value is null)
        {
            return OperationResult<DashboardSnapshot>.Failure(restoreRecords.Error);
        }

        var categories = BuildCategorySummaries(catalog.Value);
        var recordSummaries = restoreRecords.Value
            .Select(RestoreRecordSummary.FromRecord)
            .ToArray();
        var recentRecords = recordSummaries
            .OrderByDescending(record => record.CreatedAt)
            .Take(12)
            .ToArray();

        return OperationResult<DashboardSnapshot>.Success(new DashboardSnapshot(
            paths.LibraryRoot,
            paths.ImportedRoot,
            paths.RestoreStateFile,
            catalog.Value.Categories.Count,
            catalog.Value.Entries.Count,
            catalog.Value.Warnings.Count,
            restoreRecords.Value.Count,
            recordSummaries.Count(record => record.CanRestore),
            recordSummaries.Count(record => !record.TargetExists),
            recordSummaries.Count(record => !record.AppliedIconExists),
            categories,
            recentRecords));
    }

    public OperationResult<DashboardSnapshot> GetSnapshotFromEnvironment()
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<DashboardSnapshot>.Failure(paths.Error);
        }

        return GetSnapshot(paths.Value);
    }

    private static IReadOnlyList<IconCategorySummary> BuildCategorySummaries(IconCatalog catalog)
    {
        var summaries = new List<IconCategorySummary>();
        var rootEntries = catalog.Entries.Where(entry => entry.Category is null).ToArray();

        if (rootEntries.Length > 0)
        {
            summaries.Add(new IconCategorySummary("(root)", catalog.LibraryRoot, rootEntries.Length));
        }

        summaries.AddRange(catalog.Categories.Select(category =>
            new IconCategorySummary(
                category.Name,
                category.FullPath,
                catalog.Entries.Count(entry => entry.Category?.Name == category.Name))));

        return summaries
            .OrderBy(summary => summary.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
