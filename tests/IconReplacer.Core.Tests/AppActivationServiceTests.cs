using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppActivationServiceTests
{
    [Fact]
    public void ActivateWithoutArgumentsReturnsHomeSnapshot()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));

        var activation = new AppActivationService().Activate([], paths);

        Assert.True(activation.Succeeded, activation.Error.Message);
        Assert.NotNull(activation.Value);
        Assert.Equal(AppActivationKind.Home, activation.Value.Kind);
        Assert.True(activation.Value.CanContinue);
        Assert.NotNull(activation.Value.Home);
        Assert.Null(activation.Value.LaunchRequest);
        Assert.Null(activation.Value.MenuApplyRequest);
        Assert.Equal(1, activation.Value.Home.Dashboard.IconCount);
    }

    [Fact]
    public void ActivateWithChangeIconArgumentsReturnsLaunchRequest()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        Directory.CreateDirectory(folder);

        var activation = new AppActivationService().Activate(
            [
                "change-icon",
                "--target",
                folder,
                "--target-kind",
                "folder"
            ],
            paths);

        Assert.True(activation.Succeeded, activation.Error.Message);
        Assert.NotNull(activation.Value);
        Assert.Equal(AppActivationKind.ChangeIcon, activation.Value.Kind);
        Assert.True(activation.Value.CanContinue);
        Assert.Null(activation.Value.Home);
        Assert.NotNull(activation.Value.LaunchRequest);
        Assert.Null(activation.Value.MenuApplyRequest);
        Assert.Equal(TargetKind.Folder, activation.Value.LaunchRequest.Selection.Target!.Kind);
        Assert.True(activation.Value.LaunchRequest.PickerRequest.CanOpenPicker);
    }

    [Fact]
    public void ActivateWithMenuApplyArgumentsReturnsMenuApplyRequest()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        var icon = Path.Combine(paths.LibraryRoot, "Work", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(icon);

        var activation = new AppActivationService().Activate(
            [IconMenuCommandService.MenuApplyVerbName, folder, icon],
            paths);

        Assert.True(activation.Succeeded, activation.Error.Message);
        Assert.NotNull(activation.Value);
        Assert.Equal(AppActivationKind.MenuApply, activation.Value.Kind);
        Assert.True(activation.Value.CanContinue);
        Assert.Null(activation.Value.Home);
        Assert.Null(activation.Value.LaunchRequest);
        Assert.NotNull(activation.Value.MenuApplyRequest);
        Assert.True(activation.Value.MenuApplyRequest.CanApply);
        Assert.Equal(icon, activation.Value.MenuApplyRequest.RequestedIconPath);
    }

    [Fact]
    public void ActivateWithUnsupportedTargetReturnsDisabledChangeIconSnapshot()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var file = temp.PathFor("notes.txt");
        File.WriteAllText(file, "not a shortcut");

        var activation = new AppActivationService().Activate(
            [
                "change-icon",
                "--target",
                file,
                "--target-kind",
                "shortcut"
            ],
            paths);

        Assert.True(activation.Succeeded, activation.Error.Message);
        Assert.NotNull(activation.Value);
        Assert.Equal(AppActivationKind.ChangeIcon, activation.Value.Kind);
        Assert.False(activation.Value.CanContinue);
        Assert.Equal(ErrorCode.UnsupportedTarget, activation.Value.Error.Code);
        Assert.NotNull(activation.Value.LaunchRequest);
        Assert.Null(activation.Value.MenuApplyRequest);
        Assert.Empty(activation.Value.LaunchRequest.AppArguments);
    }

    [Fact]
    public void ActivateRejectsUnknownVerb()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var activation = new AppActivationService().Activate(["unknown"], paths);

        Assert.False(activation.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, activation.Error.Code);
    }
}
