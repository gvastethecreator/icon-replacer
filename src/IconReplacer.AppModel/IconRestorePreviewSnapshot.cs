using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconRestorePreviewSnapshot(
    RestoreRecordSummary Record,
    string PreviousStateDetail,
    string RestoreActionDetail,
    bool CanRestore,
    string? WarningText,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
