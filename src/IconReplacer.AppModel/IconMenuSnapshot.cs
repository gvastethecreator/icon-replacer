using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconMenuSnapshot(
    string IconLibraryRoot,
    string ChangeIconCommandLabel,
    IconMenuState State,
    string StatusMessage,
    string? RecommendedActionLabel,
    int TotalIconCount,
    int VisibleIconCount,
    int OmittedIconCount,
    IReadOnlyList<IconMenuItem> RootIcons,
    IReadOnlyList<IconMenuCategory> Categories,
    IReadOnlyList<IconCatalogWarning> Warnings,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt)
{
    public bool IsEmpty => TotalIconCount == 0;

    public bool IsTruncated => OmittedIconCount > 0;

    public bool IsAvailable => State != IconMenuState.Unavailable;
}
