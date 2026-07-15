using System.Diagnostics;
using System.Text.Json;
using IconReplacer.AppModel;
using IconReplacer.Core;

return Run(args);

static int Run(string[] args)
{
    if (args.Length == 0 || IsHelp(args[0]))
    {
        WriteUsage();
        return 0;
    }

    return args[0].ToLowerInvariant() switch
    {
        "doctor" => RunDoctor(),
        "diagnostics" or "diag" => RunDiagnostics(),
        "shell-plan" or "integration-plan" => RunShellPlan(),
        "shell-manifest" or "manifest-plan" => RunShellManifest(),
        "shell-bridge" or "native-shell-bridge" => RunShellBridge(args),
        "package-plan" or "packaging-plan" => RunPackagePlan(),
        "release-readiness" or "release-evidence" => RunReleaseReadiness(args),
        "accessibility-plan" or "a11y-plan" => RunAccessibilityPlan(),
        "navigation-plan" or "nav-plan" => RunNavigationPlan(),
        "app-window" or "window" => RunAppWindow(args),
        "app-commands" or "commands" => RunAppCommands(args),
        "app-command-request" or "command-request" => RunAppCommandRequest(args),
        "app-view" or "view" => RunAppView(args),
        "change-icon-workflow" or "change-workflow" => RunChangeIconWorkflow(args),
        "restore-workflow" or "restore-flow" => RunRestoreWorkflow(args),
        "status" or "setup" => RunStatus(),
        "action-request" or "action" => RunActionRequest(args),
        "activate" => RunActivate(args),
        "activate-preview" => RunActivatePreview(args),
        "activate-apply" => RunActivateApply(args),
        "activate-menu-apply" or "activate-apply-menu" => RunActivateMenuApply(args),
        "home" or "app" => RunHome(args),
        "paths" or "locations" => RunPaths(),
        "open-request" => RunOpenRequest(args),
        "target" or "selection" => RunTarget(args),
        "import-picker-request" or "import-picker" => RunImportPickerRequest(args),
        "picker-request" or "picker" => RunPickerRequest(args),
        "launch-request" or "launch" => RunLaunchRequest(args),
        "catalog" => RunCatalog(),
        "catalog-warnings" or "warnings" => RunCatalogWarnings(),
        "collections" or "collection-list" => RunCollections(),
        "collection-create" => RunCollectionCreate(args),
        "collection-import" => RunCollectionImport(args),
        "browse" or "icons" => RunBrowse(args),
        "details" or "icon-details" => RunDetails(args),
        "menu" => RunMenu(),
        "menu-commands" or "shell-menu" => RunMenuCommands(),
        "menu-invoke-preview" or "shell-invoke-preview" => RunMenuInvokePreview(args),
        "menu-apply" => RunMenuApply(args),
        "recent" or "changes" => RunRecent(args),
        "history" or "records" => RunHistory(args),
        "restore-preview" or "preview-restore" => RunRestorePreview(args),
        "import" => RunImport(args),
        "batch-import" or "import-many" => RunBatchImport(args),
        "preview-change" or "preview" => RunPreviewChange(args),
        "change" => RunChange(args),
        "apply" => RunApply(args),
        "apply-folder" => RunApplyFolder(args),
        "apply-shortcut" => RunApplyShortcut(args),
        "restore" => RunRestore(args),
        _ => UnknownCommand(args[0])
    };
}

static bool IsHelp(string value)
{
    return value is "-h" or "--help" or "help" or "/?";
}

static int RunDoctor()
{
    var paths = IconLibraryPaths.FromEnvironment();
    if (!paths.Succeeded || paths.Value is null)
    {
        Console.Error.WriteLine(paths.Error.Message);
        if (!string.IsNullOrWhiteSpace(paths.Error.Detail))
        {
            Console.Error.WriteLine(paths.Error.Detail);
        }

        return 1;
    }

    Console.WriteLine("Icon Replacer doctor");
    Console.WriteLine($"Icon library: {paths.Value.LibraryRoot}");
    Console.WriteLine($"Imported icons: {paths.Value.ImportedRoot}");
    Console.WriteLine($"Restore state: {paths.Value.RestoreStateFile}");
    Console.WriteLine("Core/CLI harness: ready");
    Console.WriteLine("Apply commands: available");
    Console.WriteLine("Restore command: available");

    var snapshot = new DashboardService().GetSnapshot(paths.Value);
    if (snapshot.Succeeded && snapshot.Value is not null)
    {
        Console.WriteLine($"Catalog categories: {snapshot.Value.CategoryCount}");
        Console.WriteLine($"Catalog icons: {snapshot.Value.IconCount}");
        Console.WriteLine($"Catalog warnings: {snapshot.Value.CatalogWarningCount}");
        Console.WriteLine($"Restore records: {snapshot.Value.RestoreRecordCount}");
        Console.WriteLine($"Restorable records: {snapshot.Value.RestorableRecordCount}");
        Console.WriteLine($"Records with missing targets: {snapshot.Value.MissingTargetRecordCount}");
        Console.WriteLine($"Records with missing applied icons: {snapshot.Value.MissingAppliedIconRecordCount}");
    }
    else
    {
        Console.WriteLine($"Dashboard check: {snapshot.Error.Message}");
    }

    return 0;
}

static int RunDiagnostics()
{
    var winUiTooling = DetectWinUiTooling();
    var nativeTooling = DetectNativeTooling();
    var diagnostics = new AppDiagnosticsService().GetDiagnosticsFromEnvironment(winUiTooling, nativeTooling);
    if (!diagnostics.Succeeded || diagnostics.Value is null)
    {
        return WriteError(diagnostics.Error);
    }

    Console.WriteLine("Icon Replacer diagnostics");
    Console.WriteLine($"Blocking issues: {diagnostics.Value.BlockingCount}");
    Console.WriteLine($"Warnings: {diagnostics.Value.WarningCount}");
    Console.WriteLine($"Shell integration: {diagnostics.Value.Setup.ShellIntegration}");
    Console.WriteLine($"WinUI templates: {(diagnostics.Value.WinUiTooling.WinUiTemplatesAvailable ? "available" : "missing")}");
    Console.WriteLine($"winapp CLI: {(diagnostics.Value.WinUiTooling.WinAppAvailable ? "available" : "missing")}");
    Console.WriteLine($"Native build tools: {(diagnostics.Value.NativeTooling.CanBuildNativeExtension ? "available" : "missing")}");
    Console.WriteLine($"CMake: {(diagnostics.Value.NativeTooling.CMakeAvailable ? "available" : "missing")}");

    foreach (var check in diagnostics.Value.Checks)
    {
        Console.WriteLine($"- {check.Status}: {check.Title} ({check.Id})");
        Console.WriteLine($"  {check.Detail}");
    }

    return diagnostics.Value.HasBlockingIssues ? 2 : 0;
}

static int RunShellPlan()
{
    var plan = new ShellIntegrationPlanService().GetPlan(
        ShellIntegrationReadiness.NotConfigured,
        DetectWinUiTooling());

    Console.WriteLine("Icon Replacer shell integration plan");
    Console.WriteLine($"Decision final: {(plan.DecisionFinal ? "yes" : "no")}");
    Console.WriteLine($"Selected mode: {plan.SelectedModeName}");
    Console.WriteLine($"Fallback mode: {plan.FallbackModeName}");
    Console.WriteLine($"Readiness: {plan.Readiness}");
    Console.WriteLine($"Blocking issues: {plan.BlockingCount}");
    Console.WriteLine($"Warnings: {plan.WarningCount}");
    Console.WriteLine($"Rationale: {plan.Rationale}");

    foreach (var item in plan.Items)
    {
        var required = item.RequiredForV1 ? "required" : "optional";
        Console.WriteLine($"- {item.Status}: {item.Title} ({item.Id}, {required})");
        Console.WriteLine($"  {item.Detail}");
    }

    return plan.HasBlockingIssues ? 2 : 0;
}

static int RunShellManifest()
{
    var contract = new ShellManifestContractService().GetContract();

    Console.WriteLine("Icon Replacer shell manifest contract");
    Console.WriteLine($"Package name: {contract.PackageName}");
    Console.WriteLine($"Application id: {contract.ApplicationId}");
    Console.WriteLine($"COM category: {contract.ComServerCategory}");
    Console.WriteLine($"Context menu category: {contract.FileExplorerContextMenusCategory}");
    Console.WriteLine($"CLSID: {contract.ExplorerCommandClsid}");
    Console.WriteLine($"COM display name: {contract.SurrogateServerDisplayName}");
    Console.WriteLine($"Shell extension DLL: {contract.ShellExtensionDllPath}");
    Console.WriteLine($"Threading model: {contract.ThreadingModel}");
    Console.WriteLine($"Requires package identity: {(contract.RequiresPackageIdentity ? "yes" : "no")}");
    Console.WriteLine($"Restart Explorer after install: {(contract.RequiresExplorerRestartAfterInstall ? "yes" : "no")}");
    Console.WriteLine("Required interfaces:");
    foreach (var requiredInterface in contract.RequiredInterfaces)
    {
        Console.WriteLine($"- {requiredInterface}");
    }

    Console.WriteLine("Context menu targets:");
    foreach (var target in contract.Targets)
    {
        Console.WriteLine($"- {target.ItemType} | {target.VerbId} | {target.Clsid}");
    }

    Console.WriteLine("Manifest fragment:");
    Console.WriteLine(contract.ManifestFragment);
    return 0;
}

static int RunShellBridge(string[] args)
{
    if (args.Length > 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli shell-bridge [target]");
        return 64;
    }

    var targetPath = args.Length == 2 ? args[1] : null;
    var bridge = new ShellExtensionBridgeService().BuildBridgeFromEnvironment(targetPath);
    if (!bridge.Succeeded || bridge.Value is null)
    {
        return WriteError(bridge.Error);
    }

    Console.WriteLine("Icon Replacer shell extension bridge");
    Console.WriteLine($"Protocol: {bridge.Value.ProtocolVersion}");
    Console.WriteLine($"CLSID: {bridge.Value.ExplorerCommandClsid}");
    Console.WriteLine($"Shell extension DLL: {bridge.Value.ShellExtensionDllPath}");
    Console.WriteLine($"Threading model: {bridge.Value.ThreadingModel}");
    Console.WriteLine($"Required interfaces: {string.Join(", ", bridge.Value.RequiredInterfaces)}");
    Console.WriteLine($"Targets: {string.Join(", ", bridge.Value.Targets.Select(target => target.ItemType))}");
    Console.WriteLine($"Menu state: {bridge.Value.MenuState}");
    Console.WriteLine($"Status: {bridge.Value.StatusMessage}");
    Console.WriteLine($"Commands: {bridge.Value.CommandCount}");
    Console.WriteLine($"Invocable commands: {bridge.Value.InvocableCommandCount}");
    Console.WriteLine($"Icon commands: {bridge.Value.VisibleIconCommandCount}/{bridge.Value.TotalIconCount} visible");
    Console.WriteLine($"Omitted icons: {bridge.Value.OmittedIconCount}");
    Console.WriteLine($"Target status: {bridge.Value.Selection.Status}");
    Console.WriteLine($"Target path: {bridge.Value.Selection.ResolvedPath ?? targetPath ?? "(none)"}");

    if (bridge.Value.Selection.Target is not null)
    {
        Console.WriteLine($"Target kind: {bridge.Value.Selection.Target.Kind}");
    }
    else if (bridge.Value.Selection.Error.Code != ErrorCode.None)
    {
        Console.WriteLine($"Target reason: {bridge.Value.Selection.Error.Message}");
    }

    Console.WriteLine("Safety rules:");
    foreach (var rule in bridge.Value.SafetyRules)
    {
        Console.WriteLine($"- {rule}");
    }

    Console.WriteLine("Commands:");
    foreach (var command in bridge.Value.Commands)
    {
        Console.WriteLine(FormatShellBridgeCommand(command));
    }

    if (!bridge.Value.CanShowContextMenu)
    {
        return ExitCodeForError(bridge.Value.Error);
    }

    return targetPath is not null && !bridge.Value.Selection.CanShowChangeIcon
        ? ExitCodeForError(bridge.Value.Selection.Error)
        : 0;
}

static int RunPackagePlan()
{
    var plan = new PackagingPlanService().GetPlan(DetectPackagingInputs());

    Console.WriteLine("Icon Replacer package plan");
    Console.WriteLine($"Install mode: {plan.InstallMode}");
    Console.WriteLine($"Package name: {plan.PackageName}");
    Console.WriteLine($"Requires package identity: {(plan.RequiresPackageIdentity ? "yes" : "no")}");
    Console.WriteLine($"Requires dev signing: {(plan.RequiresDevSigning ? "yes" : "no")}");
    Console.WriteLine($"Remove shell integration on uninstall: {(plan.RemovesShellIntegrationOnUninstall ? "yes" : "no")}");
    Console.WriteLine($"Preserve Icon Library on uninstall: {(plan.PreservesIconLibraryOnUninstall ? "yes" : "no")}");
    Console.WriteLine($"Preserve restore history by default: {(plan.PreservesRestoreHistoryByDefault ? "yes" : "no")}");
    Console.WriteLine($"Manifest CLSID: {plan.ManifestContract.ExplorerCommandClsid}");
    Console.WriteLine($"Manifest DLL: {plan.ManifestContract.ShellExtensionDllPath}");
    Console.WriteLine($"Blocking issues: {plan.BlockingCount}");
    Console.WriteLine($"Warnings: {plan.WarningCount}");

    foreach (var item in plan.Items)
    {
        var required = item.RequiredForV1 ? "required" : "optional";
        Console.WriteLine($"- {item.Status}: {item.Title} ({item.Id}, {required})");
        Console.WriteLine($"  {item.Detail}");
    }

    return plan.HasBlockingIssues ? 2 : 0;
}

static int RunReleaseReadiness(string[] args)
{
    var parsed = ParseReleaseReadinessInputs(args);
    if (!parsed.Succeeded || parsed.Inputs is null)
    {
        return parsed.ExitCode;
    }

    var readiness = new ReleaseReadinessService().GetReadinessFromEnvironment(parsed.Inputs);
    if (!readiness.Succeeded || readiness.Value is null)
    {
        return WriteError(readiness.Error);
    }

    Console.WriteLine("Icon Replacer release readiness");
    Console.WriteLine($"Integration path: {readiness.Value.IntegrationPath}");
    Console.WriteLine($"Ready for release: {(readiness.Value.IsReadyForRelease ? "yes" : "no")}");
    Console.WriteLine($"Required items: {readiness.Value.RequiredCount}");
    Console.WriteLine($"Passed: {readiness.Value.PassCount}");
    Console.WriteLine($"Blocking issues: {readiness.Value.BlockingCount}");
    Console.WriteLine($"Warnings: {readiness.Value.WarningCount}");
    Console.WriteLine($"Package blockers: {readiness.Value.PackagingPlan.BlockingCount}");
    Console.WriteLine($"Package warnings: {readiness.Value.PackagingPlan.WarningCount}");
    Console.WriteLine($"Diagnostics blockers: {readiness.Value.Diagnostics.BlockingCount}");
    Console.WriteLine($"Diagnostics warnings: {readiness.Value.Diagnostics.WarningCount}");
    Console.WriteLine($"Accessibility manual proofs: {readiness.Value.AccessibilityPlan.ManualProofCount}");

    foreach (var item in readiness.Value.Items)
    {
        Console.WriteLine($"- {item.Status}: {item.Title} ({item.Id})");
        Console.WriteLine($"  {item.Detail}");
        Console.WriteLine($"  Evidence: {item.EvidenceCommand}");
    }

    return readiness.Value.IsReadyForRelease ? 0 : 2;
}

