using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconImportPickerRequestSnapshot(
    bool CanOpenPicker,
    string Title,
    string InitialDirectory,
    string DestinationCollectionName,
    string DestinationDirectory,
    string FileTypeLabel,
    IReadOnlyList<string> FileExtensions,
    bool AllowMultiple,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
