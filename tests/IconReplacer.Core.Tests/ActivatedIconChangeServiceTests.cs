using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class ActivatedIconChangeServiceTests
{
    [Fact]
    public void PreviewSelectedIconCombinesActivationAndIconDetailsWithoutApplying()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        var icon = temp.PathFor("icons", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(icon);

        var preview = new ActivatedIconChangeService().PreviewSelectedIcon(
            ChangeIconArguments(folder),
            icon,
            paths);

        Assert.True(preview.Succeeded, preview.Error.Message);
        Assert.NotNull(preview.Value);
        Assert.True(preview.Value.CanApply);
        Assert.Equal(AppActivationKind.ChangeIcon, preview.Value.Activation.Kind);
        Assert.Equal(TargetKind.Folder, preview.Value.Preview.Selection.Target!.Kind);
        Assert.Equal("blue", preview.Value.Preview.IconDetails!.DisplayName);
        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void ApplySelectedIconChangesFolderAndStoresRestoreRecord()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        var icon = temp.PathFor("icons", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(icon);

        var result = new ActivatedIconChangeService().ApplySelectedIcon(
            ChangeIconArguments(folder),
            icon,
            paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(AppActivationKind.ChangeIcon, result.Value.Activation.Kind);
        Assert.Equal(TargetKind.Folder, result.Value.ChangeResult.ApplyResult.TargetKind);
        Assert.True(File.Exists(result.Value.ChangeResult.ApplyResult.DesktopIniPath));

        var records = new RestoreRecordStore(paths.RestoreStateFile).List();
        Assert.True(records.Succeeded, records.Error.Message);
        Assert.Single(records.Value!);
    }

    [Fact]
    public void PreviewSelectedIconRejectsHomeActivation()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var icon = temp.PathFor("icons", "blue.ico");
        TestIconFactory.WriteValidIcon(icon);

        var preview = new ActivatedIconChangeService().PreviewSelectedIcon([], icon, paths);

        Assert.False(preview.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, preview.Error.Code);
    }

    [Fact]
    public void ApplySelectedIconRejectsHomeActivation()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var icon = temp.PathFor("icons", "blue.ico");
        TestIconFactory.WriteValidIcon(icon);

        var result = new ActivatedIconChangeService().ApplySelectedIcon([], icon, paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, result.Error.Code);
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void ApplySelectedIconRejectsUnsupportedTargetBeforeImport()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var file = temp.PathFor("notes.txt");
        var icon = temp.PathFor("icons", "blue.ico");
        File.WriteAllText(file, "not a shortcut");
        TestIconFactory.WriteValidIcon(icon);

        var result = new ActivatedIconChangeService().ApplySelectedIcon(
            [
                "change-icon",
                "--target",
                file,
                "--target-kind",
                "shortcut"
            ],
            icon,
            paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.UnsupportedTarget, result.Error.Code);
        Assert.False(Directory.Exists(paths.ImportedRoot));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void ApplySelectedIconRejectsInvalidIconBeforeMutation()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        var badIcon = temp.PathFor("icons", "bad.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WritePngHeader(badIcon);

        var result = new ActivatedIconChangeService().ApplySelectedIcon(
            ChangeIconArguments(folder),
            badIcon,
            paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidIcon, result.Error.Code);
        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    private static string[] ChangeIconArguments(string folder)
    {
        return
        [
            "change-icon",
            "--target",
            folder,
            "--target-kind",
            "folder"
        ];
    }
}
