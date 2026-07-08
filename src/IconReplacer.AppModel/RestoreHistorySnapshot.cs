namespace IconReplacer.AppModel;

public sealed record RestoreHistorySnapshot(
    string RestoreStateFile,
    RestoreHistoryFilter Filter,
    int TotalCount,
    int AppliedCount,
    int RestoredCount,
    int RestorableCount,
    int StaleCount,
    IReadOnlyList<RestoreRecordSummary> Records);
