using Microsoft.UI.Xaml;
using Microsoft.UI;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace IconReplacer.App;

public sealed partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");
        ResizeForMainExperience();

        RootFrame.Navigate(typeof(MainPage));
    }

    private void ResizeForMainExperience()
    {
        var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        var scale = GetDpiForWindow(hwnd) / 96.0;
        AppWindow.Resize(new SizeInt32((int)(1180 * scale), (int)(780 * scale)));
    }
}
