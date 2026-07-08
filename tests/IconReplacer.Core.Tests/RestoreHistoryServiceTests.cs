using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class RestoreHistoryServiceTests
{
    [Fact]
    public void GetHistorySummarizesAllRecords()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var applied = CreateRecord(temp, TargetKind.Folder, "applied-folder", "applied.ico", RestoreRecordStatus.Applied);
        var restored = CreateRecord(temp, TargetKind.Folder, "restored-folder", "restored.ico", RestoreRecordStatus.Restored);
        var stale = CreateRecord(temp, TargetKind.Shortcut, "missing-shortcut.lnk", "missing-icon.ico", RestoreRecordStatus.Applied, createTarget: false, createIcon: false);
        var store = new RestoreRecordStore(paths.RestoreStateFile);
        Assert.True(store.Upsert(applied).Succeeded);
        Assert.True(store.Upsert(restored).Succeeded);
        Assert.True(store.Upsert(stale).Succeeded);

        var history = new RestoreHistoryService().GetHistory(paths);

        Assert.True(history.Succeeded, history.Error.Message);
        Assert.NotNull(history.Value);
        Assert.Equal(3, history.Value.TotalCount);
        Assert.Equal(2, history.Value.AppliedCount);
        Assert.Equal(1, history.Value.RestoredCount);
        Assert.Equal(1, history.Value.RestorableCount);
        Assert.Equal(1, history.Value.StaleCount);
        Assert.Equal(3, history.Value.Records.Count);
    }

    [Fact]
    public void GetHistoryFiltersRestorableRecords()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var restorable = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Applied);
        var restored = CreateRecord(temp, TargetKind.Folder, "restored-folder", "restored.ico", RestoreRecordStatus.Restored);
        var store = new RestoreRecordStore(paths.RestoreStateFile);
        Assert.True(store.Upsert(restorable).Succeeded);
        Assert.True(store.Upsert(restored).Succeeded);

        var history = new RestoreHistoryService().GetHistory(paths, RestoreHistoryFilter.Restorable);

        Assert.True(history.Succeeded, history.Error.Message);
        var record = Assert.Single(history.Value!.Records);
        Assert.Equal(restorable.Id, record.Id);
        Assert.True(record.CanRestore);
    }

    [Fact]
    public void GetHistoryFiltersStaleRecords()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var healthy = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Applied);
        var stale = CreateRecord(temp, TargetKind.Folder, "missing-folder", "missing-icon.ico", RestoreRecordStatus.Restored, createTarget: false, createIcon: false);
        var store = new RestoreRecordStore(paths.RestoreStateFile);
        Assert.True(store.Upsert(healthy).Succeeded);
        Assert.True(store.Upsert(stale).Succeeded);

        var history = new RestoreHistoryService().GetHistory(paths, RestoreHistoryFilter.Stale);

        Assert.True(history.Succeeded, history.Error.Message);
        var record = Assert.Single(history.Value!.Records);
        Assert.Equal(stale.Id, record.Id);
        Assert.False(record.TargetExists);
        Assert.False(record.AppliedIconExists);
    }

    private static RestoreRecord CreateRecord(
        TempDirectory temp,
        TargetKind targetKind,
        string targetName,
        string iconName,
        RestoreRecordStatus status,
        bool createTarget = true,
        bool createIcon = true)
    {
        var targetPath = temp.PathFor("targets", targetName);
        if (createTarget)
        {
            if (targetKind == TargetKind.Folder)
            {
                Directory.CreateDirectory(targetPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.WriteAllText(targetPath, "shortcut placeholder");
            }
        }

        var iconPath = temp.PathFor("icons", iconName);
        if (createIcon)
        {
            TestIconFactory.WriteValidIcon(iconPath);
        }

        return RestoreRecord.CreatePending(
            new TargetItem(targetKind, targetPath),
            iconPath,
            targetKind == TargetKind.Folder
                ? new FolderRestoreSnapshot(
                    DesktopIniExisted: false,
                    new Dictionary<string, string?>(),
                    FileAttributes.Directory,
                    null)
                : new ShortcutRestoreSnapshot(null, 0)) with { Status = status };
    }
}