static int RunAccessibilityPlan()
{
    var plan = new AccessibilityPlanService().GetPlan();

    Console.WriteLine("Icon Replacer accessibility plan");
    Console.WriteLine($"Requirements: {plan.RequiredCount}");
    Console.WriteLine($"Manual proof required: {(plan.RequiresManualProof ? "yes" : "no")}");
    Console.WriteLine($"Manual proof items: {plan.ManualProofCount}");
    Console.WriteLine($"Surfaces: {plan.Surfaces.Count}");
    Console.WriteLine("Requirements:");
    foreach (var requirement in plan.Requirements)
    {
        var required = requirement.RequiredForV1 ? "required" : "optional";
        var proof = requirement.ManualProofRequired ? ", manual proof" : string.Empty;
        Console.WriteLine($"- {requirement.Category}: {requirement.Title} ({requirement.Id}, {required}{proof})");
        Console.WriteLine($"  {requirement.Detail}");
    }

    Console.WriteLine("Surfaces:");
    foreach (var surface in plan.Surfaces)
    {
        Console.WriteLine($"- {surface.Title} ({surface.RouteId})");
        Console.WriteLine($"  {surface.Purpose}");
        Console.WriteLine($"  Commands: {string.Join(", ", surface.PrimaryCommands)}");
        Console.WriteLine($"  Keyboard reachable: {(surface.RequiresKeyboardReachability ? "yes" : "no")}");
        Console.WriteLine($"  Persistent errors: {(surface.RequiresPersistentErrors ? "yes" : "no")}");
    }

    return 0;
}

static int RunNavigationPlan()
{
    var plan = new AppNavigationService().GetPlan();

    Console.WriteLine("Icon Replacer navigation plan");
    Console.WriteLine($"Default route: {plan.DefaultRouteId}");
    Console.WriteLine($"Fallback route: {plan.FallbackRouteId}");
    Console.WriteLine($"Routes: {plan.RouteCount}");
    Console.WriteLine($"Top-level routes: {plan.TopLevelRouteCount}");
    Console.WriteLine("Routes:");
    foreach (var route in plan.Routes)
    {
        var level = route.IsTopLevel ? "top-level" : "workflow";
        Console.WriteLine($"- {route.Title} ({route.RouteId}, {route.Section}, {level})");
        Console.WriteLine($"  {route.Purpose}");
        Console.WriteLine($"  Commands: {string.Join(", ", route.PrimaryCommands)}");
        Console.WriteLine($"  Requires selection: {(route.RequiresSelection ? "yes" : "no")}");
        if (route.DefaultHistoryFilter is not null)
        {
            Console.WriteLine($"  Default history filter: {route.DefaultHistoryFilter}");
        }
    }

    Console.WriteLine("Action targets:");
    foreach (var target in plan.ActionTargets.OrderBy(target => target.Key.ToString()))
    {
        Console.WriteLine($"- {target.Key}: {target.Value}");
    }

    return 0;
}

static int RunAppWindow(string[] args)
{
    var window = new AppWindowService().GetWindowFromEnvironment(
        args.Skip(1).ToArray(),
        DetectWinUiTooling());
    if (!window.Succeeded || window.Value is null)
    {
        return WriteError(window.Error);
    }

    Console.WriteLine("Icon Replacer app window");
    Console.WriteLine($"Selected route: {window.Value.SelectedRouteId}");
    Console.WriteLine($"Window title: {window.Value.WindowTitle}");
    Console.WriteLine($"Can use selected route: {(window.Value.CanUseSelectedRoute ? "yes" : "no")}");
    Console.WriteLine($"Default route: {window.Value.Navigation.DefaultRouteId}");
    Console.WriteLine($"Fallback route: {window.Value.Navigation.FallbackRouteId}");
    Console.WriteLine($"Routes: {window.Value.Navigation.RouteCount}");
    Console.WriteLine($"Top-level routes: {window.Value.Navigation.TopLevelRouteCount}");
    Console.WriteLine($"Diagnostic blockers: {window.Value.BlockingCount}");
    Console.WriteLine($"Diagnostic warnings: {window.Value.WarningCount}");
    Console.WriteLine($"Shell integration: {window.Value.Diagnostics.Setup.ShellIntegration}");
    Console.WriteLine($"WinUI templates: {(window.Value.Diagnostics.WinUiTooling.WinUiTemplatesAvailable ? "available" : "missing")}");
    Console.WriteLine($"winapp CLI: {(window.Value.Diagnostics.WinUiTooling.WinAppAvailable ? "available" : "missing")}");

    if (window.Value.Activation is not null)
    {
        Console.WriteLine($"Activation kind: {window.Value.Activation.Kind}");
        Console.WriteLine($"Activation can continue: {(window.Value.Activation.CanContinue ? "yes" : "no")}");
    }
    else
    {
        Console.WriteLine("Activation kind: unavailable");
    }

    if (!window.Value.CanUseSelectedRoute)
    {
        Console.WriteLine($"Reason: {window.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(window.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {window.Value.Error.Detail}");
        }
    }

    return window.Value.CanUseSelectedRoute ? 0 : ExitCodeForError(window.Value.Error);
}

static int RunAppCommands(string[] args)
{
    var routeId = GetOptionalValue(args, "--route");
    var selectedIconPath = GetOptionalValue(args, "--icon");
    Guid? restoreRecordId = null;
    var restoreRecordIdText = GetOptionalValue(args, "--record");
    if (!string.IsNullOrWhiteSpace(restoreRecordIdText))
    {
        if (!Guid.TryParse(restoreRecordIdText, out var parsedRecordId))
        {
            Console.Error.WriteLine("Restore record id must be a GUID.");
            return 64;
        }

        restoreRecordId = parsedRecordId;
    }

    var restoreFilter = RestoreHistoryFilter.All;
    var restoreFilterText = GetOptionalValue(args, "--filter");
    if (!string.IsNullOrWhiteSpace(restoreFilterText) &&
        !TryParseHistoryFilter(restoreFilterText, out restoreFilter))
    {
        Console.Error.WriteLine("History filter must be one of: all, restorable, applied, restored, stale.");
        return 64;
    }

    var activationArgs = RemoveOptions(
        args.Skip(1).ToArray(),
        "--route",
        "--icon",
        "--collection",
        "--record",
        "--filter");
    var commands = new AppCommandService().GetCommandsFromEnvironment(
        activationArgs,
        routeId,
        DetectWinUiTooling(),
        restoreRecordId,
        restoreFilter,
        selectedIconPath);
    if (!commands.Succeeded || commands.Value is null)
    {
        return WriteError(commands.Error);
    }

    Console.WriteLine("Icon Replacer app commands");
    Console.WriteLine($"Route: {commands.Value.RouteTitle} ({commands.Value.RouteId})");
    Console.WriteLine($"Commands: {commands.Value.CommandCount}");
    Console.WriteLine($"Enabled: {commands.Value.EnabledCount}");
    Console.WriteLine($"Disabled: {commands.Value.DisabledCount}");

    foreach (var command in commands.Value.Commands)
    {
        var status = command.IsEnabled ? "enabled" : "disabled";
        var primary = command.IsPrimary ? "primary" : "secondary";
        Console.WriteLine($"- {status}: {command.Label} ({command.Id}, {command.Kind}, {primary})");
        Console.WriteLine($"  {command.Detail}");
        if (!string.IsNullOrWhiteSpace(command.TargetRouteId))
        {
            Console.WriteLine($"  Target route: {command.TargetRouteId}");
        }

        if (command.LocationKind is not null)
        {
            Console.WriteLine($"  Location: {command.LocationKind}");
        }

        if (!command.IsEnabled && command.Error.Code != ErrorCode.None)
        {
            Console.WriteLine($"  Reason: {command.Error.Message}");
        }
    }

    return 0;
}

static int RunAppCommandRequest(string[] args)
{
    if (args.Length < 2 || IsHelp(args[1]))
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli app-command-request <command-id> [--route <route-id>] [--record <record-id>] [--filter <filter>] [--icon <icon.ico>] [activation args]");
        return 64;
    }

    var commandId = args[1];
    var routeId = GetOptionalValue(args, "--route");
    var selectedIconPath = GetOptionalValue(args, "--icon");
    Guid? restoreRecordId = null;
    var restoreRecordIdText = GetOptionalValue(args, "--record");
    if (!string.IsNullOrWhiteSpace(restoreRecordIdText))
    {
        if (!Guid.TryParse(restoreRecordIdText, out var parsedRecordId))
        {
            Console.Error.WriteLine("Restore record id must be a GUID.");
            return 64;
        }

        restoreRecordId = parsedRecordId;
    }

    var restoreFilter = RestoreHistoryFilter.All;
    var restoreFilterText = GetOptionalValue(args, "--filter");
    if (!string.IsNullOrWhiteSpace(restoreFilterText) &&
        !TryParseHistoryFilter(restoreFilterText, out restoreFilter))
    {
        Console.Error.WriteLine("History filter must be one of: all, restorable, applied, restored, stale.");
        return 64;
    }

    var activationArgs = RemoveOptions(
        args.Skip(2).ToArray(),
        "--route",
        "--icon",
        "--collection",
        "--record",
        "--filter");
    var request = new AppCommandRequestService().CreateRequestFromEnvironment(
        activationArgs,
        commandId,
        routeId,
        DetectWinUiTooling(),
        restoreRecordId,
        restoreFilter,
        selectedIconPath);
    if (!request.Succeeded || request.Value is null)
    {
        return WriteError(request.Error);
    }

    Console.WriteLine("Icon Replacer app command request");
    Console.WriteLine($"Route: {request.Value.RouteId}");
    Console.WriteLine($"Command: {request.Value.CommandId}");
    Console.WriteLine($"Label: {request.Value.Command.Label}");
    Console.WriteLine($"Kind: {request.Value.Command.Kind}");
    Console.WriteLine($"Can execute: {(request.Value.CanExecute ? "yes" : "no")}");
    Console.WriteLine($"Summary: {request.Value.Summary}");

    if (!string.IsNullOrWhiteSpace(request.Value.NavigationTarget))
    {
        Console.WriteLine($"Navigation target: {request.Value.NavigationTarget}");
    }

    if (request.Value.LocationOpenRequest is not null)
    {
        Console.WriteLine($"Location: {request.Value.LocationOpenRequest.Location.Kind}");
        Console.WriteLine($"Location can open: {(request.Value.LocationOpenRequest.CanOpen ? "yes" : "no")}");
        Console.WriteLine($"Location target: {request.Value.LocationOpenRequest.TargetPath}");
        Console.WriteLine($"Location verb: {request.Value.LocationOpenRequest.ShellVerb}");
    }

    if (!string.IsNullOrWhiteSpace(request.Value.WorkflowId))
    {
        Console.WriteLine($"Workflow id: {request.Value.WorkflowId}");
    }

    Console.WriteLine($"Requires refresh: {(request.Value.RequiresRefresh ? "yes" : "no")}");

    if (!request.Value.CanExecute)
    {
        Console.WriteLine($"Reason: {request.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(request.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {request.Value.Error.Detail}");
        }
    }

    return request.Value.CanExecute ? 0 : ExitCodeForError(request.Value.Error);
}

