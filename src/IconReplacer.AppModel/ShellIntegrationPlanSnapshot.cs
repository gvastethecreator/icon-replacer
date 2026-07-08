namespace IconReplacer.AppModel;

public sealed record ShellIntegrationPlanSnapshot(
    ShellIntegrationMode SelectedMode,
    ShellIntegrationMode FallbackMode,
    ShellIntegrationReadiness Readiness,
    bool DecisionFinal,
    string SelectedModeName,
    string FallbackModeName,
    string Rationale,
    IReadOnlyList<ShellIntegrationPlanItem> Items,
    DateTimeOffset RefreshedAt)
{
    public int BlockingCount => Items.Count(item => item.Status == AppDiagnosticStatus.Blocking);

    public int WarningCount => Items.Count(item => item.Status == AppDiagnosticStatus.Warning);

    public bool HasBlockingIssues => BlockingCount > 0;
}
