using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconReplacer.AppModel;
using IconReplacer.Core;
using Microsoft.UI.Xaml.Controls;

namespace IconReplacer.App.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    private readonly AppRouteViewService _routeViewService = new();
    private readonly AppCommandRequestService _commandRequestService = new();
    private readonly AppLocationService _locationService = new();
    private readonly IconImportPickerRequestService _importPickerRequestService = new();
    private readonly IconLibraryService _iconLibraryService = new();
    private readonly IconRestoreService _iconRestoreService = new();
    private readonly AppOperationFeedbackService _feedbackService = new();
    private readonly AppActivationService _activationService = new();
    private readonly ActivatedIconChangeService _activatedIconChangeService = new();
    private readonly ActivatedMenuApplyService _activatedMenuApplyService = new();
    private IReadOnlyList<string> _activationArguments = [];

    [ObservableProperty]
    public partial string PageTitle { get; set; } = "Icon Replacer";

    [ObservableProperty]
    public partial string PageSubtitle { get; set; } = "Loading library state...";

    [ObservableProperty]
    public partial string StatusTitle { get; set; } = "Loading";

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Reading the Icon Library.";

    [ObservableProperty]
    public partial InfoBarSeverity StatusSeverity { get; set; } = InfoBarSeverity.Informational;

    [ObservableProperty]
    public partial bool IsStatusOpen { get; set; } = true;

    [ObservableProperty]
    public partial string PrimaryListTitle { get; set; } = "Recent changes";

    [ObservableProperty]
    public partial string SecondaryListTitle { get; set; } = "Readiness";

    public ObservableCollection<MetricTileViewModel> Metrics { get; } = [];

    public ObservableCollection<ContentRowViewModel> PrimaryRows { get; } = [];

    public ObservableCollection<ContentRowViewModel> SecondaryRows { get; } = [];

    public string SelectedRouteId { get; private set; } = AppNavigationRouteIds.Home;

    public MainPageViewModel()
    {
        LoadRoute(AppNavigationRouteIds.Home);
    }

    public async Task InitializeAsync(IReadOnlyList<string> activationArguments)
    {
        _activationArguments = activationArguments.ToArray();
        if (_activationArguments.Count == 0)
        {
            return;
        }

        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            ShowError(paths.Error);
            return;
        }

        var activation = _activationService.Activate(_activationArguments, paths.Value);
        if (!activation.Succeeded || activation.Value is null)
        {
            ShowError(activation.Error);
            return;
        }

        switch (activation.Value.Kind)
        {
            case AppActivationKind.ChangeIcon:
                await HandleChangeIconActivationAsync(paths.Value);
                break;
            case AppActivationKind.MenuApply:
                HandleMenuApplyActivation(paths.Value);
                break;
            default:
                LoadRoute(AppNavigationRouteIds.Home);
                break;
        }
    }

    public void SelectRoute(string routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            return;
        }

        SelectedRouteId = routeId;
        LoadRoute(routeId);
    }

    public void ShowUnexpectedException(Exception ex)
    {
        ShowFeedback(_feedbackService.FromError(new IconReplacerError(
            ErrorCode.Unknown,
            "The activation workflow could not be completed.",
            ex.Message)));
    }

    [RelayCommand]
    private void Refresh()
    {
        LoadRoute(SelectedRouteId);
    }

    [RelayCommand]
    private async Task ImportIconsAsync()
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            ShowError(paths.Error);
            return;
        }

        var request = _importPickerRequestService.CreateRequest(paths.Value);
        if (!request.Succeeded || request.Value is null)
        {
            ShowError(request.Error);
            return;
        }

        if (!request.Value.CanOpenPicker)
        {
            ShowError(request.Value.Error);
            return;
        }

        IReadOnlyList<string> sourcePaths;
        try
        {
            sourcePaths = await PickMultipleIconPathsAsync(
                request.Value.FileExtensions,
                request.Value.InitialDirectory,
                "Import icons");
        }
        catch (Exception ex)
        {
            ShowFeedback(_feedbackService.FromError(new IconReplacerError(
                ErrorCode.Unknown,
                "The icon picker could not be opened.",
                ex.Message)));
            return;
        }

        if (sourcePaths.Count == 0)
        {
            SelectRoute(AppNavigationRouteIds.ImportIcons);
            ShowFeedback(new AppOperationFeedback(
                AppOperationFeedbackSeverity.Info,
                "Import cancelled.",
                "No icons were selected."));
            return;
        }

        var import = _iconLibraryService.ImportIcons(sourcePaths, paths.Value);
        if (!import.Succeeded || import.Value is null)
        {
            ShowFeedback(_feedbackService.FromError(import.Error));
            return;
        }

        LoadRoute(AppNavigationRouteIds.IconBrowser);
        ShowFeedback(_feedbackService.FromBatchImport(import.Value));
    }

    private async Task HandleChangeIconActivationAsync(IconLibraryPaths paths)
    {
        SelectedRouteId = AppNavigationRouteIds.ChangeIcon;
        LoadRoute(AppNavigationRouteIds.ChangeIcon);

        var activation = _activationService.Activate(_activationArguments, paths);
        var pickerRequest = activation.Value?.LaunchRequest?.PickerRequest;
        if (!activation.Succeeded || activation.Value is null || pickerRequest is null)
        {
            ShowFeedback(_feedbackService.FromError(activation.Error));
            return;
        }

        if (!pickerRequest.CanOpenPicker)
        {
            ShowFeedback(_feedbackService.FromError(pickerRequest.Error));
            return;
        }

        string? selectedIconPath;
        try
        {
            await PreparePickerOwnerAsync();
            selectedIconPath = await PickSingleIconPathAsync(
                pickerRequest.FileExtensions,
                pickerRequest.InitialDirectory,
                "Change icon");
        }
        catch (Exception ex)
        {
            ShowFeedback(_feedbackService.FromError(new IconReplacerError(
                ErrorCode.Unknown,
                "The icon picker could not be opened.",
                ex.Message)));
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedIconPath))
        {
            ShowFeedback(new AppOperationFeedback(
                AppOperationFeedbackSeverity.Info,
                "Change icon cancelled.",
                "No icon was selected."));
            return;
        }

        var apply = _activatedIconChangeService.ApplySelectedIcon(_activationArguments, selectedIconPath, paths);
        if (!apply.Succeeded || apply.Value is null)
        {
            ShowFeedback(_feedbackService.FromError(apply.Error));
            return;
        }

        LoadRoute(AppNavigationRouteIds.History);
        ShowFeedback(_feedbackService.FromApply(apply.Value.ChangeResult.ApplyResult));
    }

    private void HandleMenuApplyActivation(IconLibraryPaths paths)
    {
        var apply = _activatedMenuApplyService.ApplyActivation(_activationArguments, paths);
        if (!apply.Succeeded || apply.Value is null)
        {
            ShowFeedback(_feedbackService.FromError(apply.Error));
            return;
        }

        LoadRoute(AppNavigationRouteIds.History);
        ShowFeedback(_feedbackService.FromApply(apply.Value.MenuApplyResult.ChangeResult.ApplyResult));
    }

    [RelayCommand]
    private void OpenIconLibrary()
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            ShowError(paths.Error);
            return;
        }

        var request = _locationService.CreateOpenRequest(AppLocationKind.IconLibrary, paths.Value);
        if (!request.Succeeded || request.Value is null)
        {
            ShowError(request.Error);
            return;
        }

        if (!request.Value.CanOpen)
        {
            ShowError(request.Value.Error);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = request.Value.TargetPath,
                UseShellExecute = true,
                Verb = request.Value.ShellVerb
            });
        }
        catch (Exception ex)
        {
            ShowFeedback(_feedbackService.FromError(new IconReplacerError(
                ErrorCode.Unknown,
                "The Icon Library could not be opened.",
                ex.Message)));
            return;
        }

        StatusTitle = "Icon Library opened";
        StatusMessage = request.Value.TargetPath;
        StatusSeverity = InfoBarSeverity.Success;
        IsStatusOpen = true;
    }

    private Task RestoreRecordAsync(Guid recordId)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            ShowError(paths.Error);
            return Task.CompletedTask;
        }

        var restore = _iconRestoreService.Restore(recordId, paths.Value);
        if (!restore.Succeeded || restore.Value is null)
        {
            ShowFeedback(_feedbackService.FromError(restore.Error));
            return Task.CompletedTask;
        }

        LoadRoute(AppNavigationRouteIds.History);
        ShowFeedback(_feedbackService.FromRestore(restore.Value));
        return Task.CompletedTask;
    }

    private void LoadRoute(string routeId)
    {
        var tooling = ToolingProbe.GetPackagingInputs();
        var browserOptions = routeId == AppNavigationRouteIds.IconBrowser
            ? new IconBrowserOptions(MaxItems: 80)
            : null;

        var view = _routeViewService.GetViewFromEnvironment(
            _activationArguments,
            routeId,
            tooling.WinUiTooling,
            browserOptions: browserOptions,
            packagingInputs: tooling);
        if (!view.Succeeded || view.Value is null)
        {
            ShowError(view.Error);
            return;
        }

        ApplyView(view.Value);
    }

    private void ApplyView(AppRouteViewSnapshot view)
    {
        PageTitle = view.Route.Title;
        PageSubtitle = view.Summary;
        StatusTitle = view.IsContentReady ? "Ready" : "Needs input";
        StatusMessage = view.IsContentReady ? view.Route.Purpose : view.Error.Message;
        StatusSeverity = view.IsContentReady ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        IsStatusOpen = true;

        Metrics.Clear();
        PrimaryRows.Clear();
        SecondaryRows.Clear();

        switch (view.ContentKind)
        {
            case AppRouteContentKind.Home:
                FillHome(view);
                break;
            case AppRouteContentKind.IconBrowser:
                FillBrowser(view);
                break;
            case AppRouteContentKind.History:
                FillHistory(view);
                break;
            case AppRouteContentKind.Diagnostics:
                FillDiagnostics(view);
                break;
            case AppRouteContentKind.PackagePlan:
                FillPackagePlan(view);
                break;
            case AppRouteContentKind.ImportIcons:
                FillImport(view);
                break;
            case AppRouteContentKind.ChangeIconWorkflow:
                FillChangeIconWorkflow(view);
                break;
            default:
                FillFallback(view);
                break;
        }
    }

    private void FillHome(AppRouteViewSnapshot view)
    {
        var home = view.Home!;
        PrimaryListTitle = "Recent changes";
        SecondaryListTitle = "Setup";

        AddMetric("Icons", home.Dashboard.IconCount, $"{home.Dashboard.CategoryCount} categories");
        AddMetric("History", home.History.TotalCount, $"{home.History.RestorableCount} restorable");
        AddMetric("Menu", home.Menu.VisibleIconCount, $"{home.Menu.Categories.Count} groups");
        AddMetric("Warnings", home.Dashboard.CatalogWarningCount, "catalog");

        foreach (var record in home.History.Records.Take(12))
        {
            PrimaryRows.Add(CreateRestoreRow(
                record,
                $"{record.TargetKind} - {record.CreatedAt.LocalDateTime:g}"));
        }

        foreach (var action in home.Setup.Actions)
        {
            SecondaryRows.Add(new ContentRowViewModel(
                action.Title,
                action.Detail,
                action.Severity.ToString()));
        }
    }

    private void FillBrowser(AppRouteViewSnapshot view)
    {
        var browser = view.Browser!;
        PrimaryListTitle = "Icons";
        SecondaryListTitle = "Categories";

        AddMetric("Visible", browser.VisibleIconCount, $"{browser.MatchedIconCount} matched");
        AddMetric("Total", browser.TotalIconCount, "icons");
        AddMetric("Categories", browser.Categories.Count, "folders");
        AddMetric("Warnings", browser.Warnings.Count, "catalog");

        foreach (var item in browser.Items)
        {
            PrimaryRows.Add(new ContentRowViewModel(
                item.DisplayName,
                item.FullPath,
                item.CategoryName));
        }

        foreach (var category in browser.Categories.Take(18))
        {
            SecondaryRows.Add(new ContentRowViewModel(
                category.Name,
                $"{category.IconCount} icons",
                "Library"));
        }
    }

    private void FillHistory(AppRouteViewSnapshot view)
    {
        var history = view.History!;
        PrimaryListTitle = "Restore history";
        SecondaryListTitle = "State";

        AddMetric("Records", history.TotalCount, history.Filter.ToString());
        AddMetric("Shown", history.Records.Count, "current filter");
        AddMetric("Restorable", history.RestorableCount, "safe");
        AddMetric("Stale", history.StaleCount, "needs review");

        foreach (var record in history.Records)
        {
            PrimaryRows.Add(CreateRestoreRow(record, record.AppliedIconPath));
        }

        SecondaryRows.Add(new ContentRowViewModel("Restore state", history.RestoreStateFile, "JSON"));
        SecondaryRows.Add(new ContentRowViewModel("Filter", history.Filter.ToString(), "Active"));
    }

    private void FillDiagnostics(AppRouteViewSnapshot view)
    {
        var diagnostics = view.Diagnostics!;
        PrimaryListTitle = "Checks";
        SecondaryListTitle = "Locations";

        AddMetric("Blockers", diagnostics.BlockingCount, "diagnostics");
        AddMetric("Warnings", diagnostics.WarningCount, "diagnostics");
        AddMetric("Icons", diagnostics.Dashboard.IconCount, "library");
        AddMetric("Records", diagnostics.Dashboard.RestoreRecordCount, "history");

        foreach (var check in diagnostics.Checks)
        {
            PrimaryRows.Add(new ContentRowViewModel(
                check.Title,
                check.Detail,
                check.Status.ToString()));
        }

        foreach (var location in diagnostics.Locations)
        {
            SecondaryRows.Add(new ContentRowViewModel(
                location.Label,
                location.FullPath,
                location.Exists ? "Found" : "Missing"));
        }
    }

    private void FillPackagePlan(AppRouteViewSnapshot view)
    {
        var plan = view.PackagePlan!;
        PrimaryListTitle = "Package gates";
        SecondaryListTitle = "Manifest";

        AddMetric("Blockers", plan.BlockingCount, "package");
        AddMetric("Warnings", plan.WarningCount, "proof");
        AddMetric("Install", plan.InstallMode, "mode");
        AddMetric("Signing", plan.RequiresDevSigning ? "Required" : "Ready", "local");

        foreach (var item in plan.Items)
        {
            PrimaryRows.Add(new ContentRowViewModel(
                item.Title,
                item.Detail,
                item.Status.ToString()));
        }

        SecondaryRows.Add(new ContentRowViewModel("Package", plan.PackageName, "Identity"));
        SecondaryRows.Add(new ContentRowViewModel("Application", plan.ManifestContract.ApplicationId, "App ID"));
        SecondaryRows.Add(new ContentRowViewModel("CLSID", plan.ManifestContract.ExplorerCommandClsid, "COM"));
        SecondaryRows.Add(new ContentRowViewModel("DLL", plan.ManifestContract.ShellExtensionDllPath, "Native"));
    }

    private void FillImport(AppRouteViewSnapshot view)
    {
        var request = view.ImportPickerRequest!;
        PrimaryListTitle = "Import target";
        SecondaryListTitle = "Picker";

        AddMetric("Destination", request.DestinationCollectionName, "collection");
        AddMetric("Files", request.AllowMultiple ? "Multiple" : "Single", "selection");
        AddMetric("Type", string.Join(", ", request.FileExtensions), "filter");
        AddMetric("Ready", request.CanOpenPicker ? "Yes" : "No", "picker");

        PrimaryRows.Add(new ContentRowViewModel(
            "Destination",
            request.DestinationDirectory,
            request.DestinationCollectionName,
            "Choose icons",
            ImportIconsCommand,
            request.CanOpenPicker));
        PrimaryRows.Add(new ContentRowViewModel("Initial folder", request.InitialDirectory, "Icon Library"));

        foreach (var command in view.Commands.Commands)
        {
            SecondaryRows.Add(new ContentRowViewModel(
                command.Label,
                command.Detail,
                command.IsEnabled ? "Enabled" : "Disabled"));
        }
    }

    private void FillChangeIconWorkflow(AppRouteViewSnapshot view)
    {
        var workflow = view.ChangeIconWorkflow!;
        PrimaryListTitle = "Change icon";
        SecondaryListTitle = "Target";

        AddMetric("Step", workflow.Step.ToString(), "workflow");
        AddMetric("Picker", workflow.CanOpenPicker ? "Ready" : "Blocked", "icon");
        AddMetric("Preview", workflow.CanPreview ? "Ready" : "Waiting", "selected icon");
        AddMetric("Apply", workflow.CanApply ? "Ready" : "Waiting", "target");

        PrimaryRows.Add(new ContentRowViewModel(
            "Activation",
            string.Join(" ", _activationArguments.Select(QuoteForDisplay)),
            workflow.Activation.Kind.ToString()));

        if (workflow.LaunchRequest is not null)
        {
            SecondaryRows.Add(new ContentRowViewModel(
                "Target",
                workflow.LaunchRequest.RequestedTargetPath,
                workflow.LaunchRequest.Selection.Status.ToString()));
            SecondaryRows.Add(new ContentRowViewModel(
                "Picker",
                workflow.LaunchRequest.PickerRequest.InitialDirectory,
                workflow.CanOpenPicker ? "Ready" : "Blocked"));
        }

        if (workflow.Preview?.Preview.IconDetails is not null)
        {
            PrimaryRows.Add(new ContentRowViewModel(
                workflow.Preview.Preview.IconDetails.DisplayName,
                workflow.SelectedIconPath ?? string.Empty,
                workflow.CanApply ? "Ready" : "Blocked"));
        }

        if (workflow.Error.Code != ErrorCode.None)
        {
            PrimaryRows.Add(new ContentRowViewModel(
                workflow.Error.Message,
                workflow.Error.Detail ?? workflow.Error.Code.ToString(),
                "Blocked"));
        }
    }

    private void FillFallback(AppRouteViewSnapshot view)
    {
        PrimaryListTitle = "Route";
        SecondaryListTitle = "Commands";
        AddMetric("Commands", view.Commands.CommandCount, $"{view.Commands.EnabledCount} enabled");
        AddMetric("Ready", view.IsContentReady ? "Yes" : "No", view.ContentKind.ToString());

        PrimaryRows.Add(new ContentRowViewModel(view.Route.Title, view.Route.Purpose, view.ContentKind.ToString()));
        foreach (var command in view.Commands.Commands)
        {
            SecondaryRows.Add(new ContentRowViewModel(command.Label, command.Detail, command.IsEnabled ? "Enabled" : "Disabled"));
        }
    }

    private void ShowError(IconReplacerError error)
    {
        PageTitle = "Icon Replacer";
        PageSubtitle = error.Detail ?? error.Message;
        StatusTitle = error.Message;
        StatusMessage = error.Detail ?? error.Code.ToString();
        StatusSeverity = InfoBarSeverity.Error;
        IsStatusOpen = true;
    }

    private void ShowFeedback(AppOperationFeedback feedback)
    {
        StatusTitle = feedback.Title;
        StatusMessage = feedback.Detail;
        StatusSeverity = feedback.Severity switch
        {
            AppOperationFeedbackSeverity.Success => InfoBarSeverity.Success,
            AppOperationFeedbackSeverity.Warning => InfoBarSeverity.Warning,
            AppOperationFeedbackSeverity.Error => InfoBarSeverity.Error,
            _ => InfoBarSeverity.Informational
        };
        IsStatusOpen = true;
    }

    private ContentRowViewModel CreateRestoreRow(RestoreRecordSummary record, string detail)
    {
        return new ContentRowViewModel(
            Shorten(record.TargetPath),
            detail,
            record.CanRestore ? "Restorable" : record.Status.ToString(),
            "Restore",
            record.CanRestore ? new AsyncRelayCommand(() => RestoreRecordAsync(record.Id)) : null,
            record.CanRestore);
    }

    private void AddMetric(string label, int value, string detail)
    {
        AddMetric(label, value.ToString("N0"), detail);
    }

    private void AddMetric(string label, string value, string detail)
    {
        Metrics.Add(new MetricTileViewModel(label, value, detail));
    }

    private static string Shorten(string value)
    {
        return value.Length <= 84 ? value : "..." + value[^81..];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<IReadOnlyList<string>> PickMultipleIconPathsAsync(
        IReadOnlyList<string> fileExtensions,
        string initialDirectory,
        string commitButtonText)
    {
        return Task.FromResult(NativeIconFileDialog.PickFiles(
            App.WindowHandle,
            fileExtensions,
            initialDirectory,
            commitButtonText,
            allowMultiple: true));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Task<string?> PickSingleIconPathAsync(
        IReadOnlyList<string> fileExtensions,
        string initialDirectory,
        string commitButtonText)
    {
        var paths = NativeIconFileDialog.PickFiles(
            App.WindowHandle,
            fileExtensions,
            initialDirectory,
            commitButtonText,
            allowMultiple: false);
        return Task.FromResult(paths.FirstOrDefault());
    }

    private static string QuoteForDisplay(string argument)
    {
        return argument.Any(char.IsWhiteSpace)
            ? $"\"{argument}\""
            : argument;
    }

    private static async Task PreparePickerOwnerAsync()
    {
        App.Window.Activate();
        await Task.Delay(500);
    }
}
