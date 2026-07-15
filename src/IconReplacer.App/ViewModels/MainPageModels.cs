using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconReplacer.AppModel;

namespace IconReplacer.App.ViewModels;

public enum AppThemePreference
{
    System,
    Light,
    Dark
}

public sealed class GalleryLayoutMetrics : ObservableObject
{
    private double _cellWidth = 88;
    private double _cellHeight = 116;
    private double _previewSize = 64;

    public double CellWidth
    {
        get => _cellWidth;
        private set => SetProperty(ref _cellWidth, value);
    }

    public double CellHeight
    {
        get => _cellHeight;
        private set => SetProperty(ref _cellHeight, value);
    }

    public double PreviewSize
    {
        get => _previewSize;
        private set => SetProperty(ref _previewSize, value);
    }

    public void Set(double cellWidth, double previewSize, double cellHeight)
    {
        CellWidth = cellWidth;
        PreviewSize = previewSize;
        CellHeight = cellHeight;
    }
}

public sealed class IconTileViewModel
{
    public IconTileViewModel(
        string displayName,
        string fullPath,
        string categoryName,
        long lengthBytes,
        GalleryLayoutMetrics layout)
    {
        DisplayName = displayName;
        FullPath = fullPath;
        CategoryName = categoryName;
        LengthBytes = lengthBytes;
        Layout = layout;
    }

    public string DisplayName { get; }

    public string FullPath { get; }

    public string CategoryName { get; }

    public long LengthBytes { get; }

    public GalleryLayoutMetrics Layout { get; }

    public string AccessibilityName => $"{DisplayName}, {CategoryName} collection";

    public override string ToString() => DisplayName;
}

public sealed record IconCategoryViewModel(
    string Id,
    string DisplayName,
    int IconCount,
    IReadOnlyList<IconTileViewModel> PreviewIcons,
    bool IsAllIcons = false)
{
    public string IconCountText => IconCount == 1 ? "1 icon" : $"{IconCount:N0} icons";

    public override string ToString() => DisplayName;
}

public sealed class RecentChangeItemViewModel
{
    public RecentChangeItemViewModel(
        RestoreRecordSummary record,
        Func<Guid, Task> restoreAsync,
        Func<bool>? canRunRestore = null)
    {
        Id = record.Id;
        TargetKind = record.TargetKind.ToString();
        TargetPath = record.TargetPath;
        TargetDisplayName = ResolveDisplayName(record.TargetPath);
        TargetDirectory = Path.GetDirectoryName(record.TargetPath) ?? record.TargetPath;
        AppliedIconPath = record.AppliedIconPath;
        CreatedAt = record.CreatedAt;
        Timestamp = record.CreatedAt.LocalDateTime.ToString("g");
        Status = record.CanRestore ? "Applied" : record.Status.ToString();
        CanRestore = record.CanRestore;
        RestoreCommand = new AsyncRelayCommand(
            () => restoreAsync(record.Id),
            () => record.CanRestore && (canRunRestore?.Invoke() ?? true));
    }

    public Guid Id { get; }

    public string TargetKind { get; }

    public string TargetPath { get; }

    public string TargetDisplayName { get; }

    public string TargetDirectory { get; }

    public string AppliedIconPath { get; }

    public DateTimeOffset CreatedAt { get; }

    public string Timestamp { get; }

    public string Status { get; }

    public bool CanRestore { get; }

    public string RestoreAutomationId => $"Restore-{Id:N}";

    public string RestoreAutomationName => $"Restore original icon for {TargetDisplayName}";

    public string AccessibilityName =>
        $"{TargetDisplayName}, {TargetKind}, {Status}, changed {Timestamp}";

    public IAsyncRelayCommand RestoreCommand { get; }

    public void NotifyCanExecuteChanged() => RestoreCommand.NotifyCanExecuteChanged();

    public override string ToString() => TargetDisplayName;

    private static string ResolveDisplayName(string path)
    {
        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);
        return string.IsNullOrWhiteSpace(name) ? path : name;
    }
}
