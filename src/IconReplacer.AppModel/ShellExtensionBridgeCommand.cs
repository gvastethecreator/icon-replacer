using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record ShellExtensionBridgeCommand(
    string Id,
    IconMenuCommandKind Kind,
    string Label,
    string? CategoryName,
    string? IconPath,
    bool RequiresTarget,
    bool CanInvoke,
    IReadOnlyList<string> ResolvedArguments,
    string DisplayArguments,
    IconReplacerError Error);