static int RunAppView(string[] args)
{
    var routeId = GetOptionalValue(args, "--route");
    var selectedIconPath = GetOptionalValue(args, "--icon");
    var importCollectionName = GetOptionalValue(args, "--collection");
    var shellTargetPath = GetOptionalValue(args, "--shell-target");
    var browserSearchText = GetOptionalValue(args, "--search");
    var browserCategoryName = GetOptionalValue(args, "--category");
    int? browserMaxItems = null;
    var browserMaxItemsText = GetOptionalValue(args, "--max");
    if (!string.IsNullOrWhiteSpace(browserMaxItemsText))
    {
        if (!int.TryParse(browserMaxItemsText, out var parsedMaxItems) || parsedMaxItems < 0)
        {
            Console.Error.WriteLine("Browser max must be a non-negative integer.");
            return 64;
        }

        browserMaxItems = parsedMaxItems;
    }

    var browserOptions = string.IsNullOrWhiteSpace(browserSearchText) &&
        string.IsNullOrWhiteSpace(browserCategoryName) &&
        browserMaxItems is null
            ? null
            : new IconBrowserOptions(
                browserSearchText,
                browserCategoryName,
                browserMaxItems ?? IconBrowserOptions.Default.MaxItems);
    Guid? restoreRecordId = null;
    var restoreRecordIdText = GetOptionalValue(args, "--record");
    if (!string.IsNullOrWhiteSpace(restoreRecordIdText))
    {
        if (!Guid.TryParse(restoreRecordIdText, out var parsedRecordId))
        {
            Console.Error.WriteLine("Restore record id must be a GUID.");
            return 64;
        }

        restoreRecordId = parsedRecordId;
    }

    var restoreFilter = RestoreHistoryFilter.All;
    var restoreFilterText = GetOptionalValue(args, "--filter");
    if (!string.IsNullOrWhiteSpace(restoreFilterText) &&
        !TryParseHistoryFilter(restoreFilterText, out restoreFilter))
    {
        Console.Error.WriteLine("History filter must be one of: all, restorable, applied, restored, stale.");
        return 64;
    }

    var activationArgs = RemoveOptions(
        args.Skip(1).ToArray(),
        "--route",
        "--icon",
        "--collection",
        "--shell-target",
        "--record",
        "--filter",
        "--search",
        "--category",
        "--max");
    var winUiTooling = DetectWinUiTooling();
    var packagingInputs = DetectPackagingInputs(winUiTooling);
    var view = new AppRouteViewService().GetViewFromEnvironment(
        activationArgs,
        routeId,
        winUiTooling,
        restoreRecordId,
        restoreFilter,
        selectedIconPath,
        importCollectionName,
        browserOptions,
        shellTargetPath,
        packagingInputs);
    if (!view.Succeeded || view.Value is null)
    {
        return WriteError(view.Error);
    }

    Console.WriteLine("Icon Replacer app view");
    Console.WriteLine($"Route: {view.Value.Route.Title} ({view.Value.Route.RouteId})");
    Console.WriteLine($"Selected window route: {view.Value.Window.SelectedRouteId}");
    Console.WriteLine($"Content kind: {view.Value.ContentKind}");
    Console.WriteLine($"Content ready: {(view.Value.IsContentReady ? "yes" : "no")}");
    Console.WriteLine($"Summary: {view.Value.Summary}");
    Console.WriteLine($"Commands: {view.Value.Commands.CommandCount}");
    Console.WriteLine($"Enabled commands: {view.Value.Commands.EnabledCount}");
    Console.WriteLine($"Disabled commands: {view.Value.Commands.DisabledCount}");

    if (view.Value.Home is not null)
    {
        Console.WriteLine($"Home icons: {view.Value.Home.Dashboard.IconCount}");
        Console.WriteLine($"Home categories: {view.Value.Home.Dashboard.CategoryCount}");
        Console.WriteLine($"Home history records: {view.Value.Home.History.TotalCount}");
        Console.WriteLine($"Home setup actions: {view.Value.Home.Setup.Actions.Count}");
    }

    if (view.Value.Browser is not null)
    {
        Console.WriteLine($"Browser search: {view.Value.Browser.SearchText ?? "(none)"}");
        Console.WriteLine($"Browser category: {view.Value.Browser.CategoryName ?? "(all)"}");
        Console.WriteLine($"Browser visible icons: {view.Value.Browser.VisibleIconCount}");
        Console.WriteLine($"Browser matched icons: {view.Value.Browser.MatchedIconCount}");
        Console.WriteLine($"Browser total icons: {view.Value.Browser.TotalIconCount}");
        Console.WriteLine($"Browser omitted icons: {view.Value.Browser.OmittedIconCount}");
        Console.WriteLine($"Browser categories: {view.Value.Browser.Categories.Count}");
    }

    if (view.Value.IconDetails is not null)
    {
        Console.WriteLine($"Icon path: {view.Value.IconDetails.FullPath}");
        Console.WriteLine($"Icon display name: {view.Value.IconDetails.DisplayName}");
        Console.WriteLine($"Icon category: {view.Value.IconDetails.CategoryName}");
        Console.WriteLine($"Icon in library: {(view.Value.IconDetails.IsInIconLibrary ? "yes" : "no")}");
        Console.WriteLine($"Icon images: {view.Value.IconDetails.ImageCount}");
        Console.WriteLine($"Icon recommended image: {FormatImageDetail(view.Value.IconDetails.RecommendedImage)}");
    }

    if (view.Value.ImportPickerRequest is not null)
    {
        Console.WriteLine($"Import can open picker: {(view.Value.ImportPickerRequest.CanOpenPicker ? "yes" : "no")}");
        Console.WriteLine($"Import title: {view.Value.ImportPickerRequest.Title}");
        Console.WriteLine($"Import initial directory: {view.Value.ImportPickerRequest.InitialDirectory}");
        Console.WriteLine($"Import destination collection: {view.Value.ImportPickerRequest.DestinationCollectionName}");
        Console.WriteLine($"Import destination directory: {view.Value.ImportPickerRequest.DestinationDirectory}");
        Console.WriteLine($"Import allow multiple: {(view.Value.ImportPickerRequest.AllowMultiple ? "yes" : "no")}");
        Console.WriteLine($"Import file types: {view.Value.ImportPickerRequest.FileTypeLabel}");
    }

    if (view.Value.Collections is not null)
    {
        Console.WriteLine($"Collections: {view.Value.Collections.Count}");
        Console.WriteLine($"Collection icons: {view.Value.Collections.Sum(collection => collection.IconCount)}");
        Console.WriteLine($"Imported collection present: {(view.Value.Collections.Any(collection => collection.IsImportedCollection) ? "yes" : "no")}");
    }

    if (view.Value.ChangeIconWorkflow is not null)
    {
        Console.WriteLine($"Change workflow step: {view.Value.ChangeIconWorkflow.Step}");
        Console.WriteLine($"Change can open picker: {(view.Value.ChangeIconWorkflow.CanOpenPicker ? "yes" : "no")}");
        Console.WriteLine($"Change can preview: {(view.Value.ChangeIconWorkflow.CanPreview ? "yes" : "no")}");
        Console.WriteLine($"Change can apply: {(view.Value.ChangeIconWorkflow.CanApply ? "yes" : "no")}");
        Console.WriteLine($"Change target: {view.Value.ChangeIconWorkflow.LaunchRequest?.Selection.Target?.FullPath ?? "(none)"}");
        Console.WriteLine($"Selected icon: {view.Value.ChangeIconWorkflow.SelectedIconPath ?? "(none)"}");

        if (view.Value.ChangeIconWorkflow.Preview is not null)
        {
            Console.WriteLine($"Preview target: {view.Value.ChangeIconWorkflow.Preview.Preview.RequestedTargetPath}");
            Console.WriteLine($"Preview icon: {view.Value.ChangeIconWorkflow.Preview.Preview.RequestedIconPath}");
            Console.WriteLine($"Preview can apply: {(view.Value.ChangeIconWorkflow.Preview.CanApply ? "yes" : "no")}");
        }
    }

    if (view.Value.History is not null)
    {
        Console.WriteLine($"History filter: {view.Value.History.Filter}");
        Console.WriteLine($"History shown records: {view.Value.History.Records.Count}");
        Console.WriteLine($"History total records: {view.Value.History.TotalCount}");
    }

    if (view.Value.RestoreWorkflow is not null)
    {
        Console.WriteLine($"Restore workflow step: {view.Value.RestoreWorkflow.Step}");
        Console.WriteLine($"Restore can preview: {(view.Value.RestoreWorkflow.CanPreview ? "yes" : "no")}");
        Console.WriteLine($"Restore can restore: {(view.Value.RestoreWorkflow.CanRestore ? "yes" : "no")}");
        Console.WriteLine($"Restore selected record: {view.Value.RestoreWorkflow.SelectedRecordId?.ToString() ?? "(none)"}");
        Console.WriteLine($"Restore history filter: {view.Value.RestoreWorkflow.History.Filter}");
        Console.WriteLine($"Restore history shown records: {view.Value.RestoreWorkflow.History.Records.Count}");
        Console.WriteLine($"Restore history total records: {view.Value.RestoreWorkflow.History.TotalCount}");

        if (view.Value.RestoreWorkflow.Preview is not null)
        {
            Console.WriteLine($"Restore record status: {view.Value.RestoreWorkflow.Preview.Record.Status}");
            Console.WriteLine($"Restore target: {view.Value.RestoreWorkflow.Preview.Record.TargetPath}");
            Console.WriteLine($"Restore action: {view.Value.RestoreWorkflow.Preview.RestoreActionDetail}");
        }
    }

    if (view.Value.Diagnostics is not null)
    {
        Console.WriteLine($"Diagnostic blockers: {view.Value.Diagnostics.BlockingCount}");
        Console.WriteLine($"Diagnostic warnings: {view.Value.Diagnostics.WarningCount}");
    }

    if (view.Value.ShellPlan is not null)
    {
        Console.WriteLine($"Shell selected mode: {view.Value.ShellPlan.SelectedModeName}");
        Console.WriteLine($"Shell blockers: {view.Value.ShellPlan.BlockingCount}");
        Console.WriteLine($"Shell warnings: {view.Value.ShellPlan.WarningCount}");
    }

    if (view.Value.ShellBridge is not null)
    {
        Console.WriteLine($"Shell bridge protocol: {view.Value.ShellBridge.ProtocolVersion}");
        Console.WriteLine($"Shell bridge CLSID: {view.Value.ShellBridge.ExplorerCommandClsid}");
        Console.WriteLine($"Shell bridge menu state: {view.Value.ShellBridge.MenuState}");
        Console.WriteLine($"Shell bridge commands: {view.Value.ShellBridge.CommandCount}");
        Console.WriteLine($"Shell bridge invocable commands: {view.Value.ShellBridge.InvocableCommandCount}");
        Console.WriteLine($"Shell bridge visible icon commands: {view.Value.ShellBridge.VisibleIconCommandCount}");
        Console.WriteLine($"Shell bridge omitted icon commands: {view.Value.ShellBridge.OmittedIconCount}");
        Console.WriteLine($"Shell bridge target status: {view.Value.ShellBridge.Selection.Status}");
        Console.WriteLine($"Shell bridge target: {view.Value.ShellBridge.Selection.Target?.FullPath ?? "(none)"}");
        Console.WriteLine($"Shell bridge safety rules: {view.Value.ShellBridge.SafetyRules.Count}");
    }

    if (view.Value.PackagePlan is not null)
    {
        Console.WriteLine($"Package blockers: {view.Value.PackagePlan.BlockingCount}");
        Console.WriteLine($"Package warnings: {view.Value.PackagePlan.WarningCount}");
    }

    if (view.Value.AccessibilityPlan is not null)
    {
        Console.WriteLine($"Accessibility requirements: {view.Value.AccessibilityPlan.RequiredCount}");
        Console.WriteLine($"Accessibility manual proofs: {view.Value.AccessibilityPlan.ManualProofCount}");
    }

    if (!view.Value.IsContentReady)
    {
        Console.WriteLine($"Reason: {view.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(view.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {view.Value.Error.Detail}");
        }
    }

    return 0;
}

static int RunChangeIconWorkflow(string[] args)
{
    var selectedIconPath = GetOptionalValue(args, "--icon");
    var activationArgs = RemoveOption(args.Skip(1).ToArray(), "--icon");
    var workflow = new AppChangeIconWorkflowService().GetWorkflowFromEnvironment(
        activationArgs,
        selectedIconPath);
    if (!workflow.Succeeded || workflow.Value is null)
    {
        return WriteError(workflow.Error);
    }

    Console.WriteLine("Icon Replacer change icon workflow");
    Console.WriteLine($"Step: {workflow.Value.Step}");
    Console.WriteLine($"Can open picker: {(workflow.Value.CanOpenPicker ? "yes" : "no")}");
    Console.WriteLine($"Can preview: {(workflow.Value.CanPreview ? "yes" : "no")}");
    Console.WriteLine($"Can apply: {(workflow.Value.CanApply ? "yes" : "no")}");
    Console.WriteLine($"Activation kind: {workflow.Value.Activation.Kind}");

    if (workflow.Value.LaunchRequest is not null)
    {
        Console.WriteLine($"Target status: {workflow.Value.LaunchRequest.Selection.Status}");
        Console.WriteLine($"Target path: {workflow.Value.LaunchRequest.Selection.ResolvedPath ?? workflow.Value.LaunchRequest.RequestedTargetPath}");
        if (workflow.Value.LaunchRequest.Selection.Target is not null)
        {
            Console.WriteLine($"Target kind: {workflow.Value.LaunchRequest.Selection.Target.Kind}");
        }

        Console.WriteLine($"Picker initial directory: {workflow.Value.LaunchRequest.PickerRequest.InitialDirectory}");
    }

    if (!string.IsNullOrWhiteSpace(workflow.Value.SelectedIconPath))
    {
        Console.WriteLine($"Selected icon: {workflow.Value.SelectedIconPath}");
    }

    if (workflow.Value.Preview is not null)
    {
        if (workflow.Value.Preview.Preview.IconDetails is not null)
        {
            Console.WriteLine($"Icon display name: {workflow.Value.Preview.Preview.IconDetails.DisplayName}");
            Console.WriteLine($"Icon category: {workflow.Value.Preview.Preview.IconDetails.CategoryName ?? "(external)"}");
            Console.WriteLine($"Recommended image: {FormatImageDetail(workflow.Value.Preview.Preview.IconDetails.RecommendedImage)}");
        }
        else
        {
            Console.WriteLine($"Icon reason: {workflow.Value.Preview.Preview.IconError.Message}");
        }
    }

    if (workflow.Value.Error.Code != ErrorCode.None)
    {
        Console.WriteLine($"Reason: {workflow.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(workflow.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {workflow.Value.Error.Detail}");
        }
    }

    return workflow.Value.CanApply || workflow.Value.Step == AppChangeIconWorkflowStep.NeedIcon
        ? 0
        : ExitCodeForError(workflow.Value.Error);
}

static int RunStatus()
{
    var packagingInputs = DetectPackagingInputs();
    var snapshot = new SetupReadinessService().GetSnapshotFromEnvironment(packagingInputs);
    if (!snapshot.Succeeded || snapshot.Value is null)
    {
        return WriteError(snapshot.Error);
    }

    Console.WriteLine("Icon Replacer status");
    Console.WriteLine($"Icon library: {snapshot.Value.IconLibraryRoot}");
    Console.WriteLine($"Imported icons: {snapshot.Value.ImportedIconsRoot}");
    Console.WriteLine($"Restore state: {snapshot.Value.RestoreStateFile}");
    Console.WriteLine($"Core features: {(snapshot.Value.CanUseCoreFeatures ? "ready" : "blocked")}");
    Console.WriteLine($"Icons: {snapshot.Value.IconCount}");
    Console.WriteLine($"Categories: {snapshot.Value.CategoryCount}");
    Console.WriteLine($"Catalog warnings: {snapshot.Value.CatalogWarningCount}");
    Console.WriteLine($"Restore records: {snapshot.Value.RestoreRecordCount}");
    Console.WriteLine($"Restorable records: {snapshot.Value.RestorableRecordCount}");
    Console.WriteLine($"Shell integration: {snapshot.Value.ShellIntegration}");

    if (snapshot.Value.Actions.Count > 0)
    {
        Console.WriteLine("Actions:");
        foreach (var action in snapshot.Value.Actions)
        {
            Console.WriteLine($"- {action.Severity}: {action.Title} ({action.Id})");
            Console.WriteLine($"  {action.Detail}");
        }
    }

    return 0;
}

static int RunActionRequest(string[] args)
{
    if (args.Length != 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli action-request <action-id>");
        return 64;
    }

    var request = new AppActionRequestService().CreateRequestFromEnvironment(
        args[1],
        DetectPackagingInputs());
    if (!request.Succeeded || request.Value is null)
    {
        return WriteError(request.Error);
    }

    Console.WriteLine("Icon Replacer action request");
    Console.WriteLine($"Can execute: {(request.Value.CanExecute ? "yes" : "no")}");
    Console.WriteLine($"Action: {request.Value.Action.Id}");
    Console.WriteLine($"Title: {request.Value.Action.Title}");
    Console.WriteLine($"Severity: {request.Value.Action.Severity}");
    Console.WriteLine($"Kind: {request.Value.Kind}");
    Console.WriteLine($"Navigation target: {request.Value.NavigationTarget}");

    if (request.Value.HistoryFilter is not null)
    {
        Console.WriteLine($"History filter: {request.Value.HistoryFilter}");
    }

    if (!request.Value.CanExecute)
    {
        Console.WriteLine($"Reason: {request.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(request.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {request.Value.Error.Detail}");
        }
    }

    return request.Value.CanExecute ? 0 : ExitCodeForError(request.Value.Error);
}

