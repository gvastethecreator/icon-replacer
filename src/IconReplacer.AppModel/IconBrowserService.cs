using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconBrowserService
{
    public const string RootCategoryName = "(root)";

    private readonly IconCatalogService _catalogService;

    public IconBrowserService(IconCatalogService? catalogService = null)
    {
        _catalogService = catalogService ?? new IconCatalogService();
    }

    public OperationResult<IconBrowserSnapshot> Browse(
        IconLibraryPaths paths,
        IconBrowserOptions? options = null)
    {
        var normalizedOptions = (options ?? IconBrowserOptions.Default).Normalize();
        var catalog = _catalogService.Scan(paths);
        if (!catalog.Succeeded || catalog.Value is null)
        {
            return OperationResult<IconBrowserSnapshot>.Failure(catalog.Error);
        }

        var categories = BuildCategories(catalog.Value);
        var matchedEntries = catalog.Value.Entries
            .Where(entry => MatchesCategory(entry, normalizedOptions.CategoryName))
            .Where(entry => MatchesSearch(entry, normalizedOptions.SearchText))
            .OrderBy(entry => entry.Category?.Name ?? RootCategoryName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var visibleItems = matchedEntries
            .Take(normalizedOptions.MaxItems)
            .Select(ToBrowserItem)
            .ToArray();

        return OperationResult<IconBrowserSnapshot>.Success(new IconBrowserSnapshot(
            catalog.Value.LibraryRoot,
            normalizedOptions.SearchText,
            normalizedOptions.CategoryName,
            catalog.Value.Entries.Count,
            matchedEntries.Length,
            visibleItems.Length,
            Math.Max(0, matchedEntries.Length - visibleItems.Length),
            categories,
            visibleItems,
            catalog.Value.Warnings,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<IconBrowserSnapshot> BrowseFromEnvironment(IconBrowserOptions? options = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconBrowserSnapshot>.Failure(paths.Error);
        }

        return Browse(paths.Value, options);
    }

    private static IReadOnlyList<IconCategorySummary> BuildCategories(IconCatalog catalog)
    {
        var summaries = new List<IconCategorySummary>();
        var rootEntries = catalog.Entries.Where(entry => entry.Category is null).ToArray();
        if (rootEntries.Length > 0)
        {
            summaries.Add(new IconCategorySummary(RootCategoryName, catalog.LibraryRoot, rootEntries.Length));
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

    private static bool MatchesCategory(IconLibraryEntry entry, string? categoryName)
    {
        if (categoryName is null)
        {
            return true;
        }

        if (string.Equals(categoryName, RootCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return entry.Category is null;
        }

        return string.Equals(entry.Category?.Name, categoryName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesSearch(IconLibraryEntry entry, string? searchText)
    {
        if (searchText is null)
        {
            return true;
        }

        return entry.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            entry.FullPath.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
            (entry.Category?.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static IconBrowserItem ToBrowserItem(IconLibraryEntry entry)
    {
        return new IconBrowserItem(
            entry.DisplayName,
            entry.FullPath,
            entry.Category?.Name ?? RootCategoryName,
            entry.LengthBytes);
    }
}
