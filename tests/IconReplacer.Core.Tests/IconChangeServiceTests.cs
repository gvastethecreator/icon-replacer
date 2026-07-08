using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconChangeServiceTests
{
    [Fact]
    public void ChangeIconAppliesFolderAndStoresRestoreRecord()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(sourceIcon);

        var result = new IconChangeService().ChangeIcon(folder, sourceIcon, paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(ShellSelectionStatus.Supported, result.Value.Selection.Status);
        Assert.Equal(TargetKind.Folder, result.Value.ApplyResult.TargetKind);
        Assert.True(File.Exists(result.Value.ApplyResult.ImportedIcon.FullPath));
        Assert.True(File.Exists(result.Value.ApplyResult.DesktopIniPath));

        var records = new RestoreRecordStore(paths.RestoreStateFile).List();
        Assert.True(records.Succeeded, records.Error.Message);
        Assert.Single(records.Value!);
    }

    [Fact]
    public void ChangeIconRejectsUnsupportedSelectionBeforeImport()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var targetFile = temp.PathFor("notes.txt");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        File.WriteAllText(targetFile, "not a shortcut");
        TestIconFactory.WriteValidIcon(sourceIcon);

        var result = new IconChangeService().ChangeIcon(targetFile, sourceIcon, paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.UnsupportedTarget, result.Error.Code);
        Assert.False(Directory.Exists(paths.ImportedRoot));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void ChangeIconRejectsMultipleSelectionBeforeImport()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var first = temp.PathFor("first");
        var second = temp.PathFor("second");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        Directory.CreateDirectory(first);
        Directory.CreateDirectory(second);
        TestIconFactory.WriteValidIcon(sourceIcon);

        var result = new IconChangeService().ChangeIcon(
            [
                new ShellSelectionItem(first, IsDirectory: true),
                new ShellSelectionItem(second, IsDirectory: true)
            ],
            sourceIcon,
            paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.UnsupportedTarget, result.Error.Code);
        Assert.False(Directory.Exists(paths.ImportedRoot));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }
}
