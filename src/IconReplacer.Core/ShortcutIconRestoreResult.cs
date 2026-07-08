namespace IconReplacer.Core;

public sealed record ShortcutIconRestoreResult(
    RestoreRecord RestoreRecord,
    ShellLinkInfo Shortcut,
    bool ExplorerRefreshRequested);

