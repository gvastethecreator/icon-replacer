using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class SetupReadinessService
{
    private readonly DashboardService _dashboardService;
    private readonly ShellIntegrationReadiness _shellIntegrationReadiness;

    public SetupReadinessService(
        DashboardService? dashboardService = null,
        ShellIntegrationReadiness shellIntegrationReadiness = ShellIntegrationReadiness.NotConfigured)
    {
        _dashboardService = dashboardService ?? new DashboardService();
        _shellIntegrationReadiness = shellIntegrationReadiness;
    }

    public OperationResult<SetupReadinessSnapshot> GetSnapshot(IconLibraryPaths paths)
    {
        var libraryExistedBeforeScan = Directory.Exists(paths.LibraryRoot);
        var importedExistedBeforeScan = Directory.Exists(paths.ImportedRoot);
        var dashboard = _dashboardService.GetSnapshot(paths);
        if (!dashboard.Succeeded || dashboard.Value is null)
        {
            return OperationResult<SetupReadinessSnapshot>.Failure(dashboard.Error);
        }

        var actions = BuildActions(dashboard.Value, _shellIntegrationReadiness);
        return OperationResult<SetupReadinessSnapshot>.Success(new SetupReadinessSnapshot(
            dashboard.Value.IconLibraryRoot,
            dashboard.Value.ImportedIconsRoot,
            dashboard.Value.RestoreStateFile,
            libraryExistedBeforeScan || Directory.Exists(paths.LibraryRoot),
            importedExistedBeforeScan || Directory.Exists(paths.ImportedRoot),
            CanUseCoreFeatures(dashboard.Value),
            dashboard.Value.IconCount > 0,
            dashboard.Value.CategoryCount,
            dashboard.Value.IconCount,
            dashboard.Value.CatalogWarningCount,
            dashboard.Value.RestoreRecordCount,
            dashboard.Value.RestorableRecordCount,
            dashboard.Value.MissingTargetRecordCount,
            _shellIntegrationReadiness,
            actions));
    }

    public OperationResult<SetupReadinessSnapshot> GetSnapshotFromEnvironment()
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<SetupReadinessSnapshot>.Failure(paths.Error);
        }

        return GetSnapshot(paths.Value);
    }

    private static bool CanUseCoreFeatures(DashboardSnapshot dashboard)
    {
        return Directory.Exists(dashboard.IconLibraryRoot) &&
            Directory.Exists(dashboard.ImportedIconsRoot);
    }

    private static IReadOnlyList<SetupAction> BuildActions(
        DashboardSnapshot dashboard,
        ShellIntegrationReadiness shellIntegrationReadiness)
    {
        var actions = new List<SetupAction>();

        if (dashboard.IconCount == 0)
        {
            actions.Add(new SetupAction(
                "import-icons",
                SetupActionSeverity.Warning,
                "Import icons",
                "The Icon Library is empty. Import .ico files or add folders under .icons."));
        }

        if (dashboard.CatalogWarningCount > 0)
        {
            actions.Add(new SetupAction(
                "review-catalog-warnings",
                SetupActionSeverity.Warning,
                "Review catalog warnings",
                "Some .ico files could not be read and will not appear in the menu."));
        }

        if (dashboard.RestorableRecordCount > 0)
        {
            actions.Add(new SetupAction(
                "review-restorable-records",
                SetupActionSeverity.Info,
                "Review restorable changes",
                "Recent icon changes can be restored from history."));
        }

        if (dashboard.MissingTargetRecordCount > 0)
        {
            actions.Add(new SetupAction(
                "review-missing-targets",
                SetupActionSeverity.Info,
                "Review missing targets",
                "Some history entries point to targets that no longer exist."));
        }

        if (shellIntegrationReadiness == ShellIntegrationReadiness.DecisionPending)
        {
            actions.Add(new SetupAction(
                "resolve-shell-integration",
                SetupActionSeverity.Warning,
                "Resolve shell integration",
                "Explorer integration is waiting for the Modern vs Classic V1 decision."));
        }
        else if (shellIntegrationReadiness == ShellIntegrationReadiness.NotConfigured)
        {
            actions.Add(new SetupAction(
                "configure-shell-integration",
                SetupActionSeverity.Warning,
                "Configure shell integration",
                "Explorer integration is not installed yet."));
        }
        else if (shellIntegrationReadiness == ShellIntegrationReadiness.Unavailable)
        {
            actions.Add(new SetupAction(
                "shell-integration-unavailable",
                SetupActionSeverity.Blocking,
                "Shell integration unavailable",
                "Explorer integration cannot run in the current environment."));
        }

        return actions;
    }
}