static int RunActivate(string[] args)
{
    var activation = new AppActivationService().ActivateFromEnvironment(args.Skip(1).ToArray());
    if (!activation.Succeeded || activation.Value is null)
    {
        return WriteError(activation.Error);
    }

    Console.WriteLine("Icon Replacer activation");
    Console.WriteLine($"Kind: {activation.Value.Kind}");
    Console.WriteLine($"Can continue: {(activation.Value.CanContinue ? "yes" : "no")}");

    if (activation.Value.Kind == AppActivationKind.Home && activation.Value.Home is not null)
    {
        Console.WriteLine($"Core features: {(activation.Value.Home.CanUseCoreFeatures ? "ready" : "blocked")}");
        Console.WriteLine($"Needs attention: {(activation.Value.Home.NeedsAttention ? "yes" : "no")}");
        Console.WriteLine($"Shell integration: {activation.Value.Home.Setup.ShellIntegration}");
        Console.WriteLine($"Icons: {activation.Value.Home.Dashboard.IconCount}");
        Console.WriteLine($"Categories: {activation.Value.Home.Dashboard.CategoryCount}");
        Console.WriteLine($"Restore records: {activation.Value.Home.History.TotalCount}");
        return 0;
    }

    if (activation.Value.Kind == AppActivationKind.ChangeIcon && activation.Value.LaunchRequest is not null)
    {
        Console.WriteLine($"Verb: {activation.Value.LaunchRequest.VerbName}");
        Console.WriteLine($"Target status: {activation.Value.LaunchRequest.Selection.Status}");
        Console.WriteLine($"Target path: {activation.Value.LaunchRequest.Selection.ResolvedPath ?? activation.Value.LaunchRequest.RequestedTargetPath}");

        if (activation.Value.LaunchRequest.Selection.Target is not null)
        {
            Console.WriteLine($"Target kind: {activation.Value.LaunchRequest.Selection.Target.Kind}");
        }
        else
        {
            Console.WriteLine($"Target reason: {activation.Value.Error.Message}");
        }

        Console.WriteLine($"App arguments: {activation.Value.LaunchRequest.DisplayArguments}");
        Console.WriteLine($"Picker can open: {(activation.Value.LaunchRequest.PickerRequest.CanOpenPicker ? "yes" : "no")}");
        Console.WriteLine($"Picker initial directory: {activation.Value.LaunchRequest.PickerRequest.InitialDirectory}");
        return activation.Value.CanContinue ? 0 : ExitCodeForError(activation.Value.Error);
    }

    if (activation.Value.Kind == AppActivationKind.MenuApply && activation.Value.MenuApplyRequest is not null)
    {
        Console.WriteLine($"Verb: {activation.Value.MenuApplyRequest.VerbName}");
        Console.WriteLine($"Can apply: {(activation.Value.MenuApplyRequest.CanApply ? "yes" : "no")}");
        Console.WriteLine($"Target path: {activation.Value.MenuApplyRequest.RequestedTargetPath}");
        Console.WriteLine($"Icon path: {activation.Value.MenuApplyRequest.RequestedIconPath}");

        var selection = activation.Value.MenuApplyRequest.Invocation?.Selection;
        if (selection is not null)
        {
            Console.WriteLine($"Target status: {selection.Status}");
            if (selection.Target is not null)
            {
                Console.WriteLine($"Target kind: {selection.Target.Kind}");
            }
        }

        if (activation.Value.MenuApplyRequest.Command is not null)
        {
            Console.WriteLine($"Command id: {activation.Value.MenuApplyRequest.Command.Id}");
            Console.WriteLine($"Command label: {activation.Value.MenuApplyRequest.Command.Label}");
            Console.WriteLine($"Command category: {activation.Value.MenuApplyRequest.Command.CategoryName ?? "(root)"}");
        }

        Console.WriteLine($"App arguments: {activation.Value.MenuApplyRequest.DisplayArguments}");
        if (!activation.Value.MenuApplyRequest.CanApply)
        {
            Console.WriteLine($"Reason: {activation.Value.Error.Message}");
            if (!string.IsNullOrWhiteSpace(activation.Value.Error.Detail))
            {
                Console.WriteLine($"Detail: {activation.Value.Error.Detail}");
            }
        }

        return activation.Value.CanContinue ? 0 : ExitCodeForError(activation.Value.Error);
    }

    return activation.Value.CanContinue ? 0 : ExitCodeForError(activation.Value.Error);
}

static int RunActivatePreview(string[] args)
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli activate-preview <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>");
        return 64;
    }

    var preview = new ActivatedIconChangeService().PreviewSelectedIconFromEnvironment(
        args.Skip(2).ToArray(),
        args[1]);
    if (!preview.Succeeded || preview.Value is null)
    {
        return WriteError(preview.Error);
    }

    Console.WriteLine("Icon Replacer activated change preview");
    Console.WriteLine($"Activation kind: {preview.Value.Activation.Kind}");
    Console.WriteLine($"Can apply: {(preview.Value.CanApply ? "yes" : "no")}");
    Console.WriteLine($"Target status: {preview.Value.Preview.Selection.Status}");
    Console.WriteLine($"Target path: {preview.Value.Preview.Selection.ResolvedPath ?? preview.Value.Preview.RequestedTargetPath}");

    if (preview.Value.Preview.Selection.Target is not null)
    {
        Console.WriteLine($"Target kind: {preview.Value.Preview.Selection.Target.Kind}");
    }
    else
    {
        Console.WriteLine($"Target reason: {preview.Value.Error.Message}");
    }

    if (preview.Value.Preview.IconDetails is not null)
    {
        Console.WriteLine("Icon status: Valid");
        Console.WriteLine($"Icon path: {preview.Value.Preview.IconDetails.FullPath}");
        Console.WriteLine($"Icon display name: {preview.Value.Preview.IconDetails.DisplayName}");
        Console.WriteLine($"Icon category: {preview.Value.Preview.IconDetails.CategoryName}");
        Console.WriteLine($"Recommended image: {FormatImageDetail(preview.Value.Preview.IconDetails.RecommendedImage)}");
    }
    else
    {
        Console.WriteLine("Icon status: Invalid");
        Console.WriteLine($"Icon reason: {preview.Value.Preview.IconError.Message}");
    }

    if (!preview.Value.CanApply)
    {
        Console.WriteLine($"Reason: {preview.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(preview.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {preview.Value.Error.Detail}");
        }
    }

    return preview.Value.CanApply ? 0 : ExitCodeForError(preview.Value.Error);
}

static int RunActivateApply(string[] args)
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli activate-apply <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>");
        return 64;
    }

    var result = new ActivatedIconChangeService().ApplySelectedIconFromEnvironment(
        args.Skip(2).ToArray(),
        args[1]);
    if (!result.Succeeded || result.Value is null)
    {
        return WriteError(result.Error);
    }

    Console.WriteLine("Icon Replacer activated change apply");
    Console.WriteLine($"Activation kind: {result.Value.Activation.Kind}");
    return WriteApplyResult(OperationResult<IconApplyResult>.Success(
        result.Value.ChangeResult.ApplyResult));
}

static int RunActivateMenuApply(string[] args)
{
    if (args.Length != 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli activate-menu-apply <target> <icon-from-library.ico>");
        return 64;
    }

    var result = new ActivatedMenuApplyService().ApplyActivationFromEnvironment(
        [
            IconMenuCommandService.MenuApplyVerbName,
            args[1],
            args[2]
        ]);
    if (!result.Succeeded || result.Value is null)
    {
        return WriteError(result.Error);
    }

    Console.WriteLine("Icon Replacer activated menu apply");
    Console.WriteLine($"Activation kind: {result.Value.Activation.Kind}");
    Console.WriteLine($"Menu icon: {result.Value.MenuApplyResult.MenuItem.DisplayName}");
    Console.WriteLine($"Menu icon path: {result.Value.MenuApplyResult.MenuItem.IconPath}");
    return WriteApplyResult(OperationResult<IconApplyResult>.Success(
        result.Value.MenuApplyResult.ChangeResult.ApplyResult));
}

static int RunHome(string[] args)
{
    if (args.Length > 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli home [all|restorable|applied|restored|stale]");
        return 64;
    }

    var filter = RestoreHistoryFilter.All;
    if (args.Length == 2 && !TryParseHistoryFilter(args[1], out filter))
    {
        Console.Error.WriteLine("History filter must be one of: all, restorable, applied, restored, stale.");
        return 64;
    }

    var snapshot = new AppHomeService().GetSnapshotFromEnvironment(
        filter,
        packagingInputs: DetectPackagingInputs());
    if (!snapshot.Succeeded || snapshot.Value is null)
    {
        return WriteError(snapshot.Error);
    }

    Console.WriteLine("Icon Replacer home");
    Console.WriteLine($"Core features: {(snapshot.Value.CanUseCoreFeatures ? "ready" : "blocked")}");
    Console.WriteLine($"Needs attention: {(snapshot.Value.NeedsAttention ? "yes" : "no")}");
    Console.WriteLine($"Shell integration: {snapshot.Value.Setup.ShellIntegration}");
    Console.WriteLine($"Icon library: {snapshot.Value.Setup.IconLibraryRoot}");
    Console.WriteLine($"Icons: {snapshot.Value.Dashboard.IconCount}");
    Console.WriteLine($"Categories: {snapshot.Value.Dashboard.CategoryCount}");
    Console.WriteLine($"Menu icons: {snapshot.Value.Menu.VisibleIconCount}/{snapshot.Value.Menu.TotalIconCount} visible");
    Console.WriteLine($"Restore records: {snapshot.Value.History.TotalCount}");
    Console.WriteLine($"Restorable records: {snapshot.Value.History.RestorableCount}");
    Console.WriteLine($"Stale records: {snapshot.Value.History.StaleCount}");
    Console.WriteLine($"History filter: {snapshot.Value.History.Filter}");

    if (snapshot.Value.Setup.Actions.Count > 0)
    {
        Console.WriteLine("Actions:");
        foreach (var action in snapshot.Value.Setup.Actions)
        {
            Console.WriteLine($"- {action.Severity}: {action.Title} ({action.Id})");
        }
    }

    Console.WriteLine("Locations:");
    foreach (var location in snapshot.Value.Locations)
    {
        var kind = location.IsDirectory ? "directory" : "file";
        var exists = location.Exists ? "exists" : "missing";
        Console.WriteLine($"- {location.Label}: {location.FullPath} ({kind}, {exists})");
    }

    return 0;
}

static int RunPaths()
{
    var locations = new AppLocationService().GetLocationsFromEnvironment();
    if (!locations.Succeeded || locations.Value is null)
    {
        return WriteError(locations.Error);
    }

    Console.WriteLine("Icon Replacer paths");
    foreach (var location in locations.Value)
    {
        var kind = location.IsDirectory ? "directory" : "file";
        var exists = location.Exists ? "exists" : "missing";
        Console.WriteLine($"{location.Kind}: {location.FullPath} ({kind}, {exists})");
    }

    return 0;
}

static int RunOpenRequest(string[] args)
{
    if (args.Length != 2 || !TryParseLocationKind(args[1], out var kind))
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli open-request <icon-library|imported|appdata|restore-state>");
        return 64;
    }

    var request = new AppLocationService().CreateOpenRequestFromEnvironment(kind);
    if (!request.Succeeded || request.Value is null)
    {
        return WriteError(request.Error);
    }

    Console.WriteLine("Icon Replacer open request");
    Console.WriteLine($"Location: {request.Value.Location.Kind}");
    Console.WriteLine($"Label: {request.Value.Location.Label}");
    Console.WriteLine($"Can open: {(request.Value.CanOpen ? "yes" : "no")}");
    Console.WriteLine($"Target path: {request.Value.TargetPath}");
    Console.WriteLine($"Shell verb: {request.Value.ShellVerb}");

    if (!request.Value.CanOpen)
    {
        Console.WriteLine($"Reason: {request.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(request.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {request.Value.Error.Detail}");
        }
    }

    return request.Value.CanOpen ? 0 : ExitCodeForError(request.Value.Error);
}

static int RunTarget(string[] args)
{
    if (args.Length != 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli target <folder-or-shortcut>");
        return 64;
    }

    var evaluation = new ShellSelectionService().EvaluatePath(args[1]);
    if (!evaluation.Succeeded || evaluation.Value is null)
    {
        return WriteError(evaluation.Error);
    }

    Console.WriteLine("Icon Replacer target");
    Console.WriteLine($"Status: {evaluation.Value.Status}");
    Console.WriteLine($"Can show Change icon: {(evaluation.Value.CanShowChangeIcon ? "yes" : "no")}");

    if (evaluation.Value.Target is not null)
    {
        Console.WriteLine($"Target kind: {evaluation.Value.Target.Kind}");
        Console.WriteLine($"Target path: {evaluation.Value.Target.FullPath}");
        return 0;
    }

    Console.WriteLine($"Target path: {evaluation.Value.ResolvedPath ?? args[1]}");
    Console.WriteLine($"Reason: {evaluation.Value.Error.Message}");
    if (!string.IsNullOrWhiteSpace(evaluation.Value.Error.Detail))
    {
        Console.WriteLine($"Detail: {evaluation.Value.Error.Detail}");
    }

    return ExitCodeForError(evaluation.Value.Error);
}

static int RunPickerRequest(string[] args)
{
    if (args.Length != 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli picker-request <folder-or-shortcut>");
        return 64;
    }

    var request = new IconPickerRequestService().CreateRequestFromEnvironment(args[1]);
    if (!request.Succeeded || request.Value is null)
    {
        return WriteError(request.Error);
    }

    Console.WriteLine("Icon Replacer picker request");
    Console.WriteLine($"Can open picker: {(request.Value.CanOpenPicker ? "yes" : "no")}");
    Console.WriteLine($"Target status: {request.Value.Selection.Status}");
    Console.WriteLine($"Target path: {request.Value.Selection.ResolvedPath ?? request.Value.RequestedTargetPath}");

    if (request.Value.Selection.Target is not null)
    {
        Console.WriteLine($"Target kind: {request.Value.Selection.Target.Kind}");
    }
    else
    {
        Console.WriteLine($"Target reason: {request.Value.Error.Message}");
    }

    Console.WriteLine($"Picker title: {request.Value.Title}");
    Console.WriteLine($"Initial directory: {request.Value.InitialDirectory}");
    Console.WriteLine($"File type: {request.Value.FileTypeLabel}");
    Console.WriteLine($"Extensions: {string.Join(", ", request.Value.FileExtensions)}");
    Console.WriteLine($"Allow multiple: {(request.Value.AllowMultiple ? "yes" : "no")}");

    return request.Value.CanOpenPicker ? 0 : ExitCodeForError(request.Value.Error);
}

static int RunImportPickerRequest(string[] args)
{
    if (args.Length > 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli import-picker-request [collection]");
        return 64;
    }

    var collectionName = args.Length == 2 ? args[1] : null;
    var request = new IconImportPickerRequestService().CreateRequestFromEnvironment(collectionName);
    if (!request.Succeeded || request.Value is null)
    {
        return WriteError(request.Error);
    }

    Console.WriteLine("Icon Replacer import picker request");
    Console.WriteLine($"Can open picker: {(request.Value.CanOpenPicker ? "yes" : "no")}");
    Console.WriteLine($"Picker title: {request.Value.Title}");
    Console.WriteLine($"Initial directory: {request.Value.InitialDirectory}");
    Console.WriteLine($"Destination collection: {request.Value.DestinationCollectionName}");
    Console.WriteLine($"Destination directory: {request.Value.DestinationDirectory}");
    Console.WriteLine($"File type: {request.Value.FileTypeLabel}");
    Console.WriteLine($"Extensions: {string.Join(", ", request.Value.FileExtensions)}");
    Console.WriteLine($"Allow multiple: {(request.Value.AllowMultiple ? "yes" : "no")}");

    return request.Value.CanOpenPicker ? 0 : ExitCodeForError(request.Value.Error);
}

