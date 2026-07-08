using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppHomeServiceTests
{
    [Fact]
    public void GetSnapshotComposesMainWindowState()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));

        var target = temp.PathFor("target-folder");
        var appliedIcon = temp.PathFor("icons", "applied.ico");
        Directory.CreateDirectory(target);
        TestIconFactory.WriteValidIcon(appliedIcon);

        var record = RestoreRecord.CreatePending(
            new TargetItem(TargetKind.Folder, target),
            appliedIcon,
            new FolderRestoreSnapshot(
                DesktopIniExisted: false,
                new Dictionary<string, string?>(),
                FileAttributes.Directory,
                null)) with { Status = RestoreRecordStatus.Applied };
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);

        var snapshot = new AppHomeService().GetSnapshot(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.True(snapshot.Value.CanUseCoreFeatures);
        Assert.True(snapshot.Value.NeedsAttention);
        Assert.Equal(paths.LibraryRoot, snapshot.Value.Setup.IconLibraryRoot);
        Assert.Equal(1, snapshot.Value.Dashboard.IconCount);
        Assert.Equal(1, snapshot.Value.Menu.VisibleIconCount);
        Assert.Equal(1, snapshot.Value.History.TotalCount);
        Assert.Equal(1, snapshot.Value.History.RestorableCount);
        Assert.Contains(snapshot.Value.Locations, location => location.Kind == AppLocationKind.IconLibrary && location.Exists);
        Assert.Contains(snapshot.Value.Setup.Actions, action => action.Id == "configure-shell-integration");
    }

    [Fact]
    public void GetSnapshotHonorsHistoryFilterAndMenuCaps()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "one.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "two.ico"));

        var staleRecord = RestoreRecord.CreatePending(
            new TargetItem(TargetKind.Folder, temp.PathFor("deleted")),
            temp.PathFor("icons", "missing.ico"),
            new FolderRestoreSnapshot(
                DesktopIniExisted: false,
                new Dictionary<string, string?>(),
                FileAttributes.Directory,
                null)) with { Status = RestoreRecordStatus.Applied };
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(staleRecord).Succeeded);

        var snapshot = new AppHomeService().GetSnapshot(
            paths,
            RestoreHistoryFilter.Stale,
            new IconMenuOptions(
                MaxRootIconItems: 0,
                MaxCategoryCount: 1,
                MaxIconItemsPerCategory: 1));

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.True(snapshot.Value.Menu.IsTruncated);
        Assert.Equal(1, snapshot.Value.Menu.VisibleIconCount);
        Assert.Equal(RestoreHistoryFilter.Stale, snapshot.Value.History.Filter);
        var record = Assert.Single(snapshot.Value.History.Records);
        Assert.Equal(staleRecord.Id, record.Id);
        Assert.False(record.CanRestore);
    }
}
