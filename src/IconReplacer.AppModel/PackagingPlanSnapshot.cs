namespace IconReplacer.AppModel;

public sealed record PackagingPlanSnapshot(
    string InstallMode,
    string PackageName,
    bool RequiresPackageIdentity,
    bool RequiresDevSigning,
    bool RemovesShellIntegrationOnUninstall,
    bool PreservesIconLibraryOnUninstall,
    bool PreservesRestoreHistoryByDefault,
    ShellManifestContractSnapshot ManifestContract,
    IReadOnlyList<ShellIntegrationPlanItem> Items,
    DateTimeOffset RefreshedAt)
{
    public int BlockingCount => Items.Count(item => item.Status == AppDiagnosticStatus.Blocking);

    public int WarningCount => Items.Count(item => item.Status == AppDiagnosticStatus.Warning);

    public bool HasBlockingIssues => BlockingCount > 0;
}