static int RunLaunchRequest(string[] args)
{
    if (args.Length != 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli launch-request <folder-or-shortcut>");
        return 64;
    }

    var request = new AppLaunchRequestService().CreateChangeIconRequestFromEnvironment(args[1]);
    if (!request.Succeeded || request.Value is null)
    {
        return WriteError(request.Error);
    }

    Console.WriteLine("Icon Replacer launch request");
    Console.WriteLine($"Verb: {request.Value.VerbName}");
    Console.WriteLine($"Can launch: {(request.Value.CanLaunch ? "yes" : "no")}");
    Console.WriteLine($"Target status: {request.Value.Selection.Status}");
    Console.WriteLine($"Target path: {request.Value.Selection.ResolvedPath ?? request.Value.RequestedTargetPath}");

    if (request.Value.Selection.Target is not null)
    {
        Console.WriteLine($"Target kind: {request.Value.Selection.Target.Kind}");
    }
    else
    {
        Console.WriteLine($"Target reason: {request.Value.Error.Message}");
    }

    Console.WriteLine($"App arguments: {request.Value.DisplayArguments}");
    Console.WriteLine($"Picker can open: {(request.Value.PickerRequest.CanOpenPicker ? "yes" : "no")}");
    Console.WriteLine($"Picker initial directory: {request.Value.PickerRequest.InitialDirectory}");
    Console.WriteLine($"Picker extensions: {string.Join(", ", request.Value.PickerRequest.FileExtensions)}");

    return request.Value.CanLaunch ? 0 : ExitCodeForError(request.Value.Error);
}

static int RunCatalog()
{
    var paths = IconLibraryPaths.FromEnvironment();
    if (!paths.Succeeded || paths.Value is null)
    {
        Console.Error.WriteLine(paths.Error.Message);
        return 1;
    }

    var catalog = new IconCatalogService().Scan(paths.Value);
    if (!catalog.Succeeded || catalog.Value is null)
    {
        Console.Error.WriteLine(catalog.Error.Message);
        if (!string.IsNullOrWhiteSpace(catalog.Error.Detail))
        {
            Console.Error.WriteLine(catalog.Error.Detail);
        }

        return 1;
    }

    Console.WriteLine($"Icon library: {catalog.Value.LibraryRoot}");
    Console.WriteLine($"Categories: {catalog.Value.Categories.Count}");
    Console.WriteLine($"Icons: {catalog.Value.Entries.Count}");

    foreach (var entry in catalog.Value.Entries)
    {
        var category = entry.Category is null ? "(root)" : entry.Category.Name;
        Console.WriteLine($"{category}\\{entry.DisplayName} -> {entry.FullPath}");
    }

    if (catalog.Value.Warnings.Count > 0)
    {
        Console.WriteLine($"Warnings: {catalog.Value.Warnings.Count}");
        foreach (var warning in catalog.Value.Warnings)
        {
            Console.WriteLine($"{warning.Path}: {warning.Error.Message}");
        }
    }

    return 0;
}

static int RunCatalogWarnings()
{
    var snapshot = new CatalogWarningsService().GetWarningsFromEnvironment();
    if (!snapshot.Succeeded || snapshot.Value is null)
    {
        return WriteError(snapshot.Error);
    }

    Console.WriteLine($"Icon Library: {snapshot.Value.IconLibraryRoot}");
    Console.WriteLine($"Valid icons: {snapshot.Value.TotalIconCount}");
    Console.WriteLine($"Warnings: {snapshot.Value.WarningCount}");

    foreach (var warning in snapshot.Value.Warnings)
    {
        var category = warning.CategoryName is null ? "(root)" : warning.CategoryName;
        Console.WriteLine($"{category} | {warning.DisplayName} | {warning.ErrorCode}: {warning.Message}");
        if (!string.IsNullOrWhiteSpace(warning.Detail))
        {
            Console.WriteLine($"  {warning.Detail}");
        }
    }

    return snapshot.Value.HasWarnings ? 1 : 0;
}

static int RunCollections()
{
    var result = new IconCollectionService().ListCollectionsFromEnvironment();
    if (!result.Succeeded || result.Value is null)
    {
        return WriteError(result.Error);
    }

    Console.WriteLine("Icon Replacer collections");
    Console.WriteLine($"Collections: {result.Value.Count}");

    foreach (var collection in result.Value)
    {
        var imported = collection.IsImportedCollection ? " imported" : string.Empty;
        Console.WriteLine($"{collection.Name} | {collection.IconCount} icons | {collection.FullPath}{imported}");
    }

    return 0;
}

static int RunCollectionCreate(string[] args)
{
    if (args.Length != 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli collection-create <name>");
        return 64;
    }

    var result = new IconCollectionService().CreateCollectionFromEnvironment(args[1]);
    if (!result.Succeeded || result.Value is null)
    {
        return WriteError(result.Error);
    }

    Console.WriteLine(result.Value.Created
        ? "Icon collection created."
        : "Icon collection already exists.");
    Console.WriteLine($"Collection: {result.Value.Collection.Name}");
    Console.WriteLine($"Path: {result.Value.Collection.FullPath}");
    Console.WriteLine($"Icons: {result.Value.Collection.IconCount}");
    Console.WriteLine($"Catalog categories: {result.Value.LibraryStatus.CategoryCount}");
    Console.WriteLine($"Catalog icons: {result.Value.LibraryStatus.IconCount}");
    Console.WriteLine($"Catalog warnings: {result.Value.LibraryStatus.WarningCount}");
    return 0;
}

static int RunCollectionImport(string[] args)
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli collection-import <collection> <icon.ico> [icon2.ico ...]");
        return 64;
    }

    var result = new IconCollectionImportService().ImportIntoCollectionFromEnvironment(
        args[1],
        args.Skip(2).ToArray());
    if (!result.Succeeded || result.Value is null)
    {
        return WriteError(result.Error);
    }

    Console.WriteLine("Icon collection import");
    Console.WriteLine($"Collection: {result.Value.Collection.Name}");
    Console.WriteLine($"Collection path: {result.Value.Collection.FullPath}");
    Console.WriteLine($"Requested: {result.Value.RequestedCount}");
    Console.WriteLine($"Imported: {result.Value.ImportedCount}");
    Console.WriteLine($"Reused existing: {result.Value.ReusedExistingCount}");
    Console.WriteLine($"Failed: {result.Value.FailedCount}");
    Console.WriteLine($"Collection icons: {result.Value.Collection.IconCount}");
    Console.WriteLine($"Catalog icons: {result.Value.LibraryStatus.IconCount}");
    Console.WriteLine($"Catalog warnings: {result.Value.LibraryStatus.WarningCount}");

    foreach (var item in result.Value.Items)
    {
        Console.WriteLine(FormatBatchImportItem(item));
    }

    return result.Value.FailedCount > 0 ? 1 : 0;
}

static int RunMenu()
{
    var snapshot = new IconMenuService().BuildSnapshotFromEnvironment();
    if (!snapshot.Succeeded || snapshot.Value is null)
    {
        return WriteError(snapshot.Error);
    }

    Console.WriteLine($"Icon menu: {snapshot.Value.IconLibraryRoot}");
    Console.WriteLine($"Command: {snapshot.Value.ChangeIconCommandLabel}");
    Console.WriteLine($"State: {snapshot.Value.State}");
    Console.WriteLine($"Status: {snapshot.Value.StatusMessage}");
    if (!string.IsNullOrWhiteSpace(snapshot.Value.RecommendedActionLabel))
    {
        Console.WriteLine($"Recommended action: {snapshot.Value.RecommendedActionLabel}");
    }

    Console.WriteLine($"Icons: {snapshot.Value.VisibleIconCount}/{snapshot.Value.TotalIconCount} visible");
    Console.WriteLine($"Categories: {snapshot.Value.Categories.Count}");

    if (!snapshot.Value.IsAvailable)
    {
        Console.WriteLine($"Reason: {snapshot.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(snapshot.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {snapshot.Value.Error.Detail}");
        }

        return ExitCodeForError(snapshot.Value.Error);
    }

    if (snapshot.Value.OmittedIconCount > 0)
    {
        Console.WriteLine($"Omitted icons: {snapshot.Value.OmittedIconCount}");
    }

    if (snapshot.Value.IsEmpty)
    {
        Console.WriteLine("No icons found.");
        return 0;
    }

    foreach (var rootIcon in snapshot.Value.RootIcons)
    {
        Console.WriteLine($"(root)\\{rootIcon.DisplayName} -> {rootIcon.IconPath}");
    }

    foreach (var category in snapshot.Value.Categories)
    {
        var suffix = category.IsTruncated
            ? $" ({category.Items.Count}/{category.TotalIconCount} shown)"
            : $" ({category.TotalIconCount})";
        Console.WriteLine($"[{category.Name}]{suffix}");

        foreach (var item in category.Items)
        {
            Console.WriteLine($"  {item.DisplayName} -> {item.IconPath}");
        }
    }

    if (snapshot.Value.Warnings.Count > 0)
    {
        Console.WriteLine($"Warnings: {snapshot.Value.Warnings.Count}");
        foreach (var warning in snapshot.Value.Warnings)
        {
            Console.WriteLine($"{warning.Path}: {warning.Error.Message}");
        }
    }

    return 0;
}

static int RunMenuCommands()
{
    var snapshot = new IconMenuCommandService().BuildCommandsFromEnvironment();
    if (!snapshot.Succeeded || snapshot.Value is null)
    {
        return WriteError(snapshot.Error);
    }

    Console.WriteLine($"Icon menu commands: {snapshot.Value.IconLibraryRoot}");
    Console.WriteLine($"State: {snapshot.Value.State}");
    Console.WriteLine($"Status: {snapshot.Value.StatusMessage}");
    if (!snapshot.Value.IsAvailable)
    {
        Console.WriteLine($"Reason: {snapshot.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(snapshot.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {snapshot.Value.Error.Detail}");
        }
    }

    Console.WriteLine($"Commands: {snapshot.Value.Commands.Count}");
    Console.WriteLine($"Icon commands: {snapshot.Value.VisibleIconCommandCount}/{snapshot.Value.TotalIconCount} visible");
    Console.WriteLine($"Omitted icons: {snapshot.Value.OmittedIconCount}");

    foreach (var command in snapshot.Value.Commands)
    {
        var category = string.IsNullOrWhiteSpace(command.CategoryName)
            ? string.Empty
            : $" | {command.CategoryName}";
        var icon = string.IsNullOrWhiteSpace(command.IconPath)
            ? string.Empty
            : $" | {command.IconPath}";
        var arguments = string.IsNullOrWhiteSpace(command.DisplayArguments)
            ? string.Empty
            : $" | {command.DisplayArguments}";
        Console.WriteLine($"{command.Kind} | {command.Id} | {command.Label}{category}{icon}{arguments}");
    }

    return 0;
}

static int RunMenuInvokePreview(string[] args)
{
    if (args.Length is < 2 or > 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli menu-invoke-preview <command-id> [target]");
        return 64;
    }

    var targetPath = args.Length == 3 ? args[2] : null;
    var preview = new IconMenuCommandInvocationService().PreviewInvocationFromEnvironment(args[1], targetPath);
    if (!preview.Succeeded || preview.Value is null)
    {
        return WriteError(preview.Error);
    }

    Console.WriteLine("Icon menu invocation preview");
    Console.WriteLine($"Can invoke: {(preview.Value.CanInvoke ? "yes" : "no")}");
    Console.WriteLine($"Command: {preview.Value.Command.Id}");
    Console.WriteLine($"Kind: {preview.Value.Command.Kind}");
    Console.WriteLine($"Label: {preview.Value.Command.Label}");
    Console.WriteLine($"Requires target: {(preview.Value.Command.RequiresTarget ? "yes" : "no")}");

    if (preview.Value.Selection is not null)
    {
        Console.WriteLine($"Target status: {preview.Value.Selection.Status}");
        Console.WriteLine($"Target path: {preview.Value.Selection.ResolvedPath ?? targetPath}");
        if (preview.Value.Selection.Target is not null)
        {
            Console.WriteLine($"Target kind: {preview.Value.Selection.Target.Kind}");
        }
    }

    Console.WriteLine($"Arguments: {preview.Value.DisplayArguments}");

    if (!preview.Value.CanInvoke)
    {
        Console.WriteLine($"Reason: {preview.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(preview.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {preview.Value.Error.Detail}");
        }
    }

    return preview.Value.CanInvoke ? 0 : ExitCodeForError(preview.Value.Error);
}

static int RunMenuApply(string[] args)
{
    if (args.Length != 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli menu-apply <target> <icon-from-library.ico>");
        return 64;
    }

    var result = new IconMenuApplyService().ApplyMenuIconFromEnvironment(args[1], args[2]);
    if (!result.Succeeded || result.Value is null)
    {
        return WriteError(result.Error);
    }

    Console.WriteLine("Icon Replacer menu apply");
    Console.WriteLine($"Menu icon: {result.Value.MenuItem.DisplayName}");
    Console.WriteLine($"Menu icon path: {result.Value.MenuItem.IconPath}");
    return WriteApplyResult(OperationResult<IconApplyResult>.Success(
        result.Value.ChangeResult.ApplyResult));
}

static int RunBrowse(string[] args)
{
    string? search = null;
    string? category = null;
    var maxItems = 60;

    for (var i = 1; i < args.Length; i++)
    {
        var arg = args[i];
        if (string.Equals(arg, "--category", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "-c", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 >= args.Length)
            {
                return WriteBrowseUsage();
            }

            category = args[++i];
        }
        else if (string.Equals(arg, "--max", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "-m", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 >= args.Length || !int.TryParse(args[++i], out maxItems) || maxItems < 0)
            {
                return WriteBrowseUsage();
            }
        }
        else if (search is null)
        {
            search = arg;
        }
        else
        {
            return WriteBrowseUsage();
        }
    }

    var snapshot = new IconBrowserService().BrowseFromEnvironment(
        new IconBrowserOptions(search, category, maxItems));
    if (!snapshot.Succeeded || snapshot.Value is null)
    {
        return WriteError(snapshot.Error);
    }

    Console.WriteLine($"Icon browser: {snapshot.Value.IconLibraryRoot}");
    Console.WriteLine($"Search: {snapshot.Value.SearchText ?? "(none)"}");
    Console.WriteLine($"Category: {snapshot.Value.CategoryName ?? "(all)"}");
    Console.WriteLine($"Icons: {snapshot.Value.VisibleIconCount}/{snapshot.Value.MatchedIconCount} shown ({snapshot.Value.TotalIconCount} total)");
    Console.WriteLine($"Categories: {snapshot.Value.Categories.Count}");

    if (snapshot.Value.OmittedIconCount > 0)
    {
        Console.WriteLine($"Omitted icons: {snapshot.Value.OmittedIconCount}");
    }

    foreach (var item in snapshot.Value.Items)
    {
        Console.WriteLine($"{item.CategoryName}\\{item.DisplayName} -> {item.FullPath}");
    }

    if (snapshot.Value.Warnings.Count > 0)
    {
        Console.WriteLine($"Warnings: {snapshot.Value.Warnings.Count}");
        foreach (var warning in snapshot.Value.Warnings)
        {
            Console.WriteLine($"{warning.Path}: {warning.Error.Message}");
        }
    }

    return 0;
}

