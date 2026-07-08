using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class ActivatedMenuApplyServiceTests
{
    [Fact]
    public void ApplyActivationAppliesLibraryIconAndStoresRestoreRecord()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        var icon = Path.Combine(paths.LibraryRoot, "Work", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(icon);

        var result = new ActivatedMenuApplyService().ApplyActivation(
            [IconMenuCommandService.MenuApplyVerbName, folder, icon],
            paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(AppActivationKind.MenuApply, result.Value.Activation.Kind);
        Assert.Equal("blue", result.Value.MenuApplyResult.MenuItem.DisplayName);
        Assert.Equal(TargetKind.Folder, result.Value.MenuApplyResult.ChangeResult.ApplyResult.TargetKind);
        Assert.True(File.Exists(result.Value.MenuApplyResult.ChangeResult.ApplyResult.DesktopIniPath));

        var records = new RestoreRecordStore(paths.RestoreStateFile).List();
        Assert.True(records.Succeeded, records.Error.Message);
        Assert.Single(records.Value!);
    }

    [Fact]
    public void ApplyActivationRejectsIconOutsideLibraryBeforeMutation()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        var externalIcon = temp.PathFor("external", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(externalIcon);

        var result = new ActivatedMenuApplyService().ApplyActivation(
            [IconMenuCommandService.MenuApplyVerbName, folder, externalIcon],
            paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidIcon, result.Error.Code);
        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void ApplyActivationRejectsChangeIconActivation()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        Directory.CreateDirectory(folder);

        var result = new ActivatedMenuApplyService().ApplyActivation(
            ["change-icon", "--target", folder, "--target-kind", "folder"],
            paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, result.Error.Code);
        Assert.False(File.Exists(paths.RestoreStateFile));
    }
}
