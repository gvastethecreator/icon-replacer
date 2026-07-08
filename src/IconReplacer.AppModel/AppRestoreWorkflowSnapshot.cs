using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record AppRestoreWorkflowSnapshot(
    RestoreHistorySnapshot History,
    IconRestorePreviewSnapshot? Preview,
    Guid? SelectedRecordId,
    AppRestoreWorkflowStep Step,
    bool CanPreview,
    bool CanRestore,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
