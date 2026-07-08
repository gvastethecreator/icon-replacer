namespace IconReplacer.AppModel;

public sealed record IconMenuCommand(
    string Id,
    IconMenuCommandKind Kind,
    string Label,
    string? CategoryName,
    string? IconPath,
    bool RequiresTarget,
    IReadOnlyList<string> ArgumentTemplate,
    string DisplayArguments);
