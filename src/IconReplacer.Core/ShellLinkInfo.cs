namespace IconReplacer.Core;

public sealed record ShellLinkInfo(
    string FullPath,
    string TargetPath,
    string Arguments,
    string WorkingDirectory,
    string Description,
    short Hotkey,
    string? IconPath,
    int IconIndex);

