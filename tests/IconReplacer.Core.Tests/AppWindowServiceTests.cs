using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppWindowServiceTests
{
    [Fact]
    public void GetWindowWithoutActivationArgumentsSelectsHome()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));

        var window = new AppWindowService().GetWindow([], paths, ReadyTooling());

        Assert.True(window.Succeeded, window.Error.Message);
        Assert.NotNull(window.Value);
        Assert.Equal(AppNavigationRouteIds.Home, window.Value.SelectedRouteId);
        Assert.Equal("Home", window.Value.WindowTitle);
        Assert.True(window.Value.CanUseSelectedRoute);
        Assert.NotNull(window.Value.Activation);
        Assert.Equal(AppActivationKind.Home, window.Value.Activation.Kind);
        Assert.Equal(13, window.Value.Navigation.RouteCount);
    }

    [Fact]
    public void GetWindowWithChangeIconActivationSelectsChangeIconRoute()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        Directory.CreateDirectory(folder);

        var window = new AppWindowService().GetWindow(
            [
                "change-icon",
                "--target",
                folder,
                "--target-kind",
                "folder"
            ],
            paths,
            ReadyTooling());

        Assert.True(window.Succeeded, window.Error.Message);
        Assert.NotNull(window.Value);
        Assert.Equal(AppNavigationRouteIds.ChangeIcon, window.Value.SelectedRouteId);
        Assert.Equal("Change Icon", window.Value.WindowTitle);
        Assert.True(window.Value.CanUseSelectedRoute);
        Assert.NotNull(window.Value.Activation);
        Assert.Equal(AppActivationKind.ChangeIcon, window.Value.Activation.Kind);
    }

    [Fact]
    public void GetWindowKeepsChangeIconRouteForUnsupportedTarget()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var file = temp.PathFor("notes.txt");
        File.WriteAllText(file, "not a shortcut");

        var window = new AppWindowService().GetWindow(
            [
                "change-icon",
                "--target",
                file,
                "--target-kind",
                "shortcut"
            ],
            paths,
            ReadyTooling());

        Assert.True(window.Succeeded, window.Error.Message);
        Assert.NotNull(window.Value);
        Assert.Equal(AppNavigationRouteIds.ChangeIcon, window.Value.SelectedRouteId);
        Assert.False(window.Value.CanUseSelectedRoute);
        Assert.Equal(ErrorCode.UnsupportedTarget, window.Value.Error.Code);
        Assert.NotNull(window.Value.Activation);
    }

    [Fact]
    public void GetWindowFallsBackToDiagnosticsForMalformedActivation()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var window = new AppWindowService().GetWindow(["unknown"], paths, ReadyTooling());

        Assert.True(window.Succeeded, window.Error.Message);
        Assert.NotNull(window.Value);
        Assert.Equal(AppNavigationRouteIds.Diagnostics, window.Value.SelectedRouteId);
        Assert.Equal("Diagnostics", window.Value.WindowTitle);
        Assert.False(window.Value.CanUseSelectedRoute);
        Assert.Null(window.Value.Activation);
        Assert.Equal(ErrorCode.InvalidArgument, window.Value.Error.Code);
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
