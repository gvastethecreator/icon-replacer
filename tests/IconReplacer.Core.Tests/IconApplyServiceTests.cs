using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconApplyServiceTests
{
    [Fact]
    public void ApplyDetectsFolderTargetAndStoresRestoreRecord()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(sourceIcon);

        var result = new IconApplyService().Apply(folder, sourceIcon, paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(TargetKind.Folder, result.Value.TargetKind);
        Assert.True(File.Exists(result.Value.ImportedIcon.FullPath));
        Assert.True(File.Exists(result.Value.DesktopIniPath));
        Assert.Equal(RestoreRecordStatus.Applied, result.Value.RestoreRecord.Status);

        var records = new RestoreRecordStore(paths.RestoreStateFile).List();
        Assert.True(records.Succeeded, records.Error.Message);
        var stored = Assert.Single(records.Value!);
        Assert.Equal(result.Value.RestoreRecord.Id, stored.Id);
        Assert.Equal(TargetKind.Folder, stored.Target.Kind);
        Assert.Equal(folder, stored.Target.FullPath);
    }

    [Fact]
    public void ApplyRejectsUnsupportedExistingFile()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var targetFile = temp.PathFor("notes.txt");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        File.WriteAllText(targetFile, "not a shortcut");
        TestIconFactory.WriteValidIcon(sourceIcon);

        var result = new IconApplyService().Apply(targetFile, sourceIcon, paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.UnsupportedTarget, result.Error.Code);
    }
}
