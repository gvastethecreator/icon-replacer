using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconPickerRequestSnapshot(
    string RequestedTargetPath,
    ShellSelectionEvaluation Selection,
    bool CanOpenPicker,
    string Title,
    string InitialDirectory,
    string FileTypeLabel,
    IReadOnlyList<string> FileExtensions,
    bool AllowMultiple,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