static int RunDetails(string[] args)
{
    if (args.Length != 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli details <icon.ico>");
        return 64;
    }

    var details = new IconDetailsService().GetDetailsFromEnvironment(args[1]);
    if (!details.Succeeded || details.Value is null)
    {
        return WriteError(details.Error);
    }

    Console.WriteLine("Icon details");
    Console.WriteLine($"Path: {details.Value.FullPath}");
    Console.WriteLine($"Display name: {details.Value.DisplayName}");
    Console.WriteLine($"Category: {details.Value.CategoryName}");
    Console.WriteLine($"In Icon Library: {(details.Value.IsInIconLibrary ? "yes" : "no")}");
    Console.WriteLine($"Length: {details.Value.LengthBytes} bytes");
    Console.WriteLine($"Images: {details.Value.ImageCount}");
    Console.WriteLine($"Recommended image: {FormatImageDetail(details.Value.RecommendedImage)}");

    foreach (var image in details.Value.Images)
    {
        Console.WriteLine($"- {FormatImageDetail(image)}");
    }

    return 0;
}

static int RunImport(string[] args)
{
    if (args.Length is < 2 or > 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli import <icon.ico> [display-name]");
        return 64;
    }

    var result = new IconLibraryService().ImportIconFromEnvironment(
        args[1],
        args.Length == 3 ? args[2] : null);
    if (!result.Succeeded || result.Value is null)
    {
        return WriteError(result.Error);
    }

    Console.WriteLine("Icon imported.");
    Console.WriteLine($"Imported icon: {result.Value.ImportedIcon.FullPath}");
    Console.WriteLine($"Display name: {result.Value.ImportedIcon.DisplayName}");
    Console.WriteLine($"Icon library: {result.Value.LibraryStatus.LibraryRoot}");
    Console.WriteLine($"Catalog icons: {result.Value.LibraryStatus.IconCount}");
    Console.WriteLine($"Catalog warnings: {result.Value.LibraryStatus.WarningCount}");
    return 0;
}

static int RunBatchImport(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli batch-import <icon.ico> [icon2.ico ...]");
        return 64;
    }

    var result = new IconLibraryService().ImportIconsFromEnvironment(args.Skip(1).ToArray());
    if (!result.Succeeded || result.Value is null)
    {
        return WriteError(result.Error);
    }

    Console.WriteLine("Icon batch import");
    Console.WriteLine($"Requested: {result.Value.RequestedCount}");
    Console.WriteLine($"Imported: {result.Value.ImportedCount}");
    Console.WriteLine($"Reused existing: {result.Value.ReusedExistingCount}");
    Console.WriteLine($"Failed: {result.Value.FailedCount}");
    Console.WriteLine($"Catalog icons: {result.Value.LibraryStatus.IconCount}");
    Console.WriteLine($"Catalog warnings: {result.Value.LibraryStatus.WarningCount}");

    foreach (var item in result.Value.Items)
    {
        Console.WriteLine(FormatBatchImportItem(item));
    }

    return result.Value.FailedCount > 0 ? 1 : 0;
}

static int RunPreviewChange(string[] args)
{
    if (args.Length != 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli preview-change <target> <icon.ico>");
        return 64;
    }

    var preview = new IconChangePreviewService().PreviewChangeFromEnvironment(args[1], args[2]);
    if (!preview.Succeeded || preview.Value is null)
    {
        return WriteError(preview.Error);
    }

    Console.WriteLine("Icon Replacer change preview");
    Console.WriteLine($"Can apply: {(preview.Value.CanApply ? "yes" : "no")}");
    Console.WriteLine($"Target status: {preview.Value.Selection.Status}");
    Console.WriteLine($"Target path: {preview.Value.Selection.ResolvedPath ?? preview.Value.RequestedTargetPath}");

    if (preview.Value.Selection.Target is not null)
    {
        Console.WriteLine($"Target kind: {preview.Value.Selection.Target.Kind}");
    }
    else
    {
        Console.WriteLine($"Target reason: {preview.Value.Selection.Error.Message}");
    }

    if (preview.Value.IconDetails is not null)
    {
        Console.WriteLine("Icon status: Valid");
        Console.WriteLine($"Icon path: {preview.Value.IconDetails.FullPath}");
        Console.WriteLine($"Icon display name: {preview.Value.IconDetails.DisplayName}");
        Console.WriteLine($"Icon category: {preview.Value.IconDetails.CategoryName}");
        Console.WriteLine($"Recommended image: {FormatImageDetail(preview.Value.IconDetails.RecommendedImage)}");
    }
    else
    {
        Console.WriteLine("Icon status: Invalid");
        Console.WriteLine($"Icon reason: {preview.Value.IconError.Message}");
        if (!string.IsNullOrWhiteSpace(preview.Value.IconError.Detail))
        {
            Console.WriteLine($"Icon detail: {preview.Value.IconError.Detail}");
        }
    }

    if (!preview.Value.CanApply)
    {
        Console.WriteLine($"Reason: {preview.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(preview.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {preview.Value.Error.Detail}");
        }
    }

    return preview.Value.CanApply ? 0 : ExitCodeForError(preview.Value.Error);
}

static int RunHistory(string[] args)
{
    if (args.Length > 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli history [all|restorable|applied|restored|stale]");
        return 64;
    }

    var filter = RestoreHistoryFilter.All;
    if (args.Length == 2 && !TryParseHistoryFilter(args[1], out filter))
    {
        Console.Error.WriteLine("History filter must be one of: all, restorable, applied, restored, stale.");
        return 64;
    }

    var history = new RestoreHistoryService().GetHistoryFromEnvironment(filter);
    if (!history.Succeeded || history.Value is null)
    {
        return WriteError(history.Error);
    }

    Console.WriteLine($"Restore state: {history.Value.RestoreStateFile}");
    Console.WriteLine($"Restore records: {history.Value.TotalCount}");
    Console.WriteLine($"Filter: {history.Value.Filter}");
    Console.WriteLine($"Shown records: {history.Value.Records.Count}");
    Console.WriteLine($"Restorable records: {history.Value.RestorableCount}");
    Console.WriteLine($"Stale records: {history.Value.StaleCount}");

    foreach (var record in history.Value.Records)
    {
        Console.WriteLine(FormatRecord(record));
    }

    return 0;
}

static int RunRestoreWorkflow(string[] args)
{
    var parsed = ParseRestoreWorkflowArgs(args);
    if (!parsed.Succeeded)
    {
        return parsed.ExitCode;
    }

    var workflow = new AppRestoreWorkflowService().GetWorkflowFromEnvironment(parsed.RecordId, parsed.Filter);
    if (!workflow.Succeeded || workflow.Value is null)
    {
        return WriteError(workflow.Error);
    }

    Console.WriteLine("Icon Replacer restore workflow");
    Console.WriteLine($"Step: {workflow.Value.Step}");
    Console.WriteLine($"Can preview: {(workflow.Value.CanPreview ? "yes" : "no")}");
    Console.WriteLine($"Can restore: {(workflow.Value.CanRestore ? "yes" : "no")}");
    Console.WriteLine($"Selected record: {workflow.Value.SelectedRecordId?.ToString() ?? "(none)"}");
    Console.WriteLine($"Restore state: {workflow.Value.History.RestoreStateFile}");
    Console.WriteLine($"History filter: {workflow.Value.History.Filter}");
    Console.WriteLine($"History records: {workflow.Value.History.TotalCount}");
    Console.WriteLine($"Shown records: {workflow.Value.History.Records.Count}");
    Console.WriteLine($"Restorable records: {workflow.Value.History.RestorableCount}");
    Console.WriteLine($"Stale records: {workflow.Value.History.StaleCount}");

    if (workflow.Value.Preview is not null)
    {
        Console.WriteLine($"Record status: {workflow.Value.Preview.Record.Status}");
        Console.WriteLine($"Target kind: {workflow.Value.Preview.Record.TargetKind}");
        Console.WriteLine($"Target: {workflow.Value.Preview.Record.TargetPath}");
        Console.WriteLine($"Applied icon: {workflow.Value.Preview.Record.AppliedIconPath}");
        Console.WriteLine($"Target exists: {(workflow.Value.Preview.Record.TargetExists ? "yes" : "no")}");
        Console.WriteLine($"Applied icon exists: {(workflow.Value.Preview.Record.AppliedIconExists ? "yes" : "no")}");
        Console.WriteLine($"Previous state: {workflow.Value.Preview.PreviousStateDetail}");
        Console.WriteLine($"Action: {workflow.Value.Preview.RestoreActionDetail}");

        if (!string.IsNullOrWhiteSpace(workflow.Value.Preview.WarningText))
        {
            Console.WriteLine($"Warning: {workflow.Value.Preview.WarningText}");
        }
    }

    if (workflow.Value.Error.Code != ErrorCode.None)
    {
        Console.WriteLine($"Reason: {workflow.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(workflow.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {workflow.Value.Error.Detail}");
        }
    }

    return workflow.Value.CanRestore || workflow.Value.Step == AppRestoreWorkflowStep.NeedRecord
        ? 0
        : ExitCodeForError(workflow.Value.Error);
}

static int RunRecent(string[] args)
{
    if (args.Length > 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli recent [all|restorable|applied|restored|stale]");
        return 64;
    }

    var filter = RestoreHistoryFilter.All;
    if (args.Length == 2 && !TryParseHistoryFilter(args[1], out filter))
    {
        Console.Error.WriteLine("Recent filter must be one of: all, restorable, applied, restored, stale.");
        return 64;
    }

    var snapshot = new RecentChangesService().GetRecentChangesFromEnvironment(filter);
    if (!snapshot.Succeeded || snapshot.Value is null)
    {
        return WriteError(snapshot.Error);
    }

    Console.WriteLine($"Restore state: {snapshot.Value.RestoreStateFile}");
    Console.WriteLine($"Recent changes: {snapshot.Value.TotalCount}");
    Console.WriteLine($"Filter: {snapshot.Value.Filter}");
    Console.WriteLine($"Shown changes: {snapshot.Value.ShownCount}");
    Console.WriteLine($"Restore-enabled changes: {snapshot.Value.RestorableCount}");
    Console.WriteLine($"Warning changes: {snapshot.Value.WarningCount}");
    Console.WriteLine($"Disabled changes: {snapshot.Value.DisabledCount}");

    foreach (var item in snapshot.Value.Items)
    {
        Console.WriteLine(FormatRecentChange(item));
    }

    return 0;
}

static int RunApply(string[] args)
{
    if (args.Length != 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli apply <target> <icon.ico>");
        return 64;
    }

    var paths = IconLibraryPaths.FromEnvironment();
    if (!paths.Succeeded || paths.Value is null)
    {
        return WriteError(paths.Error);
    }

    var result = new IconApplyService().Apply(args[1], args[2], paths.Value);
    return WriteApplyResult(result);
}

static int RunChange(string[] args)
{
    if (args.Length != 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli change <target> <icon.ico>");
        return 64;
    }

    var paths = IconLibraryPaths.FromEnvironment();
    if (!paths.Succeeded || paths.Value is null)
    {
        return WriteError(paths.Error);
    }

    var result = new IconChangeService().ChangeIcon(args[1], args[2], paths.Value);
    if (!result.Succeeded || result.Value is null)
    {
        return WriteError(result.Error);
    }

    return WriteApplyResult(OperationResult<IconApplyResult>.Success(result.Value.ApplyResult));
}

static int RunApplyFolder(string[] args)
{
    if (args.Length != 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli apply-folder <folder> <icon.ico>");
        return 64;
    }

    var paths = IconLibraryPaths.FromEnvironment();
    if (!paths.Succeeded || paths.Value is null)
    {
        return WriteError(paths.Error);
    }

    var result = new IconApplyService().ApplyToShellSelection(args[1], isDirectory: true, args[2], paths.Value);
    return WriteApplyResult(result);
}

static int RunApplyShortcut(string[] args)
{
    if (args.Length != 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli apply-shortcut <shortcut.lnk> <icon.ico>");
        return 64;
    }

    var paths = IconLibraryPaths.FromEnvironment();
    if (!paths.Succeeded || paths.Value is null)
    {
        return WriteError(paths.Error);
    }

    var result = new IconApplyService().ApplyToShellSelection(args[1], isDirectory: false, args[2], paths.Value);
    return WriteApplyResult(result);
}

static int RunRestore(string[] args)
{
    if (args.Length != 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli restore <record-id>");
        return 64;
    }

    if (!Guid.TryParse(args[1], out var recordId))
    {
        Console.Error.WriteLine("Restore record id must be a GUID.");
        return 64;
    }

    var paths = IconLibraryPaths.FromEnvironment();
    if (!paths.Succeeded || paths.Value is null)
    {
        return WriteError(paths.Error);
    }

    var restore = new IconRestoreService().Restore(recordId, paths.Value);
    return WriteRestoreResult(restore);
}

static int RunRestorePreview(string[] args)
{
    if (args.Length != 2)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli restore-preview <record-id>");
        return 64;
    }

    if (!Guid.TryParse(args[1], out var recordId))
    {
        Console.Error.WriteLine("Restore record id must be a GUID.");
        return 64;
    }

    var preview = new IconRestorePreviewService().PreviewRestoreFromEnvironment(recordId);
    if (!preview.Succeeded || preview.Value is null)
    {
        return WriteError(preview.Error);
    }

    Console.WriteLine("Icon Replacer restore preview");
    Console.WriteLine($"Can restore: {(preview.Value.CanRestore ? "yes" : "no")}");
    Console.WriteLine($"Record: {preview.Value.Record.Id}");
    Console.WriteLine($"Status: {preview.Value.Record.Status}");
    Console.WriteLine($"Target kind: {preview.Value.Record.TargetKind}");
    Console.WriteLine($"Target: {preview.Value.Record.TargetPath}");
    Console.WriteLine($"Applied icon: {preview.Value.Record.AppliedIconPath}");
    Console.WriteLine($"Target exists: {(preview.Value.Record.TargetExists ? "yes" : "no")}");
    Console.WriteLine($"Applied icon exists: {(preview.Value.Record.AppliedIconExists ? "yes" : "no")}");
    Console.WriteLine($"Previous state: {preview.Value.PreviousStateDetail}");
    Console.WriteLine($"Action: {preview.Value.RestoreActionDetail}");

    if (!string.IsNullOrWhiteSpace(preview.Value.WarningText))
    {
        Console.WriteLine($"Warning: {preview.Value.WarningText}");
    }

    if (!preview.Value.CanRestore)
    {
        Console.WriteLine($"Reason: {preview.Value.Error.Message}");
        if (!string.IsNullOrWhiteSpace(preview.Value.Error.Detail))
        {
            Console.WriteLine($"Detail: {preview.Value.Error.Detail}");
        }
    }

    return preview.Value.CanRestore ? 0 : ExitCodeForError(preview.Value.Error);
}

static int WriteError(IconReplacerError error)
{
    Console.Error.WriteLine(error.Message);
    if (!string.IsNullOrWhiteSpace(error.Detail))
    {
        Console.Error.WriteLine(error.Detail);
    }

    return ExitCodeForError(error);
}

