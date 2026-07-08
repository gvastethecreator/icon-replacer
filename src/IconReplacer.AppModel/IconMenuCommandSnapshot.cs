using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconMenuCommandSnapshot(
    string IconLibraryRoot,
    IconMenuState State,
    string StatusMessage,
    int TotalIconCount,
    int VisibleIconCommandCount,
    int OmittedIconCount,
    IReadOnlyList<IconMenuCommand> Commands,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt)
{
    public bool IsTruncated => OmittedIconCount > 0;

    public bool IsAvailable => State != IconMenuState.Unavailable;
}
