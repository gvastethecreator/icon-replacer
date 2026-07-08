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
            return OperationResult<IconMenuSnapshot>.Success(CreateUnavailableSnapshot(paths, catalog.Error));
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

        var state = GetState(catalog.Value.Entries.Count, omittedIconCount, catalog.Value.Warnings.Count);
        var (statusMessage, recommendedActionLabel) = GetStatus(state, catalog.Value.Warnings.Count);

        return OperationResult<IconMenuSnapshot>.Success(new IconMenuSnapshot(
            catalog.Value.LibraryRoot,
            DefaultChangeIconCommandLabel,
            state,
            statusMessage,
            recommendedActionLabel,
            catalog.Value.Entries.Count,
            visibleIconCount,
            omittedIconCount,
            rootIcons,
            visibleCategories,
            catalog.Value.Warnings,
            IconReplacerError.None,
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

    private static IconMenuSnapshot CreateUnavailableSnapshot(IconLibraryPaths paths, IconReplacerError error)
    {
        return new IconMenuSnapshot(
            paths.LibraryRoot,
            DefaultChangeIconCommandLabel,
            IconMenuState.Unavailable,
            "The Icon Library could not be scanned. Change icon can still open the picker, and the app can show diagnostics.",
            "Open diagnostics",
            TotalIconCount: 0,
            VisibleIconCount: 0,
            OmittedIconCount: 0,
            Array.Empty<IconMenuItem>(),
            Array.Empty<IconMenuCategory>(),
            Array.Empty<IconCatalogWarning>(),
            error,
            DateTimeOffset.UtcNow);
    }

    private static IconMenuState GetState(int totalIconCount, int omittedIconCount, int warningCount)
    {
        if (totalIconCount == 0)
        {
            return IconMenuState.Empty;
        }

        if (omittedIconCount > 0)
        {
            return IconMenuState.Truncated;
        }

        return warningCount > 0 ? IconMenuState.HasWarnings : IconMenuState.Ready;
    }

    private static (string StatusMessage, string? RecommendedActionLabel) GetStatus(
        IconMenuState state,
        int warningCount)
    {
        return state switch
        {
            IconMenuState.Empty when warningCount > 0 => (
                $"No valid icons were found, and {warningCount} .ico file(s) could not be read.",
                "Review catalog warnings"),
            IconMenuState.Empty => (
                "No icons were found in the Icon Library. Add .ico files or create folders under .icons.",
                "Import icons"),
            IconMenuState.Truncated => (
                "The Icon Library is larger than the bounded shell menu. Open the app to browse every icon.",
                "Open Icon Replacer"),
            IconMenuState.HasWarnings => (
                $"The menu is ready, but {warningCount} .ico file(s) could not be read.",
                "Review catalog warnings"),
            _ => (
                "The Icon Library menu is ready.",
                null)
        };
    }
}
