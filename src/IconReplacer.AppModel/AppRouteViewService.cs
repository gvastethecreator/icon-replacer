using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppRouteViewService
{
    private readonly AppWindowService _windowService;
    private readonly AppCommandService _commandService;
    private readonly AppHomeService _homeService;
    private readonly IconBrowserService _browserService;
    private readonly IconDetailsService _detailsService;
    private readonly IconImportPickerRequestService _importPickerRequestService;
    private readonly IconCollectionService _collectionService;
    private readonly AppChangeIconWorkflowService _changeIconWorkflowService;
    private readonly RestoreHistoryService _historyService;
    private readonly AppDiagnosticsService _diagnosticsService;
    private readonly ShellIntegrationPlanService _shellPlanService;
    private readonly ShellExtensionBridgeService _shellBridgeService;
    private readonly PackagingPlanService _packagingPlanService;
    private readonly AccessibilityPlanService _accessibilityPlanService;
    private readonly AppRestoreWorkflowService _restoreWorkflowService;

    public AppRouteViewService(
        AppWindowService? windowService = null,
        AppCommandService? commandService = null,
        AppHomeService? homeService = null,
        IconBrowserService? browserService = null,
        IconDetailsService? detailsService = null,
        IconImportPickerRequestService? importPickerRequestService = null,
        IconCollectionService? collectionService = null,
        AppChangeIconWorkflowService? changeIconWorkflowService = null,
        RestoreHistoryService? historyService = null,
        AppDiagnosticsService? diagnosticsService = null,
        ShellIntegrationPlanService? shellPlanService = null,
        ShellExtensionBridgeService? shellBridgeService = null,
        PackagingPlanService? packagingPlanService = null,
        AccessibilityPlanService? accessibilityPlanService = null,
        AppRestoreWorkflowService? restoreWorkflowService = null)
    {
        _windowService = windowService ?? new AppWindowService();
        _commandService = commandService ?? new AppCommandService();
        _homeService = homeService ?? new AppHomeService();
        _browserService = browserService ?? new IconBrowserService();
        _detailsService = detailsService ?? new IconDetailsService();
        _importPickerRequestService = importPickerRequestService ?? new IconImportPickerRequestService();
        _collectionService = collectionService ?? new IconCollectionService();
        _changeIconWorkflowService = changeIconWorkflowService ?? new AppChangeIconWorkflowService();
        _historyService = historyService ?? new RestoreHistoryService();
        _diagnosticsService = diagnosticsService ?? new AppDiagnosticsService();
        _shellPlanService = shellPlanService ?? new ShellIntegrationPlanService();
        _shellBridgeService = shellBridgeService ?? new ShellExtensionBridgeService();
        _packagingPlanService = packagingPlanService ?? new PackagingPlanService();
        _accessibilityPlanService = accessibilityPlanService ?? new AccessibilityPlanService();
        _restoreWorkflowService = restoreWorkflowService ?? new AppRestoreWorkflowService();
    }

    public OperationResult<AppRouteViewSnapshot> GetView(
        IReadOnlyList<string> activationArguments,
        IconLibraryPaths paths,
        string? routeId = null,
        WinUiToolingSnapshot? winUiTooling = null,
        Guid? restoreRecordId = null,
        RestoreHistoryFilter restoreHistoryFilter = RestoreHistoryFilter.All,
        string? selectedIconPath = null,
        string? importCollectionName = null,
        IconBrowserOptions? browserOptions = null,
        string? shellTargetPath = null,
        PackagingPlanInputs? packagingInputs = null)
    {
        var window = _windowService.GetWindow(activationArguments, paths, winUiTooling);
        if (!window.Succeeded || window.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(window.Error);
        }

        var effectiveRouteId = string.IsNullOrWhiteSpace(routeId)
            ? window.Value.SelectedRouteId
            : routeId;
        var route = window.Value.Navigation.FindRoute(effectiveRouteId);
        if (route is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The app route is not registered.",
                effectiveRouteId));
        }

        if (route.RouteId == AppNavigationRouteIds.RestorePreview)
        {
            return RestoreWorkflowView(
                window.Value,
                route,
                paths,
                restoreRecordId,
                restoreHistoryFilter);
        }

        if (route.RouteId == AppNavigationRouteIds.ChangeIcon)
        {
            return ChangeIconWorkflowView(
                window.Value,
                route,
                activationArguments,
                paths,
                selectedIconPath);
        }

        if (route.RouteId == AppNavigationRouteIds.IconDetails)
        {
            return IconDetailsView(
                window.Value,
                route,
                paths,
                selectedIconPath);
        }

        var commands = _commandService.GetCommands(window.Value, route.RouteId);
        if (!commands.Succeeded || commands.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(commands.Error);
        }

        return route.RouteId switch
        {
            AppNavigationRouteIds.Home => HomeView(window.Value, route, commands.Value, paths, winUiTooling, packagingInputs),
            AppNavigationRouteIds.IconBrowser => BrowserView(window.Value, route, commands.Value, paths, browserOptions),
            AppNavigationRouteIds.ImportIcons => ImportIconsView(window.Value, route, commands.Value, paths, importCollectionName),
            AppNavigationRouteIds.Collections => CollectionsView(window.Value, route, commands.Value, paths),
            AppNavigationRouteIds.History => HistoryView(window.Value, route, commands.Value, paths),
            AppNavigationRouteIds.Diagnostics => DiagnosticsView(
                window.Value,
                route,
                commands.Value,
                paths,
                winUiTooling,
                packagingInputs?.NativeTooling),
            AppNavigationRouteIds.ShellPlan => ShellPlanView(window.Value, route, commands.Value),
            AppNavigationRouteIds.ShellBridge => ShellBridgeView(window.Value, route, commands.Value, paths, shellTargetPath),
            AppNavigationRouteIds.PackagePlan => PackagePlanView(window.Value, route, commands.Value, packagingInputs),
            AppNavigationRouteIds.AccessibilityPlan => AccessibilityPlanView(window.Value, route, commands.Value),
            _ => OperationResult<AppRouteViewSnapshot>.Success(PlaceholderView(window.Value, route, commands.Value))
        };
    }

    public OperationResult<AppRouteViewSnapshot> GetViewFromEnvironment(
        IReadOnlyList<string> activationArguments,
        string? routeId = null,
        WinUiToolingSnapshot? winUiTooling = null,
        Guid? restoreRecordId = null,
        RestoreHistoryFilter restoreHistoryFilter = RestoreHistoryFilter.All,
        string? selectedIconPath = null,
        string? importCollectionName = null,
        IconBrowserOptions? browserOptions = null,
        string? shellTargetPath = null,
        PackagingPlanInputs? packagingInputs = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(paths.Error);
        }

        return GetView(
            activationArguments,
            paths.Value,
            routeId,
            winUiTooling,
            restoreRecordId,
            restoreHistoryFilter,
            selectedIconPath,
            importCollectionName,
            browserOptions,
            shellTargetPath,
            packagingInputs);
    }

    private OperationResult<AppRouteViewSnapshot> HomeView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        AppCommandStateSnapshot commands,
        IconLibraryPaths paths,
        WinUiToolingSnapshot? winUiTooling,
        PackagingPlanInputs? packagingInputs)
    {
        var effectivePackagingInputs = packagingInputs ?? (winUiTooling is null
            ? null
            : PackagingPlanInputs.FromTooling(winUiTooling));
        var home = _homeService.GetSnapshot(paths, packagingInputs: effectivePackagingInputs);
        if (!home.Succeeded || home.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(home.Error);
        }

        return Ready(
            window,
            route,
            commands,
            AppRouteContentKind.Home,
            $"Home shows {home.Value.Dashboard.IconCount} icons, {home.Value.Dashboard.CategoryCount} categories, and {home.Value.History.TotalCount} history records.",
            home: home.Value);
    }

    private OperationResult<AppRouteViewSnapshot> BrowserView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        AppCommandStateSnapshot commands,
        IconLibraryPaths paths,
        IconBrowserOptions? options)
    {
        var browser = _browserService.Browse(paths, options);
        if (!browser.Succeeded || browser.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(browser.Error);
        }

        return Ready(
            window,
            route,
            commands,
            AppRouteContentKind.IconBrowser,
            $"Browser shows {browser.Value.VisibleIconCount}/{browser.Value.MatchedIconCount} matched icons from {browser.Value.TotalIconCount} total.",
            browser: browser.Value);
    }

    private OperationResult<AppRouteViewSnapshot> IconDetailsView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        IconLibraryPaths paths,
        string? selectedIconPath)
    {
        if (string.IsNullOrWhiteSpace(selectedIconPath))
        {
            var commands = _commandService.GetCommands(window, route.RouteId);
            if (!commands.Succeeded || commands.Value is null)
            {
                return OperationResult<AppRouteViewSnapshot>.Failure(commands.Error);
            }

            return OperationResult<AppRouteViewSnapshot>.Success(PlaceholderView(
                window,
                route,
                commands.Value,
                new IconReplacerError(
                    ErrorCode.InvalidArgument,
                    "Select an icon before opening details.",
                    route.RouteId)));
        }

        var details = _detailsService.GetDetails(selectedIconPath, paths);
        if (!details.Succeeded || details.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(details.Error);
        }

        var commandsWithSelection = _commandService.GetCommands(window, route.RouteId, iconDetails: details.Value);
        if (!commandsWithSelection.Succeeded || commandsWithSelection.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(commandsWithSelection.Error);
        }

        return Ready(
            window,
            route,
            commandsWithSelection.Value,
            AppRouteContentKind.IconDetails,
            $"Icon details show {details.Value.DisplayName} with {details.Value.ImageCount} image entries and recommended {details.Value.RecommendedImage.Width}x{details.Value.RecommendedImage.Height}.",
            iconDetails: details.Value);
    }

    private OperationResult<AppRouteViewSnapshot> ImportIconsView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        AppCommandStateSnapshot commands,
        IconLibraryPaths paths,
        string? collectionName)
    {
        var request = _importPickerRequestService.CreateRequest(paths, collectionName);
        if (!request.Succeeded || request.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(request.Error);
        }

        return Ready(
            window,
            route,
            commands,
            AppRouteContentKind.ImportIcons,
            $"Import icons targets {request.Value.DestinationCollectionName} and allows multiple {string.Join(", ", request.Value.FileExtensions)} files.",
            importPickerRequest: request.Value);
    }

    private OperationResult<AppRouteViewSnapshot> CollectionsView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        AppCommandStateSnapshot commands,
        IconLibraryPaths paths)
    {
        var collections = _collectionService.ListCollections(paths);
        if (!collections.Succeeded || collections.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(collections.Error);
        }

        return Ready(
            window,
            route,
            commands,
            AppRouteContentKind.Collections,
            $"Collections shows {collections.Value.Count} one-level folders with {collections.Value.Sum(collection => collection.IconCount)} icons.",
            collections: collections.Value);
    }

    private OperationResult<AppRouteViewSnapshot> HistoryView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        AppCommandStateSnapshot commands,
        IconLibraryPaths paths)
    {
        var filter = route.DefaultHistoryFilter ?? RestoreHistoryFilter.All;
        var history = _historyService.GetHistory(paths, filter);
        if (!history.Succeeded || history.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(history.Error);
        }

        return Ready(
            window,
            route,
            commands,
            AppRouteContentKind.History,
            $"History shows {history.Value.Records.Count} {history.Value.Filter} records from {history.Value.TotalCount} total.",
            history: history.Value);
    }

    private OperationResult<AppRouteViewSnapshot> ChangeIconWorkflowView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        IReadOnlyList<string> activationArguments,
        IconLibraryPaths paths,
        string? selectedIconPath)
    {
        var workflow = _changeIconWorkflowService.GetWorkflow(activationArguments, paths, selectedIconPath);
        if (!workflow.Succeeded || workflow.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Success(PlaceholderView(
                window,
                route,
                ChangeIconUnavailableCommands(workflow.Error),
                workflow.Error));
        }

        var commands = _commandService.GetCommands(window, route.RouteId, changeIconWorkflow: workflow.Value);
        if (!commands.Succeeded || commands.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(commands.Error);
        }

        return Ready(
            window,
            route,
            commands.Value,
            AppRouteContentKind.ChangeIconWorkflow,
            DescribeChangeIconWorkflow(workflow.Value),
            changeIconWorkflow: workflow.Value);
    }

    private static string DescribeChangeIconWorkflow(AppChangeIconWorkflowSnapshot workflow)
    {
        return workflow.Step switch
        {
            AppChangeIconWorkflowStep.NeedIcon =>
                $"Change Icon workflow is waiting for an icon selection for {workflow.LaunchRequest?.Selection.ResolvedPath}.",
            AppChangeIconWorkflowStep.ReadyToApply =>
                $"Change Icon workflow is ready to apply {workflow.Preview?.Preview.IconDetails?.DisplayName ?? "the selected icon"} to {workflow.LaunchRequest?.Selection.Target?.FullPath}.",
            AppChangeIconWorkflowStep.Blocked =>
                $"Change Icon workflow is blocked: {workflow.Error.Message}",
            _ => "Change Icon workflow state is unavailable."
        };
    }

    private OperationResult<AppRouteViewSnapshot> DiagnosticsView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        AppCommandStateSnapshot commands,
        IconLibraryPaths paths,
        WinUiToolingSnapshot? winUiTooling,
        NativeToolingSnapshot? nativeTooling)
    {
        var diagnostics = _diagnosticsService.GetDiagnostics(paths, winUiTooling, nativeTooling);
        if (!diagnostics.Succeeded || diagnostics.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(diagnostics.Error);
        }

        return Ready(
            window,
            route,
            commands,
            AppRouteContentKind.Diagnostics,
            $"Diagnostics has {diagnostics.Value.BlockingCount} blockers and {diagnostics.Value.WarningCount} warnings.",
            diagnostics: diagnostics.Value);
    }

    private OperationResult<AppRouteViewSnapshot> RestoreWorkflowView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        IconLibraryPaths paths,
        Guid? restoreRecordId,
        RestoreHistoryFilter restoreHistoryFilter)
    {
        var workflow = _restoreWorkflowService.GetWorkflow(paths, restoreRecordId, restoreHistoryFilter);
        if (!workflow.Succeeded || workflow.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(workflow.Error);
        }

        var commands = _commandService.GetCommands(window, route.RouteId, workflow.Value);
        if (!commands.Succeeded || commands.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(commands.Error);
        }

        return Ready(
            window,
            route,
            commands.Value,
            AppRouteContentKind.RestoreWorkflow,
            DescribeRestoreWorkflow(workflow.Value),
            restoreWorkflow: workflow.Value);
    }

    private static string DescribeRestoreWorkflow(AppRestoreWorkflowSnapshot workflow)
    {
        return workflow.Step switch
        {
            AppRestoreWorkflowStep.NeedRecord =>
                $"Restore workflow is waiting for a record selection with {workflow.History.Records.Count} visible history records.",
            AppRestoreWorkflowStep.ReadyToRestore =>
                $"Restore workflow is ready for record {workflow.SelectedRecordId}.",
            AppRestoreWorkflowStep.Blocked =>
                $"Restore workflow is blocked for record {workflow.SelectedRecordId}: {workflow.Error.Message}",
            _ => "Restore workflow state is unavailable."
        };
    }

    private OperationResult<AppRouteViewSnapshot> ShellPlanView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        AppCommandStateSnapshot commands)
    {
        var plan = _shellPlanService.GetPlan(
            window.Diagnostics.Setup.ShellIntegration,
            window.Diagnostics.WinUiTooling);

        return Ready(
            window,
            route,
            commands,
            AppRouteContentKind.ShellPlan,
            $"Shell plan selects {plan.SelectedModeName} with {plan.BlockingCount} blockers and {plan.WarningCount} warnings.",
            shellPlan: plan);
    }

    private OperationResult<AppRouteViewSnapshot> ShellBridgeView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        AppCommandStateSnapshot commands,
        IconLibraryPaths paths,
        string? targetPath)
    {
        var bridge = _shellBridgeService.BuildBridge(paths, targetPath);
        if (!bridge.Succeeded || bridge.Value is null)
        {
            return OperationResult<AppRouteViewSnapshot>.Failure(bridge.Error);
        }

        var targetDetail = bridge.Value.Selection.Target is null
            ? "without a target selection"
            : $"for {bridge.Value.Selection.Target.Kind} {bridge.Value.Selection.Target.FullPath}";

        return Ready(
            window,
            route,
            commands,
            AppRouteContentKind.ShellBridge,
            $"Shell bridge exposes {bridge.Value.InvocableCommandCount}/{bridge.Value.CommandCount} invocable commands {targetDetail}.",
            shellBridge: bridge.Value);
    }

    private OperationResult<AppRouteViewSnapshot> PackagePlanView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        AppCommandStateSnapshot commands,
        PackagingPlanInputs? packagingInputs)
    {
        var plan = _packagingPlanService.GetPlan(
            packagingInputs ?? PackagingPlanInputs.FromTooling(window.Diagnostics.WinUiTooling));

        return Ready(
            window,
            route,
            commands,
            AppRouteContentKind.PackagePlan,
            $"Package plan has {plan.BlockingCount} blockers and {plan.WarningCount} warnings.",
            packagePlan: plan);
    }

    private OperationResult<AppRouteViewSnapshot> AccessibilityPlanView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        AppCommandStateSnapshot commands)
    {
        var plan = _accessibilityPlanService.GetPlan();

        return Ready(
            window,
            route,
            commands,
            AppRouteContentKind.AccessibilityPlan,
            $"Accessibility plan has {plan.RequiredCount} requirements and {plan.ManualProofCount} manual proof items.",
            accessibilityPlan: plan);
    }

    private static AppRouteViewSnapshot PlaceholderView(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        AppCommandStateSnapshot commands,
        IconReplacerError? error = null)
    {
        var effectiveError = error ?? new IconReplacerError(
            ErrorCode.InvalidArgument,
            "The route needs additional user input before content is ready.",
            route.RouteId);

        return new AppRouteViewSnapshot(
            window,
            route,
            commands,
            AppRouteContentKind.WorkflowPlaceholder,
            IsContentReady: false,
            $"Route {route.RouteId} is a workflow surface that needs selection or user input before content is ready.",
            Home: null,
            Browser: null,
            IconDetails: null,
            ImportPickerRequest: null,
            Collections: null,
            ChangeIconWorkflow: null,
            History: null,
            RestoreWorkflow: null,
            Diagnostics: null,
            ShellPlan: null,
            ShellBridge: null,
            PackagePlan: null,
            AccessibilityPlan: null,
            effectiveError,
            DateTimeOffset.UtcNow);
    }

    private static AppCommandStateSnapshot ChangeIconUnavailableCommands(IconReplacerError error)
    {
        static AppCommandDescriptor Disabled(string id, string label, string detail)
        {
            return new AppCommandDescriptor(
                id,
                label,
                AppCommandKind.Workflow,
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

        return new AppCommandStateSnapshot(
            AppNavigationRouteIds.ChangeIcon,
            "Change Icon",
            [
                Disabled("choose-icon", "Choose icon", error.Message),
                Disabled("preview-change", "Preview change", "Choose an icon before previewing the change."),
                Disabled("apply-change", "Apply change", "Preview a valid target and icon before applying.")
            ],
            DateTimeOffset.UtcNow);
    }

    private static OperationResult<AppRouteViewSnapshot> Ready(
        AppWindowSnapshot window,
        AppNavigationRoute route,
        AppCommandStateSnapshot commands,
        AppRouteContentKind kind,
        string summary,
        AppHomeSnapshot? home = null,
        IconBrowserSnapshot? browser = null,
        IconDetailsSnapshot? iconDetails = null,
        IconImportPickerRequestSnapshot? importPickerRequest = null,
        IReadOnlyList<IconCollectionSummary>? collections = null,
        AppChangeIconWorkflowSnapshot? changeIconWorkflow = null,
        RestoreHistorySnapshot? history = null,
        AppRestoreWorkflowSnapshot? restoreWorkflow = null,
        AppDiagnosticsSnapshot? diagnostics = null,
        ShellIntegrationPlanSnapshot? shellPlan = null,
        ShellExtensionBridgeSnapshot? shellBridge = null,
        PackagingPlanSnapshot? packagePlan = null,
        AccessibilityPlanSnapshot? accessibilityPlan = null)
    {
        return OperationResult<AppRouteViewSnapshot>.Success(new AppRouteViewSnapshot(
            window,
            route,
            commands,
            kind,
            IsContentReady: true,
            summary,
            home,
            browser,
            iconDetails,
            importPickerRequest,
            collections,
            changeIconWorkflow,
            history,
            restoreWorkflow,
            diagnostics,
            shellPlan,
            shellBridge,
            packagePlan,
            accessibilityPlan,
            IconReplacerError.None,
            DateTimeOffset.UtcNow));
    }
}
