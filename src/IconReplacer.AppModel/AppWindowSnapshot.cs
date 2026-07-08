using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record AppWindowSnapshot(
    AppNavigationPlanSnapshot Navigation,
    AppActivationSnapshot? Activation,
    AppDiagnosticsSnapshot Diagnostics,
    string SelectedRouteId,
    string WindowTitle,
    bool CanUseSelectedRoute,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt)
{
    public bool HasBlockingDiagnostics => Diagnostics.HasBlockingIssues;

    public int BlockingCount => Diagnostics.BlockingCount;

    public int WarningCount => Diagnostics.WarningCount;
}
