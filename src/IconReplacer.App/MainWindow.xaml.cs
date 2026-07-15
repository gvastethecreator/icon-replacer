using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.UI.ViewManagement;

namespace IconReplacer.App;

public sealed partial class MainWindow : Window
{
    private const string IconFileName = "AppIcon.ico";
    private const string DisplayIconFileName = "AppIcon.png";
    private readonly AccessibilitySettings _accessibilitySettings = new();
    private readonly UISettings _uiSettings = new();
    private string? _currentIconFileName;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        WindowRoot.ActualThemeChanged += WindowRoot_ActualThemeChanged;

        UpdateWindowChrome();
        PositionMainExperience();
    }

    internal void ShowMainPage()
    {
        if (RootFrame.Content is null && !RootFrame.Navigate(typeof(MainPage)))
        {
            return;
        }

        StartupProgress.IsActive = false;
        StartupProgress.Visibility = Visibility.Collapsed;
    }

    public void ApplyTheme(ElementTheme theme)
    {
        WindowRoot.RequestedTheme = theme;
        UpdateWindowChrome();
    }

    private void WindowRoot_ActualThemeChanged(FrameworkElement sender, object args)
    {
        UpdateWindowChrome();
    }

    private void UpdateWindowChrome()
    {
        var foreground = _accessibilitySettings.HighContrast
            ? _uiSettings.GetColorValue(UIColorType.Foreground)
            : WindowRoot.ActualTheme == ElementTheme.Light
                ? Colors.Black
                : Colors.White;
        AppWindow.TitleBar.ButtonForegroundColor = foreground;
        AppWindow.TitleBar.ButtonHoverForegroundColor = foreground;
        AppWindow.TitleBar.ButtonPressedForegroundColor = foreground;

        if (string.Equals(_currentIconFileName, IconFileName, StringComparison.Ordinal))
        {
            return;
        }

        var appIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", IconFileName);
        if (!File.Exists(appIconPath))
        {
            return;
        }

        AppWindow.SetIcon(appIconPath);
        AppTitleBar.IconSource = new ImageIconSource
        {
            ImageSource = new BitmapImage(new Uri($"ms-appx:///Assets/{DisplayIconFileName}"))
            {
                DecodePixelWidth = 64
            }
        };
        _currentIconFileName = IconFileName;
    }

    private void PositionMainExperience()
    {
        var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        var scale = GetDpiForWindow(hwnd) / 96.0;
        var displayArea = DisplayArea.GetFromWindowId(
            AppWindow.Id,
            DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;
        var width = Math.Min((int)Math.Round(1360 * scale), workArea.Width);
        var height = Math.Min((int)Math.Round(860 * scale), workArea.Height);
        var x = workArea.X + ((workArea.Width - width) / 2);
        var y = workArea.Y + ((workArea.Height - height) / 2);

        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
    }
}
