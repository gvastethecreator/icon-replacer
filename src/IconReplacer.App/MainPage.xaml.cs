using Microsoft.UI.Xaml.Controls;
using IconReplacer.App.ViewModels;

namespace IconReplacer.App;

public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; } = new();
    private bool _initialized;

    public MainPage()
    {
        InitializeComponent();
        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        if (App.InitialActivationArguments.Count == 0)
        {
            AppNavigation.SelectedItem = HomeNavItem;
        }

        DispatcherQueue.TryEnqueue(async () => await InitializeAfterLoadedAsync());
    }

    private async Task InitializeAfterLoadedAsync()
    {
        await Task.Delay(250);

        if (App.StartupActivationException is not null)
        {
            ViewModel.ShowUnexpectedException(App.StartupActivationException);
            return;
        }

        try
        {
            await ViewModel.InitializeAsync(App.InitialActivationArguments);
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
        if (args.SelectedItem is NavigationViewItem { Tag: string routeId })
        {
            ViewModel.SelectRoute(routeId);
        }
    }
}
