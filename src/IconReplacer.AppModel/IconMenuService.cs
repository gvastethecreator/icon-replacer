using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconMenuService
{
    public const string DefaultChangeIconCommandLabel = "Change icon...";

    private readonly IconCatalogService _catalogService;

    public IconMenuService(IconCatalogService? catalogService = null)
    {
        _catalogService = catalogService ?? new IconCatalogService();
    }

    public OperationResult<IconMenuSnapshot> BuildSnapshot(
        IconLibraryPaths paths,
        IconMenuOptions? options = null)
    {
        var normalizedOptions = (options ?? IconMenuOptions.Default).Normalize();
        var catalog = _catalogService.Scan(paths);
        if (!catalog.Succeeded || catalog.Value is null)
        {
            return OperationResult<IconMenuSnapshot>.Failure(catalog.Error);
        }

        var rootEntries = catalog.Value.Entries
            .Where(entry => entry.Category is null)
            .OrderBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rootIcons = rootEntries
            .Take(normalizedOptions.MaxRootIconItems)
            .Select(ToMenuItem)
            .ToArray();
        var rootOmitted = Math.Max(0, rootEntries.Length - rootIcons.Length);

        var allCategories = catalog.Value.Categories
            .Select(category => BuildCategory(category, catalog.Value.Entries, normalizedOptions.MaxIconItemsPerCategory))
            .Where(category => category.TotalIconCount > 0)
            .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var visibleCategories = allCategories
            .Take(normalizedOptions.MaxCategoryCount)
            .ToArray();
        var omittedCategories = allCategories.Skip(visibleCategories.Length).ToArray();

        var omittedFromHiddenCategories = omittedCategories.Sum(category => category.TotalIconCount);
        var visibleIconCount = rootIcons.Length + visibleCategories.Sum(category => category.Items.Count);
        var omittedIconCount = rootOmitted +
            visibleCategories.Sum(category => category.OmittedIconCount) +
            omittedFromHiddenCategories;

        return OperationResult<IconMenuSnapshot>.Success(new IconMenuSnapshot(
            catalog.Value.LibraryRoot,
            DefaultChangeIconCommandLabel,
            catalog.Value.Entries.Count,
            visibleIconCount,
            omittedIconCount,
            rootIcons,
            visibleCategories,
            catalog.Value.Warnings,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<IconMenuSnapshot> BuildSnapshotFromEnvironment(IconMenuOptions? options = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconMenuSnapshot>.Failure(paths.Error);
        }

        return BuildSnapshot(paths.Value, options);
    }

    private static IconMenuCategory BuildCategory(
        IconCategory category,
        IReadOnlyList<IconLibraryEntry> entries,
        int maxItems)
    {
        var categoryEntries = entries
            .Where(entry => entry.Category?.Name == category.Name)
            .OrderBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var items = categoryEntries
            .Take(maxItems)
            .Select(ToMenuItem)
            .ToArray();

        return new IconMenuCategory(
            category.Name,
            category.FullPath,
            categoryEntries.Length,
            Math.Max(0, categoryEntries.Length - items.Length),
            items);
    }

    private static IconMenuItem ToMenuItem(IconLibraryEntry entry)
    {
        return new IconMenuItem(entry.DisplayName, entry.FullPath);
    }
}
