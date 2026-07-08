using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppActionRequestService
{
    private readonly SetupReadinessService _setupReadinessService;

    public AppActionRequestService(SetupReadinessService? setupReadinessService = null)
    {
        _setupReadinessService = setupReadinessService ?? new SetupReadinessService();
    }

    public OperationResult<AppActionRequestSnapshot> CreateRequest(
        string actionId,
        IconLibraryPaths paths,
        PackagingPlanInputs? packagingInputs = null)
    {
        if (string.IsNullOrWhiteSpace(actionId))
        {
            return OperationResult<AppActionRequestSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "An app action id is required."));
        }

        var fallbackAction = GetKnownAction(actionId);
        if (fallbackAction is null)
        {
            return OperationResult<AppActionRequestSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The app action is not supported.",
                actionId));
        }

        var setup = _setupReadinessService.GetSnapshot(paths, packagingInputs);
        if (!setup.Succeeded || setup.Value is null)
        {
            return OperationResult<AppActionRequestSnapshot>.Failure(setup.Error);
        }

        var currentAction = setup.Value.Actions.FirstOrDefault(action =>
            string.Equals(action.Id, fallbackAction.Id, StringComparison.OrdinalIgnoreCase));
        if (currentAction is null)
        {
            return OperationResult<AppActionRequestSnapshot>.Success(CreateDisabledRequest(fallbackAction));
        }

        return OperationResult<AppActionRequestSnapshot>.Success(CreateEnabledRequest(currentAction));
    }

    public OperationResult<AppActionRequestSnapshot> CreateRequestFromEnvironment(
        string actionId,
        PackagingPlanInputs? packagingInputs = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppActionRequestSnapshot>.Failure(paths.Error);
        }

        return CreateRequest(actionId, paths.Value, packagingInputs);
    }

    private static AppActionRequestSnapshot CreateEnabledRequest(SetupAction action)
    {
        var (kind, navigationTarget, historyFilter) = GetActionTarget(action.Id);
        return new AppActionRequestSnapshot(
            action,
            kind,
            CanExecute: true,
            navigationTarget,
            historyFilter,
            IconReplacerError.None,
            DateTimeOffset.UtcNow);
    }

    private static AppActionRequestSnapshot CreateDisabledRequest(SetupAction action)
    {
        var (kind, navigationTarget, historyFilter) = GetActionTarget(action.Id);
        return new AppActionRequestSnapshot(
            action,
            kind,
            CanExecute: false,
            navigationTarget,
            historyFilter,
            new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The app action is not available in the current app state.",
                action.Id),
            DateTimeOffset.UtcNow);
    }

    private static (AppActionKind Kind, string NavigationTarget, RestoreHistoryFilter? HistoryFilter) GetActionTarget(
        string actionId)
    {
        return actionId switch
        {
            SetupActionIds.ImportIcons => (AppActionKind.ImportIcons, AppNavigationRouteIds.ImportIcons, null),
            SetupActionIds.ReviewCatalogWarnings => (AppActionKind.ShowIconBrowser, AppNavigationRouteIds.IconBrowser, null),
            SetupActionIds.ReviewRestorableRecords => (AppActionKind.ShowRestoreHistory, AppNavigationRouteIds.History, RestoreHistoryFilter.Restorable),
            SetupActionIds.ReviewMissingTargets => (AppActionKind.ShowRestoreHistory, AppNavigationRouteIds.History, RestoreHistoryFilter.Stale),
            SetupActionIds.ReviewPackagePlan => (AppActionKind.ShowPackagePlan, AppNavigationRouteIds.PackagePlan, null),
            SetupActionIds.ResolveShellIntegration => (AppActionKind.ShowShellIntegrationPlan, AppNavigationRouteIds.ShellPlan, null),
            SetupActionIds.ConfigureShellIntegration => (AppActionKind.ShowShellIntegrationPlan, AppNavigationRouteIds.ShellPlan, null),
            SetupActionIds.ShellIntegrationUnavailable => (AppActionKind.ShowDiagnostics, AppNavigationRouteIds.Diagnostics, null),
            _ => (AppActionKind.ShowDiagnostics, AppNavigationRouteIds.Diagnostics, null)
        };
    }

    private static SetupAction? GetKnownAction(string actionId)
    {
        return actionId.ToLowerInvariant() switch
        {
            SetupActionIds.ImportIcons => new SetupAction(
                SetupActionIds.ImportIcons,
                SetupActionSeverity.Warning,
                "Import icons",
                "The Icon Library is empty. Import .ico files or add folders under .icons."),
            SetupActionIds.ReviewCatalogWarnings => new SetupAction(
                SetupActionIds.ReviewCatalogWarnings,
                SetupActionSeverity.Warning,
                "Review catalog warnings",
                "Some .ico files could not be read and will not appear in the menu."),
            SetupActionIds.ReviewRestorableRecords => new SetupAction(
                SetupActionIds.ReviewRestorableRecords,
                SetupActionSeverity.Info,
                "Review restorable changes",
                "Recent icon changes can be restored from history."),
            SetupActionIds.ReviewMissingTargets => new SetupAction(
                SetupActionIds.ReviewMissingTargets,
                SetupActionSeverity.Info,
                "Review missing targets",
                "Some history entries point to targets that no longer exist."),
            SetupActionIds.ReviewPackagePlan => new SetupAction(
                SetupActionIds.ReviewPackagePlan,
                SetupActionSeverity.Blocking,
                "Review package plan",
                "Package blockers must be resolved before Explorer integration can be installed."),
            SetupActionIds.ResolveShellIntegration => new SetupAction(
                SetupActionIds.ResolveShellIntegration,
                SetupActionSeverity.Warning,
                "Resolve shell integration",
                "Explorer integration is waiting for the Modern vs Classic V1 decision."),
            SetupActionIds.ConfigureShellIntegration => new SetupAction(
                SetupActionIds.ConfigureShellIntegration,
                SetupActionSeverity.Warning,
                "Configure shell integration",
                "Explorer integration is not installed yet."),
            SetupActionIds.ShellIntegrationUnavailable => new SetupAction(
                SetupActionIds.ShellIntegrationUnavailable,
                SetupActionSeverity.Blocking,
                "Shell integration unavailable",
                "Explorer integration cannot run in the current environment."),
            _ => null
        };
    }
}
