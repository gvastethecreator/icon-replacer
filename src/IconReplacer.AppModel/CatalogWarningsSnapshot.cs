namespace IconReplacer.AppModel;

public sealed record CatalogWarningsSnapshot(
    string IconLibraryRoot,
    int TotalIconCount,
    int WarningCount,
    IReadOnlyList<CatalogWarningItem> Warnings,
    DateTimeOffset RefreshedAt)
{
    public bool HasWarnings => WarningCount > 0;
}
