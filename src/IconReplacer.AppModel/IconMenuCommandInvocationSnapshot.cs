using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record IconMenuCommandInvocationSnapshot(
    IconMenuCommand Command,
    ShellSelectionEvaluation? Selection,
    bool CanInvoke,
    IReadOnlyList<string> ResolvedArguments,
    string DisplayArguments,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