static int ExitCodeForError(IconReplacerError error)
{
    return error.Code switch
    {
        ErrorCode.InvalidArgument => 64,
        ErrorCode.PathNotFound => 66,
        ErrorCode.UnsupportedTarget => 65,
        ErrorCode.InvalidIcon => 65,
        ErrorCode.PermissionDenied => 77,
        ErrorCode.RemotePathUnsupported => 65,
        ErrorCode.NotImplementedYet => 2,
        _ => 1
    };
}

static int UnknownCommand(string command)
{
    Console.Error.WriteLine($"Unknown command '{command}'.");
    WriteUsage();
    return 64;
}

static int WriteBrowseUsage()
{
    Console.Error.WriteLine("Usage: IconReplacer.Cli browse [search] [--category <name>] [--max <count>]");
    return 64;
}

static WinUiToolingSnapshot DetectWinUiTooling()
{
    var templates = RunProcess("dotnet", "new list winui");
    var winapp = RunProcess("where.exe", "winapp");
    var winAppPath = winapp.ExitCode == 0
        ? "winapp"
        : FindWindowsAppsTool("winapp.exe");
    var templatesAvailable = templates.ExitCode == 0 &&
        templates.Output.Contains("WinUI", StringComparison.OrdinalIgnoreCase);
    var winAppAvailable = winAppPath is not null;

    return new WinUiToolingSnapshot(
        IsChecked: true,
        templatesAvailable,
        winAppAvailable,
        templatesAvailable
            ? "WinUI templates are available."
            : "WinUI templates were not found; run /winui-setup before scaffolding WinUI.",
        winAppAvailable
            ? $"winapp CLI is available ({winAppPath})."
            : "winapp CLI was not found; run /winui-setup before scaffolding or running WinUI.");
}

static string? FindWindowsAppsTool(string fileName)
{
    var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    if (string.IsNullOrWhiteSpace(localAppData))
    {
        return null;
    }

    var toolPath = Path.Combine(localAppData, "Microsoft", "WindowsApps", fileName);
    return File.Exists(toolPath) ? toolPath : null;
}

static NativeToolingSnapshot DetectNativeTooling()
{
    var compiler = RunProcess("where.exe", "cl");
    var msbuild = RunProcess("where.exe", "msbuild");
    var cmake = RunProcess("where.exe", "cmake");
    var visualStudioTooling = FindVisualStudioNativeTooling();
    var compilerAvailable = compiler.ExitCode == 0 || visualStudioTooling.CompilerPath is not null;
    var msBuildAvailable = msbuild.ExitCode == 0 || visualStudioTooling.MsBuildPath is not null;
    var cmakeAvailable = cmake.ExitCode == 0;

    return new NativeToolingSnapshot(
        IsChecked: true,
        compilerAvailable,
        msBuildAvailable,
        cmakeAvailable,
        compiler.ExitCode == 0
            ? "cl.exe is available."
            : visualStudioTooling.CompilerPath is not null
                ? $"cl.exe is available through Visual Studio developer tools: {visualStudioTooling.CompilerPath}"
                : "cl.exe was not found on PATH or in Visual Studio C++ Build Tools.",
        msbuild.ExitCode == 0
            ? "Visual Studio MSBuild is available."
            : visualStudioTooling.MsBuildPath is not null
                ? $"Visual Studio MSBuild is available through Visual Studio developer tools: {visualStudioTooling.MsBuildPath}"
                : "Visual Studio MSBuild was not found on PATH or in Visual Studio Build Tools.",
        cmakeAvailable
            ? "CMake is available."
            : "CMake was not found on PATH.");
}

static (string? InstallationPath, string? VsDevCmdPath, string? CompilerPath, string? MsBuildPath) FindVisualStudioNativeTooling()
{
    var installationPath = FindVisualStudioInstallationPath();
    if (string.IsNullOrWhiteSpace(installationPath) || !Directory.Exists(installationPath))
    {
        return (null, null, null, null);
    }

    var vsDevCmdPath = Path.Combine(installationPath, "Common7", "Tools", "VsDevCmd.bat");
    if (!File.Exists(vsDevCmdPath))
    {
        vsDevCmdPath = null;
    }

    var compilerPath = FindPreferredFile(installationPath, "cl.exe", "Hostx64", "x64");
    var msBuildPath = Path.Combine(installationPath, "MSBuild", "Current", "Bin", "amd64", "MSBuild.exe");
    if (!File.Exists(msBuildPath))
    {
        msBuildPath = Path.Combine(installationPath, "MSBuild", "Current", "Bin", "MSBuild.exe");
    }

    if (!File.Exists(msBuildPath))
    {
        msBuildPath = FindPreferredFile(installationPath, "MSBuild.exe", "MSBuild", "Current");
    }

    return (
        installationPath,
        vsDevCmdPath,
        compilerPath,
        File.Exists(msBuildPath) ? msBuildPath : null);
}

static string? FindVisualStudioInstallationPath()
{
    var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    var vsWherePath = Path.Combine(programFilesX86, "Microsoft Visual Studio", "Installer", "vswhere.exe");
    if (File.Exists(vsWherePath))
    {
        var result = RunProcess(
            vsWherePath,
            "-latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath");
        var path = result.Output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(Directory.Exists);
        if (!string.IsNullOrWhiteSpace(path))
        {
            return path;
        }
    }

    var candidates = new[]
    {
        Path.Combine(programFilesX86, "Microsoft Visual Studio", "2022", "BuildTools"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio", "18", "Community")
    };

    return candidates.FirstOrDefault(Directory.Exists);
}

static string? FindPreferredFile(
    string root,
    string fileName,
    string preferredSegment,
    string secondaryPreferredSegment)
{
    try
    {
        return Directory
            .EnumerateFiles(root, fileName, SearchOption.AllDirectories)
            .OrderByDescending(path => path.Contains(preferredSegment, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(path => path.Contains(secondaryPreferredSegment, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
    }
    catch (UnauthorizedAccessException)
    {
        return null;
    }
    catch (DirectoryNotFoundException)
    {
        return null;
    }
}

static PackagingPlanInputs DetectPackagingInputs(WinUiToolingSnapshot? winUiTooling = null)
{
    var repositoryRoots = CandidateRepositoryRoots()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    var packageBuilt = repositoryRoots.Any(HasValidPackageBuildEvidence);
    var signingAvailable = repositoryRoots.Any(root =>
        File.Exists(Path.Combine(root, "artifacts", "package", "IconReplacer.Dev.pfx")) &&
        File.Exists(Path.Combine(root, "artifacts", "package", "IconReplacer.Dev.cer")) &&
        HasValidPackageBuildEvidence(root));
    var installProofCaptured = repositoryRoots.Any(root =>
        HasValidLifecycleEvidence(root, "lifecycle-install-evidence.json", expectInstalled: true) ||
        HasValidLifecycleEvidence(root, "lifecycle-evidence.json", expectInstalled: false));
    var uninstallProofCaptured = repositoryRoots.Any(root =>
        HasValidLifecycleEvidence(root, "lifecycle-evidence.json", expectInstalled: false));

    return PackagingPlanInputs.FromTooling(winUiTooling ?? DetectWinUiTooling(), DetectNativeTooling()) with
    {
        PackageIdentityBuilt = packageBuilt,
        NativeShellExtensionBuilt = NativeShellExtensionExists(),
        DevSigningAvailable = signingAvailable,
        InstallerBuilt = packageBuilt,
        InstallProofCaptured = installProofCaptured,
        UninstallProofCaptured = uninstallProofCaptured
    };
}

static bool HasValidPackageBuildEvidence(string repositoryRoot)
{
    var packageRoot = Path.Combine(repositoryRoot, "artifacts", "package");
    var evidencePath = Path.Combine(packageRoot, "build-evidence.json");
    if (!File.Exists(evidencePath))
    {
        return false;
    }

    return ReadJsonEvidence(evidencePath, root =>
    {
        if (!root.TryGetProperty("packagePath", out var packagePathProperty) ||
            !root.TryGetProperty("signatureStatus", out var signatureStatusProperty) ||
            !root.TryGetProperty("packageSha256", out var packageSha256Property) ||
            !root.TryGetProperty("packageLength", out var packageLengthProperty))
        {
            return false;
        }

        var packagePath = packagePathProperty.GetString();
        return !string.IsNullOrWhiteSpace(packagePath) &&
            File.Exists(packagePath) &&
            string.Equals(signatureStatusProperty.GetString(), "Valid", StringComparison.OrdinalIgnoreCase) &&
            packageLengthProperty.TryGetInt64(out var expectedLength) &&
            new FileInfo(packagePath).Length == expectedLength &&
            FileMatchesSha256(packagePath, packageSha256Property.GetString());
    });
}

static bool HasValidLifecycleEvidence(
    string repositoryRoot,
    string fileName,
    bool expectInstalled)
{
    var evidencePath = Path.Combine(repositoryRoot, "artifacts", "package", fileName);
    if (!File.Exists(evidencePath))
    {
        return false;
    }

    return ReadJsonEvidence(evidencePath, root =>
    {
        if (!root.TryGetProperty("keptInstalled", out var keptInstalledProperty) ||
            keptInstalledProperty.ValueKind is not JsonValueKind.True and not JsonValueKind.False ||
            keptInstalledProperty.GetBoolean() != expectInstalled ||
            !root.TryGetProperty("install", out var installProperty) ||
            installProperty.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("packagePath", out var packagePathProperty) ||
            !root.TryGetProperty("packageSha256", out var packageSha256Property) ||
            string.IsNullOrWhiteSpace(packagePathProperty.GetString()) ||
            !FileMatchesSha256(packagePathProperty.GetString()!, packageSha256Property.GetString()) ||
            !HasPreservedContextMenuEvidence(root) ||
            !HasPreservedExplorerEvidence(root))
        {
            return false;
        }

        if (expectInstalled)
        {
            return installProperty.TryGetProperty("shellManifestRegistered", out var shellRegistered) &&
                shellRegistered.ValueKind == JsonValueKind.True &&
                installProperty.TryGetProperty("classicShellManifestRegistered", out var classicRegistered) &&
                classicRegistered.ValueKind == JsonValueKind.True &&
                installProperty.TryGetProperty("applicationIconPresent", out var appIconPresent) &&
                appIconPresent.ValueKind == JsonValueKind.True &&
                installProperty.TryGetProperty("iconLibraryPreserved", out var installedLibraryPreserved) &&
                installedLibraryPreserved.ValueKind == JsonValueKind.True &&
                installProperty.TryGetProperty("restoreStatePreserved", out var installedRestoreStatePreserved) &&
                installedRestoreStatePreserved.ValueKind == JsonValueKind.True;
        }

        return root.TryGetProperty("uninstall", out var uninstallProperty) &&
            uninstallProperty.ValueKind == JsonValueKind.Object &&
            uninstallProperty.TryGetProperty("packageRemoved", out var packageRemoved) &&
            packageRemoved.ValueKind == JsonValueKind.True &&
            uninstallProperty.TryGetProperty("shellIntegrationRemovedWithPackage", out var shellRemoved) &&
            shellRemoved.ValueKind == JsonValueKind.True &&
            uninstallProperty.TryGetProperty("iconLibraryPreserved", out var libraryPreserved) &&
            libraryPreserved.ValueKind == JsonValueKind.True &&
            uninstallProperty.TryGetProperty("restoreStatePreserved", out var restoreStatePreserved) &&
            restoreStatePreserved.ValueKind == JsonValueKind.True;
    });
}

static bool FileMatchesSha256(string path, string? expectedSha256)
{
    if (string.IsNullOrWhiteSpace(expectedSha256) || !File.Exists(path))
    {
        return false;
    }

    try
    {
        using var stream = File.OpenRead(path);
        var actualSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
        return string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }
    catch (IOException)
    {
        return false;
    }
    catch (UnauthorizedAccessException)
    {
        return false;
    }
}

static bool HasPreservedContextMenuEvidence(JsonElement root)
{
    if (!TryGetContextMenuHash(root, "contextMenuBefore", out var baselineHash) ||
        !TryGetContextMenuHash(root, "contextMenuAfterFreshRemove", out var afterFreshRemoveHash) ||
        !TryGetContextMenuHash(root, "contextMenuAfterInstall", out var afterInstallHash))
    {
        return false;
    }

    if (!string.Equals(baselineHash, afterFreshRemoveHash, StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(baselineHash, afterInstallHash, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    if (!root.TryGetProperty("keptInstalled", out var keptInstalledProperty) ||
        keptInstalledProperty.ValueKind != JsonValueKind.False)
    {
        return true;
    }

    return root.TryGetProperty("uninstall", out var uninstallProperty) &&
        uninstallProperty.ValueKind == JsonValueKind.Object &&
        TryGetContextMenuHash(uninstallProperty, "contextMenuAfterUninstall", out var afterUninstallHash) &&
        string.Equals(baselineHash, afterUninstallHash, StringComparison.OrdinalIgnoreCase);
}

static bool TryGetContextMenuHash(JsonElement parent, string propertyName, out string hash)
{
    hash = string.Empty;
    return parent.TryGetProperty(propertyName, out var snapshot) &&
        snapshot.ValueKind == JsonValueKind.Object &&
        snapshot.TryGetProperty("contentSha256", out var hashProperty) &&
        !string.IsNullOrWhiteSpace(hash = hashProperty.GetString() ?? string.Empty);
}

static bool HasPreservedExplorerEvidence(JsonElement root)
{
    if (!root.TryGetProperty("explorerBefore", out var explorerBefore) ||
        !root.TryGetProperty("explorerAfter", out var explorerAfter) ||
        !TryGetStringArray(explorerBefore, "processIdentities", out var beforeIdentities) ||
        !TryGetStringArray(explorerAfter, "processIdentities", out var afterIdentities))
    {
        return false;
    }

    return beforeIdentities.SequenceEqual(afterIdentities, StringComparer.Ordinal);
}

static bool TryGetStringArray(
    JsonElement parent,
    string propertyName,
    out IReadOnlyList<string> values)
{
    values = [];
    if (!parent.TryGetProperty(propertyName, out var array) ||
        array.ValueKind != JsonValueKind.Array)
    {
        return false;
    }

    values = array.EnumerateArray()
        .Where(item => item.ValueKind == JsonValueKind.String)
        .Select(item => item.GetString() ?? string.Empty)
        .ToArray();
    return values.Count == array.GetArrayLength();
}

static bool ReadJsonEvidence(string path, Func<JsonElement, bool> predicate)
{
    try
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return predicate(document.RootElement);
    }
    catch (IOException)
    {
        return false;
    }
    catch (JsonException)
    {
        return false;
    }
}

static bool NativeShellExtensionExists()
{
    return CandidateRepositoryRoots().Any(root =>
        File.Exists(Path.Combine(root, "artifacts", "native", "x64", "Debug", "IconReplacer.ShellExtension.dll")) ||
        File.Exists(Path.Combine(root, "artifacts", "native", "x64", "Release", "IconReplacer.ShellExtension.dll")));
}

static IEnumerable<string> CandidateRepositoryRoots()
{
    yield return Directory.GetCurrentDirectory();

    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "IconReplacer.slnx")))
        {
            yield return directory.FullName;
        }

        directory = directory.Parent;
    }
}

static (int ExitCode, string Output) RunProcess(string fileName, string arguments)
{
    try
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        if (process is null)
        {
            return (-1, string.Empty);
        }

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(15000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            return (-1, output + error + "Process timed out.");
        }

        return (process.ExitCode, output + error);
    }
    catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
    {
        return (-1, ex.Message);
    }
}

static void WriteUsage()
{
    Console.WriteLine("Icon Replacer CLI");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  IconReplacer.Cli doctor");
    Console.WriteLine("  IconReplacer.Cli diagnostics");
    Console.WriteLine("  IconReplacer.Cli shell-plan");
    Console.WriteLine("  IconReplacer.Cli shell-manifest");
    Console.WriteLine("  IconReplacer.Cli shell-bridge [target]");
    Console.WriteLine("  IconReplacer.Cli package-plan");
    Console.WriteLine("  IconReplacer.Cli release-readiness [proof flags]");
    Console.WriteLine("  IconReplacer.Cli accessibility-plan");
    Console.WriteLine("  IconReplacer.Cli navigation-plan");
    Console.WriteLine("  IconReplacer.Cli app-window [activation args]");
    Console.WriteLine("  IconReplacer.Cli app-commands [--route <route-id>] [--record <record-id>] [--filter <filter>] [--icon <icon.ico>] [--collection <name>] [activation args]");
    Console.WriteLine("  IconReplacer.Cli app-command-request <command-id> [--route <route-id>] [--record <record-id>] [--filter <filter>] [--icon <icon.ico>] [activation args]");
    Console.WriteLine("  IconReplacer.Cli app-view [--route <route-id>] [--record <record-id>] [--filter <filter>] [--icon <icon.ico>] [--collection <name>] [--shell-target <target>] [--search <text>] [--category <name>] [--max <count>] [activation args]");
    Console.WriteLine("  IconReplacer.Cli change-icon-workflow [--icon <icon.ico>] change-icon --target <path> --target-kind <folder|shortcut>");
    Console.WriteLine("  IconReplacer.Cli status");
    Console.WriteLine("  IconReplacer.Cli action-request <action-id>");
    Console.WriteLine("  IconReplacer.Cli activate [change-icon --target <path> --target-kind <folder|shortcut>]");
    Console.WriteLine("  IconReplacer.Cli activate menu-apply <target> <icon-from-library.ico>");
    Console.WriteLine("  IconReplacer.Cli activate-preview <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>");
    Console.WriteLine("  IconReplacer.Cli activate-apply <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>");
    Console.WriteLine("  IconReplacer.Cli activate-menu-apply <target> <icon-from-library.ico>");
    Console.WriteLine("  IconReplacer.Cli home [all|restorable|applied|restored|stale]");
    Console.WriteLine("  IconReplacer.Cli paths");
    Console.WriteLine("  IconReplacer.Cli open-request <icon-library|imported|appdata|restore-state>");
    Console.WriteLine("  IconReplacer.Cli target <folder-or-shortcut>");
    Console.WriteLine("  IconReplacer.Cli import-picker-request [collection]");
    Console.WriteLine("  IconReplacer.Cli picker-request <folder-or-shortcut>");
    Console.WriteLine("  IconReplacer.Cli launch-request <folder-or-shortcut>");
    Console.WriteLine("  IconReplacer.Cli catalog");
    Console.WriteLine("  IconReplacer.Cli catalog-warnings");
    Console.WriteLine("  IconReplacer.Cli collections");
    Console.WriteLine("  IconReplacer.Cli collection-create <name>");
    Console.WriteLine("  IconReplacer.Cli collection-import <collection> <icon.ico> [icon2.ico ...]");
    Console.WriteLine("  IconReplacer.Cli browse [search] [--category <name>] [--max <count>]");
    Console.WriteLine("  IconReplacer.Cli details <icon.ico>");
    Console.WriteLine("  IconReplacer.Cli menu");
    Console.WriteLine("  IconReplacer.Cli menu-commands");
    Console.WriteLine("  IconReplacer.Cli menu-invoke-preview <command-id> [target]");
    Console.WriteLine("  IconReplacer.Cli menu-apply <target> <icon-from-library.ico>");
    Console.WriteLine("  IconReplacer.Cli recent [all|restorable|applied|restored|stale]");
    Console.WriteLine("  IconReplacer.Cli history [all|restorable|applied|restored|stale]");
    Console.WriteLine("  IconReplacer.Cli restore-workflow [record-id] [all|restorable|applied|restored|stale]");
    Console.WriteLine("  IconReplacer.Cli restore-preview <record-id>");
    Console.WriteLine("  IconReplacer.Cli import <icon.ico> [display-name]");
    Console.WriteLine("  IconReplacer.Cli batch-import <icon.ico> [icon2.ico ...]");
    Console.WriteLine("  IconReplacer.Cli preview-change <target> <icon.ico>");
    Console.WriteLine("  IconReplacer.Cli change <target> <icon.ico>");
    Console.WriteLine("  IconReplacer.Cli apply <target> <icon.ico>");
    Console.WriteLine("  IconReplacer.Cli apply-folder <folder> <icon.ico>");
    Console.WriteLine("  IconReplacer.Cli apply-shortcut <shortcut.lnk> <icon.ico>");
    Console.WriteLine("  IconReplacer.Cli restore <record-id>");
}

static (bool Succeeded, int ExitCode, ReleaseReadinessInputs? Inputs) ParseReleaseReadinessInputs(string[] args)
{
    var flags = args.Skip(1).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var knownFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "--build",
        "--tests",
        "--cli-proof",
        "--manual-explorer-proof",
        "--accessibility-proof",
        "--release-evidence",
        "--package-identity",
        "--native-extension",
        "--dev-signing",
        "--installer",
        "--install-proof",
        "--uninstall-proof"
    };

    if (flags.Any(flag => !knownFlags.Contains(flag)))
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli release-readiness [--build] [--tests] [--cli-proof] [--manual-explorer-proof] [--accessibility-proof] [--release-evidence] [--package-identity] [--native-extension] [--dev-signing] [--installer] [--install-proof] [--uninstall-proof]");
        return (false, 64, null);
    }

    var detectedPackagingInputs = DetectPackagingInputs();
    var packagingInputs = detectedPackagingInputs with
    {
        PackageIdentityBuilt = detectedPackagingInputs.PackageIdentityBuilt || flags.Contains("--package-identity"),
        NativeShellExtensionBuilt = detectedPackagingInputs.NativeShellExtensionBuilt || flags.Contains("--native-extension"),
        DevSigningAvailable = detectedPackagingInputs.DevSigningAvailable || flags.Contains("--dev-signing"),
        InstallerBuilt = detectedPackagingInputs.InstallerBuilt || flags.Contains("--installer"),
        InstallProofCaptured = detectedPackagingInputs.InstallProofCaptured || flags.Contains("--install-proof"),
        UninstallProofCaptured = detectedPackagingInputs.UninstallProofCaptured || flags.Contains("--uninstall-proof")
    };

    return (true, 0, new ReleaseReadinessInputs(
        packagingInputs,
        BuildPassed: flags.Contains("--build"),
        TestsPassed: flags.Contains("--tests"),
        CliProofCaptured: flags.Contains("--cli-proof"),
        ManualExplorerProofCaptured: flags.Contains("--manual-explorer-proof"),
        AccessibilityProofCaptured: flags.Contains("--accessibility-proof"),
        ReleaseEvidenceCaptured: flags.Contains("--release-evidence")));
}

