using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconRestoreServiceTests
{
    [Fact]
    public void RestoreByIdRestoresFolderAndUpdatesStore()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(sourceIcon);

        var apply = new IconApplyService().Apply(folder, sourceIcon, paths);
        Assert.True(apply.Succeeded, apply.Error.Message);
        Assert.NotNull(apply.Value);
        Assert.True(File.Exists(apply.Value.DesktopIniPath));

        var restore = new IconRestoreService().Restore(apply.Value.RestoreRecord.Id, paths);

        Assert.True(restore.Succeeded, restore.Error.Message);
        Assert.NotNull(restore.Value);
        Assert.Equal(TargetKind.Folder, restore.Value.TargetKind);
        Assert.Equal(RestoreRecordStatus.Restored, restore.Value.RestoreRecord.Status);
        Assert.False(File.Exists(restore.Value.DesktopIniPath));

        var stored = new RestoreRecordStore(paths.RestoreStateFile).Get(apply.Value.RestoreRecord.Id);
        Assert.True(stored.Succeeded, stored.Error.Message);
        Assert.Equal(RestoreRecordStatus.Restored, stored.Value!.Status);
    }

    [Fact]
    public void RestoreRejectsRecordThatIsNotApplied()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(sourceIcon);
        var apply = new IconApplyService().Apply(folder, sourceIcon, paths);
        Assert.True(apply.Succeeded, apply.Error.Message);
        var firstRestore = new IconRestoreService().Restore(apply.Value!.RestoreRecord.Id, paths);
        Assert.True(firstRestore.Succeeded, firstRestore.Error.Message);

        var secondRestore = new IconRestoreService().Restore(apply.Value.RestoreRecord.Id, paths);

        Assert.False(secondRestore.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, secondRestore.Error.Code);
    }

    [Fact]
    public void RestoreMissingRecordReturnsPathNotFound()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var restore = new IconRestoreService().Restore(Guid.NewGuid(), paths);

        Assert.False(restore.Succeeded);
        Assert.Equal(ErrorCode.PathNotFound, restore.Error.Code);
    }
}
