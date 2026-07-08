namespace IconReplacer.AppModel;

public sealed record DashboardSnapshot(
    string IconLibraryRoot,
    string ImportedIconsRoot,
    string RestoreStateFile,
    int CategoryCount,
    int IconCount,
    int CatalogWarningCount,
    int RestoreRecordCount,
    int RestorableRecordCount,
    int MissingTargetRecordCount,
    int MissingAppliedIconRecordCount,
    IReadOnlyList<IconCategorySummary> Categories,
    IReadOnlyList<RestoreRecordSummary> RecentRecords);
