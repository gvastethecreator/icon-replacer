namespace IconReplacer.AppModel;

public sealed record ReleaseReadinessSnapshot(
    string IntegrationPath,
    IReadOnlyList<ReleaseReadinessItem> Items,
    PackagingPlanSnapshot PackagingPlan,
    AccessibilityPlanSnapshot AccessibilityPlan,
    AppDiagnosticsSnapshot Diagnostics,
    DateTimeOffset RefreshedAt)
{
    public int BlockingCount => Items.Count(item => item.Status == AppDiagnosticStatus.Blocking);

    public int WarningCount => Items.Count(item => item.Status == AppDiagnosticStatus.Warning);

    public int PassCount => Items.Count(item => item.Status == AppDiagnosticStatus.Pass);

    public int RequiredCount => Items.Count(item => item.RequiredForRelease);

    public bool IsReadyForRelease => BlockingCount == 0 && WarningCount == 0;
}
