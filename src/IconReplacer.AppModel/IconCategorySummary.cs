using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconCategorySummary(
    string Name,
    string FullPath,
    int IconCount)
{
    public static IconCategorySummary FromCatalog(IconCategory? category, IReadOnlyList<IconLibraryEntry> entries)
    {
        var categoryName = category?.Name ?? "(root)";
        var categoryPath = category?.FullPath ?? string.Empty;
        var count = entries.Count(entry => entry.Category?.Name == category?.Name);

        return new IconCategorySummary(categoryName, categoryPath, count);
    }
}

