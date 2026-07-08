namespace IconReplacer.Core;

public sealed record FolderIconApplyResult(
    RestoreRecord RestoreRecord,
    IconLibraryEntry ImportedIcon,
    string DesktopIniPath,
    bool ExplorerRefreshRequested);

