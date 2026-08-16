using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconReplacer.AppModel;
using IconReplacer.Core;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;

namespace IconReplacer.App.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    public const string LibrarySection = "library";
    public const string RecentSection = "recent";
    public const string SettingsSection = "settings";
    public const string AboutSection = "about";
    public const string AllIconsCategoryId = "__all__";

    private readonly IconCatalogService _catalogService = new();
    private readonly RestoreHistoryService _historyService = new();
    private readonly IconLibraryService _iconLibraryService = new();
    private readonly IconImportPickerRequestService _importPickerRequestService = new();
    private readonly IconRestoreService _iconRestoreService = new();
    private readonly AppLocationService _locationService = new();
    private readonly AppOperationFeedbackService _feedbackService = new();
    private readonly GitHubReleaseUpdateService _updateService = new();
    private readonly ThemePreferenceStore _themeStore = new();
    private readonly Version _currentVersion = ResolveCurrentVersion();

    private IconLibraryPaths? _paths;
    private IReadOnlyList<IconTileViewModel> _allIcons = [];
    private bool _isRefreshing;

    public event EventHandler? CatalogChanged;

    public GalleryLayoutMetrics GalleryLayout { get; } = new();

    [ObservableProperty]
    public partial string SelectedSection { get; set; } = LibrarySection;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IconCategoryViewModel? SelectedCategory { get; set; }

    [ObservableProperty]
    public partial double IconPreviewSize { get; set; } = 64;

    [ObservableProperty]
    public partial bool IsBusy { get; set; } = true;

    [ObservableProperty]
    public partial string LibrarySummary { get; set; } = "Loading the Icon Library...";

    [ObservableProperty]
    public partial string VisibleSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EmptyStateMessage { get; set; } = "No icons match this view.";

    [ObservableProperty]
    public partial string LibraryPath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string LastUpdatedText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<IconCategoryViewModel> Categories { get; set; } = [];

    [ObservableProperty]
    public partial int CollectionCount { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<IconTileViewModel> VisibleIcons { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<RecentChangeItemViewModel> RecentChanges { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<RecentChangeItemViewModel> RecentPreview { get; set; } = [];

    [ObservableProperty]
    public partial AppThemePreference ThemePreference { get; set; }

    [ObservableProperty]
    public partial string StatusTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial InfoBarSeverity StatusSeverity { get; set; } = InfoBarSeverity.Informational;

    [ObservableProperty]
    public partial bool IsStatusOpen { get; set; }

    [ObservableProperty]
    public partial bool IsCheckingForUpdates { get; set; }

    [ObservableProperty]
    public partial string UpdateStatusTitle { get; set; } = "Not checked yet";

    [ObservableProperty]
    public partial string UpdateStatusMessage { get; set; } =
        "Open About to check GitHub Releases for a newer version.";

    [ObservableProperty]
    public partial string UpdateActionText { get; set; } = "View release";

    [ObservableProperty]
    public partial Uri? LatestReleaseUri { get; set; }

    [ObservableProperty]
    public partial bool IsUpdateLinkVisible { get; set; }

    public MainPageViewModel()
    {
        ThemePreference = _themeStore.Load();
    }

    public bool IsLibraryVisible => SelectedSection == LibrarySection;

    public bool IsRecentVisible => SelectedSection == RecentSection;

    public bool IsSettingsVisible => SelectedSection == SettingsSection;

    public bool IsAboutVisible => SelectedSection == AboutSection;

    public string AppVersionText =>
        $"Version {GitHubReleaseUpdateService.FormatVersion(_currentVersion)}";

    public Uri ProjectUri => GitHubReleaseUpdateService.ProjectUri;

    public Uri ReleasesUri => GitHubReleaseUpdateService.ReleasesUri;

    public bool HasCheckedForUpdates { get; private set; }

    public bool HasVisibleIcons => VisibleIcons.Count > 0;

    public bool HasRecentChanges => RecentChanges.Count > 0;

    public long LastRefreshMilliseconds { get; private set; }

    public async Task InitializeAsync()
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            ShowError(paths.Error);
            IsBusy = false;
            return;
        }

        _paths = paths.Value;
        LibraryPath = _paths.LibraryRoot;

        var ensure = await Task.Run(() => _iconLibraryService.EnsureLibrary(_paths));
        if (!ensure.Succeeded)
        {
            ShowError(ensure.Error);
            IsBusy = false;
            return;
        }

        await RefreshCoreAsync(clearStatus: true);
    }

    public void SelectSection(string section)
    {
        if (section is LibrarySection or RecentSection or SettingsSection or AboutSection)
        {
            SelectedSection = section;
        }
    }

    public void SetThemePreference(AppThemePreference preference)
    {
        ThemePreference = preference;
        _themeStore.Save(preference);
    }

    public void SetIconCellLayout(double width, double previewSize, double height)
    {
        GalleryLayout.Set(width, previewSize, height);
    }

    public void ShowUnexpectedException(Exception exception)
    {
        ShowError(new IconReplacerError(
            ErrorCode.Unknown,
            "Icon Replacer encountered an unexpected error.",
            exception.Message));
    }

    [RelayCommand(CanExecute = nameof(CanRunLibraryOperation))]
    private async Task RefreshAsync()
    {
        await RefreshCoreAsync(clearStatus: true);
    }

    [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
    private async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdates = true;
        IsUpdateLinkVisible = false;
        LatestReleaseUri = null;
        UpdateStatusTitle = "Checking for updates";
        UpdateStatusMessage = "Contacting GitHub Releases...";

        try
        {
            var update = await _updateService.CheckAsync(_currentVersion);
            HasCheckedForUpdates = true;
            UpdateStatusTitle = update.Status switch
            {
                AppUpdateStatus.UpdateAvailable => "Update available",
                AppUpdateStatus.UpToDate => "You're up to date",
                AppUpdateStatus.NoPublishedRelease => "No published release",
                _ => "Couldn't check for updates"
            };
            UpdateStatusMessage = update.Message;
            LatestReleaseUri = update.Status == AppUpdateStatus.UpdateAvailable
                ? update.LatestReleaseUri
                : null;
            UpdateActionText = update.LatestVersion is null
                ? "View releases"
                : $"View version {GitHubReleaseUpdateService.FormatVersion(update.LatestVersion)}";
            IsUpdateLinkVisible = LatestReleaseUri is not null;
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    private async Task<bool> RefreshCoreAsync(bool clearStatus)
    {
        if (_paths is null || _isRefreshing)
        {
            return false;
        }

        _isRefreshing = true;
        IsBusy = true;
        if (clearStatus)
        {
            IsStatusOpen = false;
        }
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var catalogTask = Task.Run(() => _catalogService.Scan(_paths));
            var historyTask = Task.Run(() => _historyService.GetHistory(_paths));
            await Task.WhenAll(catalogTask, historyTask);

            var catalog = await catalogTask;
            var history = await historyTask;
            if (!catalog.Succeeded || catalog.Value is null)
            {
                ShowError(catalog.Error);
                return false;
            }

            if (!history.Succeeded || history.Value is null)
            {
                ShowError(history.Error);
                return false;
            }

            ApplyCatalog(catalog.Value);
            ApplyHistory(history.Value);

            stopwatch.Stop();
            LastRefreshMilliseconds = stopwatch.ElapsedMilliseconds;
            LastUpdatedText = $"Updated {DateTimeOffset.Now:t}";

            if (catalog.Value.Warnings.Count > 0)
            {
                OpenStatus(
                    "Some icons were skipped",
                    $"{catalog.Value.Warnings.Count:N0} files could not be previewed.",
                    InfoBarSeverity.Warning);
            }

            return true;
        }
        catch (Exception ex)
        {
            ShowError(new IconReplacerError(
                ErrorCode.Unknown,
                "The Icon Library could not be refreshed.",
                ex.Message));
            return false;
        }
        finally
        {
            _isRefreshing = false;
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunLibraryOperation))]
    private async Task ImportIconsAsync()
    {
        if (_paths is null)
        {
            return;
        }

        var request = _importPickerRequestService.CreateRequest(_paths);
        if (!request.Succeeded || request.Value is null || !request.Value.CanOpenPicker)
        {
            ShowError(request.Succeeded && request.Value is not null
                ? request.Value.Error
                : request.Error);
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
            ShowError(new IconReplacerError(
                ErrorCode.Unknown,
                "The icon picker could not be opened.",
                ex.Message));
            return;
        }

        if (sourcePaths.Count == 0)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var import = await Task.Run(() => _iconLibraryService.ImportIcons(sourcePaths, _paths));
            if (!import.Succeeded || import.Value is null)
            {
                ShowError(import.Error);
                return;
            }

            var feedback = _feedbackService.FromBatchImport(import.Value);
            IsBusy = false;
            if (await RefreshCoreAsync(clearStatus: true))
            {
                ShowFeedback(feedback);
            }
        }
        catch (Exception ex)
        {
            ShowError(new IconReplacerError(
                ErrorCode.Unknown,
                "The icons could not be imported.",
                ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunLibraryOperation))]
    private void OpenIconLibrary()
    {
        if (_paths is null || IsBusy)
        {
            return;
        }

        var request = _locationService.CreateOpenRequest(AppLocationKind.IconLibrary, _paths);
        if (!request.Succeeded || request.Value is null || !request.Value.CanOpen)
        {
            ShowError(request.Succeeded && request.Value is not null
                ? request.Value.Error
                : request.Error);
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
            ShowError(new IconReplacerError(
                ErrorCode.Unknown,
                "The Icon Library could not be opened.",
                ex.Message));
        }
    }

    private async Task RestoreRecordAsync(Guid recordId)
    {
        if (_paths is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var restore = await Task.Run(() => _iconRestoreService.Restore(recordId, _paths));
            if (!restore.Succeeded || restore.Value is null)
            {
                ShowError(restore.Error);
                return;
            }

            ShowFeedback(_feedbackService.FromRestore(restore.Value));
            await RefreshHistoryAsync();
        }
        catch (Exception ex)
        {
            ShowError(new IconReplacerError(
                ErrorCode.Unknown,
                "The original icon could not be restored.",
                ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshHistoryAsync()
    {
        if (_paths is null)
        {
            return;
        }

        var history = await Task.Run(() => _historyService.GetHistory(_paths));
        if (!history.Succeeded || history.Value is null)
        {
            ShowError(history.Error);
            return;
        }

        ApplyHistory(history.Value);
    }

    private void ApplyCatalog(IconCatalog catalog)
    {
        var selectedCategoryId = SelectedCategory?.Id ?? AllIconsCategoryId;
        _allIcons = catalog.Entries
            .Select(entry => new IconTileViewModel(
                HumanizeIconName(entry.DisplayName),
                entry.FullPath,
                entry.Category?.Name ?? "Uncategorized",
                entry.LengthBytes,
                GalleryLayout))
            .OrderBy(icon => icon.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(icon => icon.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var categories = new List<IconCategoryViewModel>
        {
            new(
                AllIconsCategoryId,
                "All icons",
                _allIcons.Count,
                _allIcons.Take(4).ToArray(),
                IsAllIcons: true)
        };

        categories.AddRange(_allIcons
            .GroupBy(icon => icon.CategoryName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new IconCategoryViewModel(
                group.Key,
                group.Key,
                group.Count(),
                group.Take(4).ToArray())));

        Categories = categories;
        CollectionCount = Math.Max(0, categories.Count - 1);
        SelectedCategory = categories.FirstOrDefault(category =>
            string.Equals(category.Id, selectedCategoryId, StringComparison.OrdinalIgnoreCase))
            ?? categories[0];
        LibrarySummary = $"{_allIcons.Count:N0} icons in {CollectionCount:N0} collections";
        ApplyFilter();
        CatalogChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string HumanizeIconName(string displayName)
    {
        var words = displayName
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(' ', words.Select(word =>
            word.Length == 1
                ? word.ToUpperInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..]));
    }

    private void ApplyHistory(RestoreHistorySnapshot history)
    {
        RecentChanges = history.Records
            .Select(record => new RecentChangeItemViewModel(
                record,
                RestoreRecordAsync,
                () => !IsBusy))
            .ToArray();
        RecentPreview = RecentChanges.Take(5).ToArray();
        OnPropertyChanged(nameof(HasRecentChanges));
    }

    private void ApplyFilter()
    {
        IEnumerable<IconTileViewModel> filtered = _allIcons;
        if (SelectedCategory is { IsAllIcons: false } category)
        {
            filtered = filtered.Where(icon =>
                string.Equals(icon.CategoryName, category.Id, StringComparison.OrdinalIgnoreCase));
        }

        var search = SearchText.Trim();
        if (search.Length > 0)
        {
            filtered = filtered.Where(icon =>
                icon.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                icon.CategoryName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        VisibleIcons = filtered.ToArray();
        VisibleSummary = VisibleIcons.Count == _allIcons.Count
            ? $"{VisibleIcons.Count:N0} icons"
            : $"{VisibleIcons.Count:N0} of {_allIcons.Count:N0} icons";
        EmptyStateMessage = search.Length > 0
            ? $"No icons match \"{search}\"."
            : "This collection does not contain any valid icons.";
        OnPropertyChanged(nameof(HasVisibleIcons));
    }

    private void ShowError(IconReplacerError error)
    {
        OpenStatus(
            error.Message,
            error.Detail ?? error.Code.ToString(),
            InfoBarSeverity.Error);
    }

    private void ShowFeedback(AppOperationFeedback feedback)
    {
        var severity = feedback.Severity switch
        {
            AppOperationFeedbackSeverity.Success => InfoBarSeverity.Success,
            AppOperationFeedbackSeverity.Warning => InfoBarSeverity.Warning,
            AppOperationFeedbackSeverity.Error => InfoBarSeverity.Error,
            _ => InfoBarSeverity.Informational
        };

        OpenStatus(feedback.Title, feedback.Detail, severity);
    }

    private void OpenStatus(string title, string message, InfoBarSeverity severity)
    {
        // Reopening an InfoBar causes assistive technology to announce updated content.
        IsStatusOpen = false;
        StatusTitle = title;
        StatusMessage = message;
        StatusSeverity = severity;
        IsStatusOpen = true;
    }

    partial void OnSelectedSectionChanged(string value)
    {
        OnPropertyChanged(nameof(IsLibraryVisible));
        OnPropertyChanged(nameof(IsRecentVisible));
        OnPropertyChanged(nameof(IsSettingsVisible));
        OnPropertyChanged(nameof(IsAboutVisible));
        if (value == AboutSection && !HasCheckedForUpdates)
        {
            CheckForUpdatesCommand.Execute(null);
        }
    }

    partial void OnSelectedCategoryChanged(IconCategoryViewModel? value)
    {
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnIsBusyChanged(bool value)
    {
        RefreshCommand.NotifyCanExecuteChanged();
        ImportIconsCommand.NotifyCanExecuteChanged();
        OpenIconLibraryCommand.NotifyCanExecuteChanged();
        foreach (var item in RecentChanges)
        {
            item.NotifyCanExecuteChanged();
        }
    }

    partial void OnIsCheckingForUpdatesChanged(bool value)
    {
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
    }

    private bool CanRunLibraryOperation() => _paths is not null && !IsBusy;

    private bool CanCheckForUpdates() => !IsCheckingForUpdates;

    private static Version ResolveCurrentVersion()
    {
        try
        {
            var version = Package.Current.Id.Version;
            return new Version(version.Major, version.Minor, version.Build, version.Revision);
        }
        catch (InvalidOperationException)
        {
            return typeof(MainPageViewModel).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);
        }
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

}
