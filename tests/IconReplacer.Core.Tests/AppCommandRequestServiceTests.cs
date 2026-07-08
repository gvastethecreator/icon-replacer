using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppCommandRequestServiceTests
{
    [Fact]
    public void CreateRequestResolvesNavigationCommand()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var window = CreateWindow(paths);

        var request = new AppCommandRequestService().CreateRequest(window, paths, "import-icons");

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanExecute);
        Assert.Equal(AppCommandKind.Navigate, request.Value.Command.Kind);
        Assert.Equal(AppNavigationRouteIds.ImportIcons, request.Value.NavigationTarget);
        Assert.Null(request.Value.LocationOpenRequest);
        Assert.False(request.Value.RequiresRefresh);
    }

    [Fact]
    public void CreateRequestResolvesOpenLocationCommandWithoutOpeningIt()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var window = CreateWindow(paths);

        var request = new AppCommandRequestService().CreateRequest(
            window,
            paths,
            "open-icon-library",
            AppNavigationRouteIds.Collections);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanExecute);
        Assert.Equal(AppCommandKind.OpenLocation, request.Value.Command.Kind);
        Assert.NotNull(request.Value.LocationOpenRequest);
        Assert.True(request.Value.LocationOpenRequest.CanOpen);
        Assert.Equal(paths.LibraryRoot, request.Value.LocationOpenRequest.TargetPath);
        Assert.True(Directory.Exists(paths.LibraryRoot));
    }

    [Fact]
    public void CreateRequestReportsDisabledCommandReason()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var window = CreateWindow(paths);

        var request = new AppCommandRequestService().CreateRequest(
            window,
            paths,
            "open-icon-details",
            AppNavigationRouteIds.IconBrowser);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.False(request.Value.CanExecute);
        Assert.Equal(ErrorCode.InvalidArgument, request.Value.Error.Code);
        Assert.Contains("Select an icon", request.Value.Error.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateRequestResolvesRefreshCommand()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var window = CreateWindow(paths);

        var request = new AppCommandRequestService().CreateRequest(
            window,
            paths,
            "refresh-diagnostics",
            AppNavigationRouteIds.Diagnostics);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanExecute);
        Assert.True(request.Value.RequiresRefresh);
        Assert.Equal(AppCommandKind.Refresh, request.Value.Command.Kind);
    }

    [Fact]
    public void CreateRequestRejectsCommandNotOnRoute()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var window = CreateWindow(paths);

        var request = new AppCommandRequestService().CreateRequest(
            window,
            paths,
            "missing-command");

        Assert.False(request.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, request.Error.Code);
    }

    private static AppWindowSnapshot CreateWindow(IconLibraryPaths paths)
    {
        return new AppWindowService().GetWindow([], paths, ReadyTooling()).Value!;
    }

    private static IconLibraryPaths CreatePathsWithIcon(TempDirectory temp)
    {
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));
        return paths;
    }

    private static WinUiToolingSnapshot ReadyTooling()
    {
        return new WinUiToolingSnapshot(
            IsChecked: true,
            WinUiTemplatesAvailable: true,
            WinAppAvailable: true,
            "WinUI templates are available.",
            "winapp CLI is available.");
    }
}
