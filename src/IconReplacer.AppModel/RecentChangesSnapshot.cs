namespace IconReplacer.AppModel;

public sealed record RecentChangesSnapshot(
    string RestoreStateFile,
    RestoreHistoryFilter Filter,
    int TotalCount,
    int ShownCount,
    int RestorableCount,
    int WarningCount,
    int DisabledCount,
    IReadOnlyList<RecentChangeItem> Items);
