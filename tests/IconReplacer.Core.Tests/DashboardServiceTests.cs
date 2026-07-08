using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class DashboardServiceTests
{
    [Fact]
    public void GetSnapshotSummarizesCatalogAndRestoreRecords()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "root.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));

        var folderTarget = temp.PathFor("folder");
        var shortcutTarget = temp.PathFor("shortcut.lnk");
        Directory.CreateDirectory(folderTarget);
        File.WriteAllText(shortcutTarget, "shortcut placeholder");
        var appliedFolderIcon = temp.PathFor("icons", "folder.ico");
        var appliedShortcutIcon = temp.PathFor("icons", "shortcut.ico");
        TestIconFactory.WriteValidIcon(appliedFolderIcon);
        TestIconFactory.WriteValidIcon(appliedShortcutIcon);

        var store = new RestoreRecordStore(paths.RestoreStateFile);
        var applied = RestoreRecord.CreatePending(
            new TargetItem(TargetKind.Folder, folderTarget),
            appliedFolderIcon,
            new FolderRestoreSnapshot(
                DesktopIniExisted: false,
                new Dictionary<string, string?>(),
                FileAttributes.Directory,
                null)) with { Status = RestoreRecordStatus.Applied };
        var restored = RestoreRecord.CreatePending(
            new TargetItem(TargetKind.Shortcut, shortcutTarget),
            appliedShortcutIcon,
            new ShortcutRestoreSnapshot(null, 0)) with { Status = RestoreRecordStatus.Restored };
        Assert.True(store.Upsert(applied).Succeeded);
        Assert.True(store.Upsert(restored).Succeeded);

        var snapshot = new DashboardService().GetSnapshot(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(paths.LibraryRoot, snapshot.Value.IconLibraryRoot);
        Assert.Equal(2, snapshot.Value.CategoryCount);
        Assert.Equal(2, snapshot.Value.IconCount);
        Assert.Equal(0, snapshot.Value.CatalogWarningCount);
        Assert.Equal(2, snapshot.Value.RestoreRecordCount);
        Assert.Equal(1, snapshot.Value.RestorableRecordCount);
        Assert.Equal(0, snapshot.Value.MissingTargetRecordCount);
        Assert.Equal(0, snapshot.Value.MissingAppliedIconRecordCount);
        Assert.Contains(snapshot.Value.Categories, category => category.Name == "(root)" && category.IconCount == 1);
        Assert.Contains(snapshot.Value.Categories, category => category.Name == "Imported" && category.IconCount == 0);
        Assert.Contains(snapshot.Value.Categories, category => category.Name == "Work" && category.IconCount == 1);
        Assert.Equal(2, snapshot.Value.RecentRecords.Count);
        Assert.Contains(snapshot.Value.RecentRecords, record => record.Id == applied.Id && record.CanRestore);
    }

    [Fact]
    public void GetSnapshotFlagsAppliedRecordsWithMissingAssets()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var store = new RestoreRecordStore(paths.RestoreStateFile);
        var stale = RestoreRecord.CreatePending(
            new TargetItem(TargetKind.Folder, temp.PathFor("deleted-folder")),
            temp.PathFor("icons", "deleted-icon.ico"),
            new FolderRestoreSnapshot(
                DesktopIniExisted: false,
                new Dictionary<string, string?>(),
                FileAttributes.Directory,
                null)) with { Status = RestoreRecordStatus.Applied };
        Assert.True(store.Upsert(stale).Succeeded);

        var snapshot = new DashboardService().GetSnapshot(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(1, snapshot.Value.RestoreRecordCount);
        Assert.Equal(0, snapshot.Value.RestorableRecordCount);
        Assert.Equal(1, snapshot.Value.MissingTargetRecordCount);
        Assert.Equal(1, snapshot.Value.MissingAppliedIconRecordCount);
        var recent = Assert.Single(snapshot.Value.RecentRecords);
        Assert.Equal(stale.Id, recent.Id);
        Assert.False(recent.TargetExists);
        Assert.False(recent.AppliedIconExists);
        Assert.False(recent.CanRestore);
    }
}
