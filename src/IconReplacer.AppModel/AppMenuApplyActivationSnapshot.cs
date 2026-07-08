using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record AppMenuApplyActivationSnapshot(
    string VerbName,
    string RequestedTargetPath,
    string RequestedIconPath,
    IconMenuCommand? Command,
    IconMenuCommandInvocationSnapshot? Invocation,
    bool CanApply,
    IReadOnlyList<string> AppArguments,
    string DisplayArguments,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt);
