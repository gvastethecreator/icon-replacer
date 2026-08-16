using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppCommandService
{
    private readonly AppWindowService _windowService;
    private readonly AppChangeIconWorkflowService _changeIconWorkflowService;
    private readonly IconDetailsService _detailsService;
    private readonly AppRestoreWorkflowService _restoreWorkflowService;

    public AppCommandService(
        AppWindowService? windowService = null,
        AppChangeIconWorkflowService? changeIconWorkflowService = null,
        IconDetailsService? detailsService = null,
        AppRestoreWorkflowService? restoreWorkflowService = null)
    {
        _windowService = windowService ?? new AppWindowService();
        _changeIconWorkflowService = changeIconWorkflowService ?? new AppChangeIconWorkflowService();
        _detailsService = detailsService ?? new IconDetailsService();
        _restoreWorkflowService = restoreWorkflowService ?? new AppRestoreWorkflowService();
    }

    public OperationResult<AppCommandStateSnapshot> GetCommands(
        AppWindowSnapshot window,
        string? routeId = null,
        AppRestoreWorkflowSnapshot? restoreWorkflow = null,
        AppChangeIconWorkflowSnapshot? changeIconWorkflow = null,
        IconDetailsSnapshot? iconDetails = null)
    {
        if (window is null)
        {
            return OperationResult<AppCommandStateSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "App window state is required."));
        }

        var effectiveRouteId = string.IsNullOrWhiteSpace(routeId)
            ? window.SelectedRouteId
            : routeId;
        var route = window.Navigation.FindRoute(effectiveRouteId);
        if (route is null)
        {
            return OperationResult<AppCommandStateSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The app route is not registered.",
                effectiveRouteId));
        }

        return OperationResult<AppCommandStateSnapshot>.Success(new AppCommandStateSnapshot(
            route.RouteId,
            route.Title,
            BuildCommands(window, route.RouteId, restoreWorkflow, changeIconWorkflow, iconDetails),
            DateTimeOffset.UtcNow));
    }

    public OperationResult<AppCommandStateSnapshot> GetCommandsFromEnvironment(
        IReadOnlyList<string> activationArguments,
        string? routeId,
        WinUiToolingSnapshot? winUiTooling = null,
        Guid? restoreRecordId = null,
        RestoreHistoryFilter restoreHistoryFilter = RestoreHistoryFilter.All,
        string? selectedIconPath = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppCommandStateSnapshot>.Failure(paths.Error);
        }

        var window = _windowService.GetWindow(activationArguments, paths.Value, winUiTooling);
        if (!window.Succeeded || window.Value is null)
        {
            return OperationResult<AppCommandStateSnapshot>.Failure(window.Error);
        }

        AppRestoreWorkflowSnapshot? restoreWorkflow = null;
        AppChangeIconWorkflowSnapshot? changeIconWorkflow = null;
        IconDetailsSnapshot? iconDetails = null;
        var effectiveRouteId = string.IsNullOrWhiteSpace(routeId)
            ? window.Value.SelectedRouteId
            : routeId;
        if (effectiveRouteId == AppNavigationRouteIds.RestorePreview)
        {
            var workflow = _restoreWorkflowService.GetWorkflow(paths.Value, restoreRecordId, restoreHistoryFilter);
            if (!workflow.Succeeded || workflow.Value is null)
            {
                return OperationResult<AppCommandStateSnapshot>.Failure(workflow.Error);
            }

            restoreWorkflow = workflow.Value;
        }
        else if (effectiveRouteId == AppNavigationRouteIds.ChangeIcon)
        {
            var workflow = _changeIconWorkflowService.GetWorkflow(activationArguments, paths.Value, selectedIconPath);
            if (workflow.Succeeded && workflow.Value is not null)
            {
                changeIconWorkflow = workflow.Value;
            }
        }
        else if (effectiveRouteId == AppNavigationRouteIds.IconDetails &&
            !string.IsNullOrWhiteSpace(selectedIconPath))
        {
            var details = _detailsService.GetDetails(selectedIconPath, paths.Value);
            if (details.Succeeded && details.Value is not null)
            {
                iconDetails = details.Value;
            }
        }

        return GetCommands(window.Value, routeId, restoreWorkflow, changeIconWorkflow, iconDetails);
    }

    private static IReadOnlyList<AppCommandDescriptor> BuildCommands(
        AppWindowSnapshot window,
        string routeId,
        AppRestoreWorkflowSnapshot? restoreWorkflow,
        AppChangeIconWorkflowSnapshot? changeIconWorkflow,
        IconDetailsSnapshot? iconDetails)
    {
        return routeId switch
        {
            AppNavigationRouteIds.Home => new[]
            {
                Navigate("import-icons", "Import icons", AppNavigationRouteIds.ImportIcons, "Open the import workflow."),
                Navigate("browse-icons", "Browse Icon Library", AppNavigationRouteIds.IconBrowser, "Open catalog search and filters."),
                Navigate("recent-changes", "Recent changes", AppNavigationRouteIds.History, "Open restore history."),
                Navigate("diagnostics", "Diagnostics", AppNavigationRouteIds.Diagnostics, "Open readiness and blocker details.")
            },
            AppNavigationRouteIds.IconBrowser => new[]
            {
                Workflow("search-icons", "Search icons", "Filter the visible catalog by text."),
                Workflow("filter-category", "Filter by category", "Filter icons by one Icon Library collection."),
                DisabledWorkflow("open-icon-details", "Open icon details", "Select an icon before opening details."),
                Navigate("import-icons", "Import icons", AppNavigationRouteIds.ImportIcons, "Open the import workflow.")
            },
            AppNavigationRouteIds.IconDetails => IconDetailsCommands(iconDetails),
            AppNavigationRouteIds.ImportIcons => new[]
            {
                Workflow("choose-ico-files", "Choose .ico files", "Open the multi-select .ico import picker."),
                Workflow("choose-collection", "Choose collection", "Target Imported or a one-level collection."),
                DisabledWorkflow("review-import-results", "Review import results", "Run an import before reviewing results.")
            },
            AppNavigationRouteIds.Collections => new[]
            {
                Workflow("create-collection", "Create collection", "Create a one-level Icon Library folder."),
                Workflow("import-into-collection", "Import into collection", "Import .ico files into the selected collection."),
                OpenLocation("open-icon-library", "Open collection", AppLocationKind.IconLibrary, "Open the Icon Library location.")
            },
            AppNavigationRouteIds.History => new[]
            {
                Workflow("filter-history", "Filter history", "Switch between all, restorable, applied, restored, and stale records."),
                DisabledWorkflow("preview-restore", "Preview restore", "Select a restorable history row before previewing restore."),
                DisabledWorkflow("restore-selected-change", "Restore selected change", "Preview a restorable history row before restoring.")
            },
            AppNavigationRouteIds.RestorePreview => RestorePreviewCommands(restoreWorkflow),
            AppNavigationRouteIds.Diagnostics => DiagnosticsCommands(window),
            AppNavigationRouteIds.ShellPlan => new[]
            {
                Navigate("review-shell-bridge", "Review shell bridge", AppNavigationRouteIds.ShellBridge, "Open the native Explorer command bridge preview."),
                Workflow("review-manifest-contract", "Review manifest contract", "Inspect the MSIX COM/context-menu manifest contract."),
                Navigate("review-package-plan", "Review package plan", AppNavigationRouteIds.PackagePlan, "Open install and uninstall gates."),
                Navigate("open-diagnostics", "Open diagnostics", AppNavigationRouteIds.Diagnostics, "Open diagnostics.")
            },
            AppNavigationRouteIds.ShellBridge => new[]
            {
                Workflow("review-resolved-commands", "Review resolved commands", "Inspect command ids and AppModel arguments for Explorer."),
                Workflow("review-safety-rules", "Review safety rules", "Inspect Explorer enumeration and mutation boundaries."),
                Navigate("review-shell-plan", "Review shell plan", AppNavigationRouteIds.ShellPlan, "Return to shell integration planning."),
                Navigate("open-diagnostics", "Open diagnostics", AppNavigationRouteIds.Diagnostics, "Open diagnostics.")
            },
            AppNavigationRouteIds.PackagePlan => new[]
            {
                Workflow("review-install-blockers", "Review install blockers", "Inspect package identity, signing, native extension, and install blockers."),
                Workflow("review-uninstall-policy", "Review uninstall policy", "Inspect shell-removal and user-data preservation policy."),
                Navigate("open-diagnostics", "Open diagnostics", AppNavigationRouteIds.Diagnostics, "Open diagnostics.")
            },
            AppNavigationRouteIds.AccessibilityPlan => new[]
            {
                Workflow("review-accessibility-requirements", "Review requirements", "Inspect keyboard, names, and visual adaptation requirements."),
                Workflow("review-accessibility-surfaces", "Review app surfaces", "Inspect surfaces covered by accessibility acceptance."),
                ManualProof("capture-manual-proof", "Capture manual proof", "Capture screenshots or notes after WinUI exists.")
            },
            AppNavigationRouteIds.ChangeIcon => ChangeIconCommands(window, changeIconWorkflow),
            _ => []
        };
    }

    private static IReadOnlyList<AppCommandDescriptor> RestorePreviewCommands(AppRestoreWorkflowSnapshot? workflow)
    {
        var confirm = workflow?.CanRestore == true
            ? Workflow("confirm-restore", "Confirm restore", "Restore the selected record.")
            : DisabledWorkflow(
                "confirm-restore",
                "Confirm restore",
                workflow is null || workflow.Step == AppRestoreWorkflowStep.NeedRecord
                    ? "Select a restorable history row before confirming restore."
                    : workflow.Error.Message);

        var reviewReason = workflow is not null && workflow.Error.Code != ErrorCode.None
            ? Workflow("review-disabled-reason", "Review disabled reason", workflow.Error.Message)
            : DisabledWorkflow("review-disabled-reason", "Review disabled reason", "No blocking restore reason is currently present.");

        return new[]
        {
            confirm,
            Navigate("cancel-restore", "Cancel restore", AppNavigationRouteIds.History, "Return to recent changes."),
            reviewReason
        };
    }

    private static IReadOnlyList<AppCommandDescriptor> IconDetailsCommands(IconDetailsSnapshot? details)
    {
        if (details is null)
        {
            return new[]
            {
                DisabledWorkflow("use-selected-icon", "Use selected icon", "Select a supported shell target before applying this icon."),
                DisabledWorkflow("copy-icon-path", "Copy icon path", "Select an icon before copying its path."),
                DisabledOpenLocation("open-containing-folder", "Open containing folder", "Select an icon before opening its folder.")
            };
        }

        return new[]
        {
            DisabledWorkflow("use-selected-icon", "Use selected icon", "Select a supported shell target before applying this icon."),
            Workflow("copy-icon-path", "Copy icon path", $"Copy {details.FullPath}."),
            Workflow("open-containing-folder", "Open containing folder", $"Open {Path.GetDirectoryName(details.FullPath) ?? details.FullPath}.")
        };
    }

    private static IReadOnlyList<AppCommandDescriptor> DiagnosticsCommands(AppWindowSnapshot window)
    {
        var blockers = window.BlockingCount;
        return new[]
        {
            Refresh("refresh-diagnostics", "Refresh diagnostics", "Refresh readiness, locations, shell, and tooling checks."),
            OpenLocation("open-app-data", "Open app data", AppLocationKind.AppData, "Open the app data folder."),
            blockers > 0
                ? Workflow("review-blockers", "Review blockers", $"{blockers} blocking issue(s) need attention.")
                : DisabledWorkflow("review-blockers", "Review blockers", "No blocking diagnostics are currently present.")
        };
    }

    private static IReadOnlyList<AppCommandDescriptor> ChangeIconCommands(
        AppWindowSnapshot window,
        AppChangeIconWorkflowSnapshot? workflow)
    {
        if (workflow is not null)
        {
            return new[]
            {
                workflow.CanOpenPicker
                    ? Workflow("choose-icon", "Choose icon", "Open the .ico picker for the selected shell target.")
                    : DisabledWorkflow("choose-icon", "Choose icon", workflow.Error.Message),
                workflow.CanPreview
                    ? Workflow("preview-change", "Preview change", "Review target and selected icon before applying.")
                    : DisabledWorkflow("preview-change", "Preview change", "Choose an icon before previewing the change."),
                workflow.CanApply
                    ? Workflow("apply-change", "Apply change", "Apply the selected icon through the shared mutation path.")
                    : DisabledWorkflow(
                        "apply-change",
                        "Apply change",
                        workflow.Error.Code == ErrorCode.None
                            ? "Preview a valid target and icon before applying."
                            : workflow.Error.Message)
            };
        }

        var canChooseIcon = window.CanUseSelectedRoute && window.Activation?.Kind == AppActivationKind.ChangeIcon;
        var chooseIcon = canChooseIcon
            ? Workflow("choose-icon", "Choose icon", "Open the .ico picker for the selected shell target.")
            : DisabledWorkflow(
                "choose-icon",
                "Choose icon",
                window.Activation?.Kind == AppActivationKind.ChangeIcon
                    ? window.Error.Message
                    : "Select one local folder, directory link, or .lnk shortcut before changing its icon.");

        return new[]
        {
            chooseIcon,
            DisabledWorkflow("preview-change", "Preview change", "Choose an icon before previewing the change."),
            DisabledWorkflow("apply-change", "Apply change", "Preview a valid target and icon before applying.")
        };
    }

    private static AppCommandDescriptor Navigate(
        string id,
        string label,
        string routeId,
        string detail)
    {
        return new AppCommandDescriptor(
            id,
            label,
            AppCommandKind.Navigate,
            IsPrimary: true,
            IsEnabled: true,
            routeId,
            LocationKind: null,
            detail,
            IconReplacerError.None);
    }

    private static AppCommandDescriptor Workflow(string id, string label, string detail)
    {
        return new AppCommandDescriptor(
            id,
            label,
            AppCommandKind.Workflow,
            IsPrimary: true,
            IsEnabled: true,
            TargetRouteId: null,
            LocationKind: null,
            detail,
            IconReplacerError.None);
    }

    private static AppCommandDescriptor DisabledWorkflow(string id, string label, string detail)
    {
        return Disabled(id, label, AppCommandKind.Workflow, detail);
    }

    private static AppCommandDescriptor OpenLocation(
        string id,
        string label,
        AppLocationKind locationKind,
        string detail)
    {
        return new AppCommandDescriptor(
            id,
            label,
            AppCommandKind.OpenLocation,
            IsPrimary: true,
            IsEnabled: true,
            TargetRouteId: null,
            locationKind,
            detail,
            IconReplacerError.None);
    }

    private static AppCommandDescriptor DisabledOpenLocation(string id, string label, string detail)
    {
        return Disabled(id, label, AppCommandKind.OpenLocation, detail);
    }

    private static AppCommandDescriptor Refresh(string id, string label, string detail)
    {
        return new AppCommandDescriptor(
            id,
            label,
            AppCommandKind.Refresh,
            IsPrimary: true,
            IsEnabled: true,
            TargetRouteId: null,
            LocationKind: null,
            detail,
            IconReplacerError.None);
    }

    private static AppCommandDescriptor ManualProof(string id, string label, string detail)
    {
        return new AppCommandDescriptor(
            id,
            label,
            AppCommandKind.ManualProof,
            IsPrimary: true,
            IsEnabled: false,
            TargetRouteId: null,
            LocationKind: null,
            detail,
            new IconReplacerError(
                ErrorCode.InvalidArgument,
                "Manual proof is captured outside the app model.",
                detail));
    }

    private static AppCommandDescriptor Disabled(
        string id,
        string label,
        AppCommandKind kind,
        string detail)
    {
        return new AppCommandDescriptor(
            id,
            label,
            kind,
            IsPrimary: true,
            IsEnabled: false,
            TargetRouteId: null,
            LocationKind: null,
            detail,
            new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The command is not available in the current state.",
                detail));
    }
}