static (bool Succeeded, int ExitCode, Guid? RecordId, RestoreHistoryFilter Filter) ParseRestoreWorkflowArgs(string[] args)
{
    if (args.Length > 3)
    {
        Console.Error.WriteLine("Usage: IconReplacer.Cli restore-workflow [record-id] [all|restorable|applied|restored|stale]");
        return (false, 64, null, RestoreHistoryFilter.All);
    }

    if (args.Length == 1)
    {
        return (true, 0, null, RestoreHistoryFilter.All);
    }

    if (args.Length == 2)
    {
        if (TryParseHistoryFilter(args[1], out var filter))
        {
            return (true, 0, null, filter);
        }

        if (Guid.TryParse(args[1], out var recordId))
        {
            return (true, 0, recordId, RestoreHistoryFilter.All);
        }

        Console.Error.WriteLine("Restore workflow argument must be a record GUID or one of: all, restorable, applied, restored, stale.");
        return (false, 64, null, RestoreHistoryFilter.All);
    }

    if (!Guid.TryParse(args[1], out var selectedRecordId))
    {
        Console.Error.WriteLine("Restore record id must be a GUID.");
        return (false, 64, null, RestoreHistoryFilter.All);
    }

    if (!TryParseHistoryFilter(args[2], out var selectedFilter))
    {
        Console.Error.WriteLine("History filter must be one of: all, restorable, applied, restored, stale.");
        return (false, 64, null, RestoreHistoryFilter.All);
    }

    return (true, 0, selectedRecordId, selectedFilter);
}

static bool TryParseHistoryFilter(string value, out RestoreHistoryFilter filter)
{
    filter = value.ToLowerInvariant() switch
    {
        "all" => RestoreHistoryFilter.All,
        "restorable" => RestoreHistoryFilter.Restorable,
        "applied" => RestoreHistoryFilter.Applied,
        "restored" => RestoreHistoryFilter.Restored,
        "stale" => RestoreHistoryFilter.Stale,
        _ => RestoreHistoryFilter.All
    };

    return value.Equals("all", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("restorable", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("applied", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("restored", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("stale", StringComparison.OrdinalIgnoreCase);
}

static bool TryParseLocationKind(string value, out AppLocationKind kind)
{
    kind = value.ToLowerInvariant() switch
    {
        "icon-library" or "library" or "icons" => AppLocationKind.IconLibrary,
        "imported" or "imported-icons" => AppLocationKind.ImportedIcons,
        "appdata" or "app-data" => AppLocationKind.AppData,
        "restore-state" or "state" => AppLocationKind.RestoreState,
        _ => AppLocationKind.IconLibrary
    };

    return value.Equals("icon-library", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("library", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("icons", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("imported", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("imported-icons", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("appdata", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("app-data", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("restore-state", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("state", StringComparison.OrdinalIgnoreCase);
}

static string? GetOptionalValue(string[] args, string optionName)
{
    for (var index = 0; index < args.Length - 1; index++)
    {
        if (string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
        {
            return args[index + 1];
        }
    }

    return null;
}

static string[] RemoveOption(string[] args, string optionName)
{
    var filtered = new List<string>();
    for (var index = 0; index < args.Length; index++)
    {
        if (string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
        {
            index++;
            continue;
        }

        filtered.Add(args[index]);
    }

    return filtered.ToArray();
}

static string[] RemoveOptions(string[] args, params string[] optionNames)
{
    var filtered = args;
    foreach (var optionName in optionNames)
    {
        filtered = RemoveOption(filtered, optionName);
    }

    return filtered;
}

static int WriteApplyResult(OperationResult<IconApplyResult> result)
{
    if (!result.Succeeded || result.Value is null)
    {
        return WriteError(result.Error);
    }

    var feedback = new AppOperationFeedbackService().FromApply(result.Value);
    Console.WriteLine(feedback.Title);
    Console.WriteLine($"Target: {result.Value.RestoreRecord.Target.FullPath}");
    Console.WriteLine($"Imported icon: {result.Value.ImportedIcon.FullPath}");

    if (!string.IsNullOrWhiteSpace(result.Value.DesktopIniPath))
    {
        Console.WriteLine($"desktop.ini: {result.Value.DesktopIniPath}");
    }

    if (result.Value.Shortcut is not null)
    {
        Console.WriteLine($"Current icon: {result.Value.Shortcut.IconPath},{result.Value.Shortcut.IconIndex}");
    }

    Console.WriteLine($"Restore record: {result.Value.RestoreRecord.Id}");
    return 0;
}

static int WriteRestoreResult(OperationResult<IconRestoreResult> result)
{
    if (!result.Succeeded || result.Value is null)
    {
        return WriteError(result.Error);
    }

    var feedback = new AppOperationFeedbackService().FromRestore(result.Value);
    Console.WriteLine(feedback.Title);
    Console.WriteLine($"Target: {result.Value.RestoreRecord.Target.FullPath}");

    if (!string.IsNullOrWhiteSpace(result.Value.DesktopIniPath))
    {
        Console.WriteLine($"desktop.ini: {result.Value.DesktopIniPath}");
    }

    if (result.Value.Shortcut is not null)
    {
        Console.WriteLine($"Current icon: {result.Value.Shortcut.IconPath},{result.Value.Shortcut.IconIndex}");
    }

    Console.WriteLine($"Restore record: {result.Value.RestoreRecord.Id}");
    return 0;
}

static string FormatRecord(RestoreRecordSummary record)
{
    var createdLocal = record.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    var health = FormatRecordHealth(record);
    return $"{record.Id} | {record.Status} | {record.TargetKind} | {createdLocal} | {health} | {record.TargetPath}";
}

static string FormatRecentChange(RecentChangeItem item)
{
    var createdLocal = item.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    var warning = string.IsNullOrWhiteSpace(item.WarningText)
        ? string.Empty
        : $" | {item.WarningText}";
    return $"{item.Id} | {item.Status} | {item.TargetKind} | {createdLocal} | {item.RestoreActionState} | {item.HealthText}{warning} | {item.TargetPath}";
}

static string FormatShellBridgeCommand(ShellExtensionBridgeCommand command)
{
    var state = command.CanInvoke ? "enabled" : "disabled";
    var category = string.IsNullOrWhiteSpace(command.CategoryName)
        ? string.Empty
        : $" | {command.CategoryName}";
    var icon = string.IsNullOrWhiteSpace(command.IconPath)
        ? string.Empty
        : $" | {command.IconPath}";
    var arguments = string.IsNullOrWhiteSpace(command.DisplayArguments)
        ? string.Empty
        : $" | {command.DisplayArguments}";
    var reason = command.CanInvoke
        ? string.Empty
        : $" | {command.Error.Code}: {command.Error.Message}";

    return $"{state} | {command.Kind} | {command.Id} | {command.Label}{category}{icon}{arguments}{reason}";
}

static string FormatBatchImportItem(IconBatchImportItem item)
{
    if (item.Succeeded && item.ImportedIcon is not null)
    {
        return $"{item.Status} | {item.SourcePath} -> {item.ImportedIcon.FullPath}";
    }

    return $"{item.Status} | {item.SourcePath} | {item.Error.Code}: {item.Error.Message}";
}

static string FormatImageDetail(IconImageDetail image)
{
    var useful = image.HasUsefulSize ? "useful" : "nonstandard";
    return $"{image.Width}x{image.Height}, {image.BitCount} bpp, {image.BytesInResource} bytes, {useful}";
}

static string FormatRecordHealth(RestoreRecordSummary record)
{
    var issues = new List<string>();

    if (!record.TargetExists)
    {
        issues.Add("target-missing");
    }

    if (!record.AppliedIconExists)
    {
        issues.Add("applied-icon-missing");
    }

    if (issues.Count > 0)
    {
        return string.Join(", ", issues);
    }

    return record.CanRestore ? "restorable" : "not-restorable";
}
