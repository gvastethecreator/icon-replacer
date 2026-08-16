using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class SetupReadinessService
{
    private readonly DashboardService _dashboardService;
    private readonly PackagingPlanService _packagingPlanService;
    private readonly ShellIntegrationReadiness _shellIntegrationReadiness;

    public SetupReadinessService(
        DashboardService? dashboardService = null,
        PackagingPlanService? packagingPlanService = null,
        ShellIntegrationReadiness shellIntegrationReadiness = ShellIntegrationReadiness.NotConfigured)
    {
        _dashboardService = dashboardService ?? new DashboardService();
        _packagingPlanService = packagingPlanService ?? new PackagingPlanService();
        _shellIntegrationReadiness = shellIntegrationReadiness;
    }

    public OperationResult<SetupReadinessSnapshot> GetSnapshot(
        IconLibraryPaths paths,
        PackagingPlanInputs? packagingInputs = null)
    {
        var libraryExistedBeforeScan = Directory.Exists(paths.LibraryRoot);
        var importedExistedBeforeScan = Directory.Exists(paths.ImportedRoot);
        var dashboard = _dashboardService.GetSnapshot(paths);
        if (!dashboard.Succeeded || dashboard.Value is null)
        {
            return OperationResult<SetupReadinessSnapshot>.Failure(dashboard.Error);
        }

        var packagingPlan = packagingInputs is null
            ? null
            : _packagingPlanService.GetPlan(packagingInputs);
        var actions = BuildActions(dashboard.Value, _shellIntegrationReadiness, packagingPlan);
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

    public OperationResult<SetupReadinessSnapshot> GetSnapshotFromEnvironment(
        PackagingPlanInputs? packagingInputs = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<SetupReadinessSnapshot>.Failure(paths.Error);
        }

        return GetSnapshot(paths.Value, packagingInputs);
    }

    private static bool CanUseCoreFeatures(DashboardSnapshot dashboard)
    {
        return Directory.Exists(dashboard.IconLibraryRoot) &&
            Directory.Exists(dashboard.ImportedIconsRoot);
    }

    private static IReadOnlyList<SetupAction> BuildActions(
        DashboardSnapshot dashboard,
        ShellIntegrationReadiness shellIntegrationReadiness,
        PackagingPlanSnapshot? packagingPlan)
    {
        var actions = new List<SetupAction>();

        if (dashboard.IconCount == 0)
        {
            actions.Add(new SetupAction(
                SetupActionIds.ImportIcons,
                SetupActionSeverity.Warning,
                "Import icons",
                "The Icon Library is empty. Import .ico files or add folders under .icons."));
        }

        if (dashboard.CatalogWarningCount > 0)
        {
            actions.Add(new SetupAction(
                SetupActionIds.ReviewCatalogWarnings,
                SetupActionSeverity.Warning,
                "Review catalog warnings",
                "Some .ico files could not be read and will not appear in the menu."));
        }

        if (dashboard.RestorableRecordCount > 0)
        {
            actions.Add(new SetupAction(
                SetupActionIds.ReviewRestorableRecords,
                SetupActionSeverity.Info,
                "Review restorable changes",
                "Recent icon changes can be restored from history."));
        }

        if (dashboard.MissingTargetRecordCount > 0)
        {
            actions.Add(new SetupAction(
                SetupActionIds.ReviewMissingTargets,
                SetupActionSeverity.Info,
                "Review missing targets",
                "Some history entries point to targets that no longer exist."));
        }

        if (shellIntegrationReadiness == ShellIntegrationReadiness.DecisionPending)
        {
            actions.Add(new SetupAction(
                SetupActionIds.ResolveShellIntegration,
                SetupActionSeverity.Warning,
                "Refresh shell integration status",
                "This setup state predates the final packaged dual Explorer decision."));
        }
        else if (shellIntegrationReadiness == ShellIntegrationReadiness.NotConfigured)
        {
            if (packagingPlan is not null &&
                (packagingPlan.HasBlockingIssues || packagingPlan.WarningCount > 0))
            {
                actions.Add(CreatePackagePlanAction(packagingPlan));
            }

            actions.Add(new SetupAction(
                SetupActionIds.ConfigureShellIntegration,
                SetupActionSeverity.Warning,
                "Configure shell integration",
                "Explorer integration is not installed yet."));
        }
        else if (shellIntegrationReadiness == ShellIntegrationReadiness.Unavailable)
        {
            actions.Add(new SetupAction(
                SetupActionIds.ShellIntegrationUnavailable,
                SetupActionSeverity.Blocking,
                "Shell integration unavailable",
                "Explorer integration cannot run in the current environment."));
        }

        return actions;
    }

    private static SetupAction CreatePackagePlanAction(PackagingPlanSnapshot packagingPlan)
    {
        var severity = packagingPlan.HasBlockingIssues
            ? SetupActionSeverity.Blocking
            : SetupActionSeverity.Warning;
        var detail = packagingPlan.HasBlockingIssues
            ? $"{packagingPlan.BlockingCount} package blockers must be resolved before Explorer integration can be installed."
            : $"{packagingPlan.WarningCount} package proof items remain before release.";

        return new SetupAction(
            SetupActionIds.ReviewPackagePlan,
            severity,
            "Review package plan",
            detail);
    }
}
