using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconApplyResult(
    TargetKind TargetKind,
    RestoreRecord RestoreRecord,
    IconLibraryEntry ImportedIcon,
    string? DesktopIniPath,
    ShellLinkInfo? Shortcut,
    bool ExplorerRefreshRequested);
