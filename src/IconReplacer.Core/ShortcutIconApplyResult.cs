namespace IconReplacer.Core;

public sealed record ShortcutIconApplyResult(
    RestoreRecord RestoreRecord,
    IconLibraryEntry ImportedIcon,
    ShellLinkInfo Shortcut,
    bool ExplorerRefreshRequested);

