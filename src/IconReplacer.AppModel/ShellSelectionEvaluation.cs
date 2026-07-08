using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record ShellSelectionEvaluation(
    ShellSelectionStatus Status,
    bool CanShowChangeIcon,
    TargetItem? Target,
    string? ResolvedPath,
    IconReplacerError Error)
{
    public static ShellSelectionEvaluation Enabled(TargetItem target)
    {
        return new ShellSelectionEvaluation(
            ShellSelectionStatus.Supported,
            CanShowChangeIcon: true,
            target,
            target.FullPath,
            IconReplacerError.None);
    }

    public static ShellSelectionEvaluation Disabled(
        ShellSelectionStatus status,
        IconReplacerError error,
        string? resolvedPath = null)
    {
        return new ShellSelectionEvaluation(
            status,
            CanShowChangeIcon: false,
            Target: null,
            resolvedPath,
            error);
    }
}
