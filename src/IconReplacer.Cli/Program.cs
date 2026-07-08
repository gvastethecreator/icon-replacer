using System.Diagnostics;
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
        "status" or "setup" => RunStatus(),
        "activate" => RunActivate(args),
        "activate-preview" => RunActivatePreview(args),
        "activate-apply" => RunActivateApply(args),
        "home" or "app" => RunHome(args),
        "paths" or "locations" => RunPaths(),
        "target" or "selection" => RunTarget(args),
        "picker-request" or "picker" => RunPickerRequest(args),
        "launch-request" or "launch" => RunLaunchRequest(args),
        "catalog" => RunCatalog(),
        "browse" or "icons" => RunBrowse(args),
        "details" or "icon-details" => RunDetails(args),
        "menu" => RunMenu(),
        "recent" or "changes" => RunRecent(args),
        "history" or "records" => RunHistory(args),
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
    var diagnostics = new AppDiagnosticsService().GetDiagnosticsFromEnvironment(DetectWinUiTooling());
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

static int RunStatus()
{
    var snapshot = new SetupReadinessService().GetSnapshotFromEnvironment();
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

    var snapshot = new AppHomeService().GetSnapshotFromEnvironment(filter);
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

static int RunMenu()
{
    var snapshot = new IconMenuService().BuildSnapshotFromEnvironment();
    if (!snapshot.Succeeded || snapshot.Value is null)
    {
        return WriteError(snapshot.Error);
    }

    Console.WriteLine($"Icon menu: {snapshot.Value.IconLibraryRoot}");
    Console.WriteLine($"Command: {snapshot.Value.ChangeIconCommandLabel}");
    Console.WriteLine($"Icons: {snapshot.Value.VisibleIconCount}/{snapshot.Value.TotalIconCount} visible");
    Console.WriteLine($"Categories: {snapshot.Value.Categories.Count}");

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
    var templatesAvailable = templates.ExitCode == 0 &&
        templates.Output.Contains("WinUI", StringComparison.OrdinalIgnoreCase);
    var winAppAvailable = winapp.ExitCode == 0;

    return new WinUiToolingSnapshot(
        IsChecked: true,
        templatesAvailable,
        winAppAvailable,
        templatesAvailable
            ? "WinUI templates are available."
            : "WinUI templates were not found; run /winui-setup before scaffolding WinUI.",
        winAppAvailable
            ? "winapp CLI is available."
            : "winapp CLI was not found; run /winui-setup before scaffolding or running WinUI.");
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
    Console.WriteLine("  IconReplacer.Cli status");
    Console.WriteLine("  IconReplacer.Cli activate [change-icon --target <path> --target-kind <folder|shortcut>]");
    Console.WriteLine("  IconReplacer.Cli activate-preview <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>");
    Console.WriteLine("  IconReplacer.Cli activate-apply <icon.ico> change-icon --target <path> --target-kind <folder|shortcut>");
    Console.WriteLine("  IconReplacer.Cli home [all|restorable|applied|restored|stale]");
    Console.WriteLine("  IconReplacer.Cli paths");
    Console.WriteLine("  IconReplacer.Cli target <folder-or-shortcut>");
    Console.WriteLine("  IconReplacer.Cli picker-request <folder-or-shortcut>");
    Console.WriteLine("  IconReplacer.Cli launch-request <folder-or-shortcut>");
    Console.WriteLine("  IconReplacer.Cli catalog");
    Console.WriteLine("  IconReplacer.Cli browse [search] [--category <name>] [--max <count>]");
    Console.WriteLine("  IconReplacer.Cli details <icon.ico>");
    Console.WriteLine("  IconReplacer.Cli menu");
    Console.WriteLine("  IconReplacer.Cli recent [all|restorable|applied|restored|stale]");
    Console.WriteLine("  IconReplacer.Cli history [all|restorable|applied|restored|stale]");
    Console.WriteLine("  IconReplacer.Cli import <icon.ico> [display-name]");
    Console.WriteLine("  IconReplacer.Cli batch-import <icon.ico> [icon2.ico ...]");
    Console.WriteLine("  IconReplacer.Cli preview-change <target> <icon.ico>");
    Console.WriteLine("  IconReplacer.Cli change <target> <icon.ico>");
    Console.WriteLine("  IconReplacer.Cli apply <target> <icon.ico>");
    Console.WriteLine("  IconReplacer.Cli apply-folder <folder> <icon.ico>");
    Console.WriteLine("  IconReplacer.Cli apply-shortcut <shortcut.lnk> <icon.ico>");
    Console.WriteLine("  IconReplacer.Cli restore <record-id>");
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

static int WriteApplyResult(OperationResult<IconApplyResult> result)
{
    if (!result.Succeeded || result.Value is null)
    {
        return WriteError(result.Error);
    }

    Console.WriteLine(result.Value.TargetKind == TargetKind.Folder
        ? "Folder icon changed."
        : "Shortcut icon changed.");
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

    Console.WriteLine(result.Value.TargetKind == TargetKind.Folder
        ? "Folder icon restored."
        : "Shortcut icon restored.");
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
