namespace IconReplacer.AppModel;

public sealed record AppDiagnosticsSnapshot(
    SetupReadinessSnapshot Setup,
    DashboardSnapshot Dashboard,
    IReadOnlyList<AppLocationTarget> Locations,
    WinUiToolingSnapshot WinUiTooling,
    NativeToolingSnapshot NativeTooling,
    IReadOnlyList<AppDiagnosticCheck> Checks,
    DateTimeOffset RefreshedAt)
{
    public int BlockingCount => Checks.Count(check => check.Status == AppDiagnosticStatus.Blocking);

    public int WarningCount => Checks.Count(check => check.Status == AppDiagnosticStatus.Warning);

    public bool HasBlockingIssues => BlockingCount > 0;
}
