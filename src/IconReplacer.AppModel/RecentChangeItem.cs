using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record RecentChangeItem(
    Guid Id,
    TargetKind TargetKind,
    RestoreRecordStatus Status,
    DateTimeOffset CreatedAt,
    string TargetPath,
    string AppliedIconPath,
    bool TargetExists,
    bool AppliedIconExists,
    bool CanRestore,
    RecentChangeActionState RestoreActionState,
    string RestoreActionLabel,
    string HealthText,
    string? WarningText)
{
    public bool IsRestoreEnabled => RestoreActionState is
        RecentChangeActionState.Enabled or
        RecentChangeActionState.EnabledWithWarning;

    public static RecentChangeItem FromSummary(RestoreRecordSummary summary)
    {
        var actionState = GetRestoreActionState(summary);
        return new RecentChangeItem(
            summary.Id,
            summary.TargetKind,
            summary.Status,
            summary.CreatedAt,
            summary.TargetPath,
            summary.AppliedIconPath,
            summary.TargetExists,
            summary.AppliedIconExists,
            summary.CanRestore,
            actionState,
            GetRestoreActionLabel(actionState),
            GetHealthText(summary),
            GetWarningText(summary, actionState));
    }

    private static RecentChangeActionState GetRestoreActionState(RestoreRecordSummary summary)
    {
        if (!summary.CanRestore)
        {
            return RecentChangeActionState.Disabled;
        }

        return summary.AppliedIconExists
            ? RecentChangeActionState.Enabled
            : RecentChangeActionState.EnabledWithWarning;
    }

    private static string GetRestoreActionLabel(RecentChangeActionState actionState)
    {
        return actionState == RecentChangeActionState.Disabled
            ? "Restore unavailable"
            : "Restore";
    }

    private static string GetHealthText(RestoreRecordSummary summary)
    {
        if (!summary.TargetExists)
        {
            return "Target missing";
        }

        if (summary.Status != RestoreRecordStatus.Applied)
        {
            return summary.Status == RestoreRecordStatus.Restored
                ? "Already restored"
                : $"Not actionable: {summary.Status}";
        }

        if (!summary.AppliedIconExists)
        {
            return "Applied icon missing";
        }

        return "Restorable";
    }

    private static string? GetWarningText(
        RestoreRecordSummary summary,
        RecentChangeActionState actionState)
    {
        if (!summary.TargetExists)
        {
            return "The target no longer exists.";
        }

        if (summary.Status != RestoreRecordStatus.Applied)
        {
            return "Only currently applied changes can be restored.";
        }

        if (actionState == RecentChangeActionState.EnabledWithWarning)
        {
            return "The applied icon file is missing, but restore can use the saved previous state.";
        }

        return null;
    }
}
