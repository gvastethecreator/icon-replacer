namespace IconReplacer.Core;

public sealed record FolderIconRestoreResult(
    RestoreRecord RestoreRecord,
    string DesktopIniPath,
    bool ExplorerRefreshRequested);

