namespace IconReplacer.AppModel;

public sealed record ShellSelectionItem(
    string Path,
    bool? IsDirectory = null);
