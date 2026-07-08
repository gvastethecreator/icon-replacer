using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconBrowserSnapshot(
    string IconLibraryRoot,
    string? SearchText,
    string? CategoryName,
    int TotalIconCount,
    int MatchedIconCount,
    int VisibleIconCount,
    int OmittedIconCount,
    IReadOnlyList<IconCategorySummary> Categories,
    IReadOnlyList<IconBrowserItem> Items,
    IReadOnlyList<IconCatalogWarning> Warnings,
    DateTimeOffset RefreshedAt)
{
    public bool IsEmpty => TotalIconCount == 0;

    public bool HasMatches => MatchedIconCount > 0;

    public bool IsTruncated => OmittedIconCount > 0;
}
