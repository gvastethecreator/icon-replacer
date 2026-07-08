using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconChangePreviewSnapshot(
    string RequestedTargetPath,
    string RequestedIconPath,
    ShellSelectionEvaluation Selection,
    IconDetailsSnapshot? IconDetails,
    IconReplacerError IconError,
    bool CanApply,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
