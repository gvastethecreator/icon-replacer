using IconReplacer.App.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using VirtualKey = Windows.System.VirtualKey;

namespace IconReplacer.App;

public sealed partial class MainPage : Page
{
    private readonly IconThumbnailCache _thumbnailCache = new();
    private DispatcherQueueTimer? _galleryLayoutTimer;
    private double _collectionsPaneDragWidth;
    private double? _pendingPreviewSize;
    private bool _collectionsPaneWidthDirty;
    private bool _galleryLayoutScheduled;
    private bool _isDraggingCollectionsSplitter;
    private bool _initialized;
    private bool _updatingThemeSelector;

    public MainPageViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        ViewModel.CatalogChanged += ViewModel_CatalogChanged;
        ApplyTheme(ViewModel.ThemePreference);
        Loaded += MainPage_Loaded;
    }

    public Visibility BoolToVisibility(bool value) =>
        value ? Visibility.Visible : Visibility.Collapsed;

    public Visibility InverseBoolToVisibility(bool value) =>
        value ? Visibility.Collapsed : Visibility.Visible;

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        AppNavigation.SelectedItem = LibraryNavItem;
        SyncThemeSelector();
        ApplyTheme(ViewModel.ThemePreference);
        DispatcherQueue.TryEnqueue(async () => await InitializeAfterLoadedAsync());
    }

    private async Task InitializeAfterLoadedAsync()
    {
        try
        {
            await ViewModel.InitializeAsync();
        }
        catch (Exception ex)
        {
            ViewModel.ShowUnexpectedException(ex);
        }
    }

    private void AppNavigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: string section })
        {
            ViewModel.SelectSection(section);
        }
    }

    private void SearchBox_TextChanged(
        AutoSuggestBox sender,
        AutoSuggestBoxTextChangedEventArgs args)
    {
        ViewModel.SearchText = sender.Text;
    }

    private void ViewAllRecent_Click(object sender, RoutedEventArgs e)
    {
        AppNavigation.SelectedItem = RecentNavItem;
    }

    private void ThemeSelector_SelectionChanged(
        object sender,
        SelectionChangedEventArgs args)
    {
        if (sender is not RadioButtons themeSelector ||
            _updatingThemeSelector ||
            themeSelector.SelectedIndex < 0)
        {
            return;
        }

        var preference = themeSelector.SelectedIndex switch
        {
            1 => AppThemePreference.Light,
            2 => AppThemePreference.Dark,
            _ => AppThemePreference.System
        };

        ViewModel.SetThemePreference(preference);
        ApplyTheme(preference);
    }

    private void SyncThemeSelector()
    {
        _updatingThemeSelector = true;
        ThemeSelector.SelectedIndex = ViewModel.ThemePreference switch
        {
            AppThemePreference.Light => 1,
            AppThemePreference.Dark => 2,
            _ => 0
        };
        _updatingThemeSelector = false;
    }

    private void ApplyTheme(AppThemePreference preference)
    {
        var elementTheme = preference switch
        {
            AppThemePreference.Light => ElementTheme.Light,
            AppThemePreference.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        RequestedTheme = elementTheme;
        if (App.Window is MainWindow mainWindow)
        {
            mainWindow.ApplyTheme(elementTheme);
        }
    }

    private void ViewModel_CatalogChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => ScheduleGalleryLayout());
    }

    private void IconGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_isDraggingCollectionsSplitter)
        {
            return;
        }

        ScheduleGalleryLayout();
    }

    private void PreviewSizeSlider_ValueChanged(
        object sender,
        RangeBaseValueChangedEventArgs e)
    {
        ScheduleGalleryLayout(e.NewValue);
    }

    private void ScheduleGalleryLayout(double? requestedPreviewSize = null)
    {
        if (requestedPreviewSize.HasValue)
        {
            _pendingPreviewSize = requestedPreviewSize;
        }

        if (_galleryLayoutTimer is null)
        {
            _galleryLayoutTimer = DispatcherQueue.CreateTimer();
            _galleryLayoutTimer.Interval = TimeSpan.FromMilliseconds(50);
            _galleryLayoutTimer.IsRepeating = false;
            _galleryLayoutTimer.Tick += GalleryLayoutTimer_Tick;
        }

        if (_galleryLayoutScheduled)
        {
            return;
        }

        _galleryLayoutScheduled = true;
        _galleryLayoutTimer.Start();
    }

    private void GalleryLayoutTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        _galleryLayoutScheduled = false;
        var requestedPreviewSize = _pendingPreviewSize;
        _pendingPreviewSize = null;
        UpdateGalleryLayout(requestedPreviewSize);
    }

    private void UpdateGalleryLayout(double? requestedPreviewSize = null)
    {
        const double itemMargin = 6;
        const double scrollbarReserve = 14;
        var availableWidth = IconGrid.ActualWidth -
            IconGrid.Padding.Left -
            IconGrid.Padding.Right -
            scrollbarReserve;
        var availableHeight = IconGrid.ActualHeight - IconGrid.Padding.Top - IconGrid.Padding.Bottom;
        if (availableWidth <= 0 || availableHeight <= 0)
        {
            return;
        }

        var preferredPreviewSize = requestedPreviewSize ?? ViewModel.IconPreviewSize;
        var minimumCellWidth = preferredPreviewSize + 20;
        var columns = Math.Max(
            1,
            (int)Math.Floor(availableWidth / (minimumCellWidth + itemMargin)));
        var cellWidth = Math.Floor(availableWidth / columns) - itemMargin;
        var renderedPreviewSize = Math.Clamp(
            Math.Min(preferredPreviewSize + 8, cellWidth - 20),
            32,
            120);

        var minimumTileHeight = renderedPreviewSize + 52;
        var rows = Math.Max(1, (int)Math.Floor(availableHeight / (minimumTileHeight + itemMargin)));
        var tileHeight = Math.Floor(availableHeight / rows) - itemMargin;
        if (tileHeight < minimumTileHeight && rows > 1)
        {
            rows--;
            tileHeight = Math.Floor(availableHeight / rows) - itemMargin;
        }

        ViewModel.SetIconCellLayout(
            cellWidth,
            renderedPreviewSize,
            Math.Max(minimumTileHeight, tileHeight));
    }

    private void CollectionsSplitter_DragStarted(object sender, DragStartedEventArgs e)
    {
        _isDraggingCollectionsSplitter = true;
        _collectionsPaneDragWidth = CollectionsColumn.ActualWidth;
        _collectionsPaneWidthDirty = false;
        _galleryLayoutTimer?.Stop();
        _galleryLayoutScheduled = false;
        CompositionTarget.Rendering -= CollectionsPane_Rendering;
        CompositionTarget.Rendering += CollectionsPane_Rendering;
    }

    private void CollectionsSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        _collectionsPaneDragWidth = ClampCollectionsPaneWidth(
            _collectionsPaneDragWidth + e.HorizontalChange);
        _collectionsPaneWidthDirty = true;
    }

    private void CollectionsSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        CompositionTarget.Rendering -= CollectionsPane_Rendering;
        if (_collectionsPaneWidthDirty)
        {
            SetCollectionsPaneWidth(_collectionsPaneDragWidth);
            _collectionsPaneWidthDirty = false;
        }

        _isDraggingCollectionsSplitter = false;
        ScheduleGalleryLayout();
    }

    private void CollectionsPane_Rendering(object? sender, object e)
    {
        if (!_collectionsPaneWidthDirty)
        {
            return;
        }

        SetCollectionsPaneWidth(_collectionsPaneDragWidth);
        _collectionsPaneWidthDirty = false;
    }

    private void CollectionsSplitter_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        const double keyboardStep = 12;
        var delta = e.Key switch
        {
            VirtualKey.Left => -keyboardStep,
            VirtualKey.Right => keyboardStep,
            _ => 0
        };
        if (delta == 0)
        {
            return;
        }

        ResizeCollectionsPane(delta);
        e.Handled = true;
    }

    private void ResizeCollectionsPane(double delta)
    {
        SetCollectionsPaneWidth(CollectionsColumn.ActualWidth + delta);
        ScheduleGalleryLayout();
    }

    private void SetCollectionsPaneWidth(double requestedWidth)
    {
        var width = ClampCollectionsPaneWidth(requestedWidth);
        CollectionsColumn.Width = new GridLength(width);
    }

    private static double ClampCollectionsPaneWidth(double requestedWidth) =>
        Math.Clamp(requestedWidth, 220, 380);

    private async void IconPreview_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is Image image)
        {
            await LoadIconPreviewAsync(image);
        }
    }

    private async void IconPreview_DataContextChanged(
        FrameworkElement sender,
        DataContextChangedEventArgs args)
    {
        if (sender is Image image)
        {
            image.Source = null;
            await LoadIconPreviewAsync(image);
        }
    }

    private async void IconPreview_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Image image && e.NewSize.Width > e.PreviousSize.Width)
        {
            await LoadIconPreviewAsync(image);
        }
    }

    private async Task LoadIconPreviewAsync(Image image)
    {
        if (image.Tag is not string path || string.IsNullOrWhiteSpace(path))
        {
            image.Source = null;
            return;
        }

        var decodePixelWidth = GetRequiredDecodePixelWidth(image);
        if (image.Source is BitmapImage currentThumbnail &&
            currentThumbnail.DecodePixelWidth >= decodePixelWidth)
        {
            return;
        }

        BitmapImage? thumbnail;
        try
        {
            thumbnail = await _thumbnailCache.GetAsync(path, decodePixelWidth);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            thumbnail = null;
        }

        if (string.Equals(image.Tag as string, path, StringComparison.OrdinalIgnoreCase))
        {
            image.Source = thumbnail;
        }
    }

    private static int GetRequiredDecodePixelWidth(Image image)
    {
        var logicalWidth = image.ActualWidth;
        if (logicalWidth <= 0 && !double.IsNaN(image.Width))
        {
            logicalWidth = image.Width;
        }
        if (logicalWidth <= 0)
        {
            logicalWidth = 64;
        }

        var rasterizationScale = image.XamlRoot?.RasterizationScale ?? 1;
        return Math.Clamp(
            (int)Math.Ceiling(logicalWidth * rasterizationScale),
            1,
            256);
    }
}
