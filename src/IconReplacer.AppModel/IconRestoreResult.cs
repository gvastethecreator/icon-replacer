using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconRestoreResult(
    TargetKind TargetKind,
    RestoreRecord RestoreRecord,
    string? DesktopIniPath,
    ShellLinkInfo? Shortcut,
    bool ExplorerRefreshRequested);
