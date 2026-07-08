using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class RecentChangesServiceTests
{
    [Fact]
    public void GetRecentChangesEnablesHealthyAppliedRecord()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var record = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Applied);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);

        var snapshot = new RecentChangesService().GetRecentChanges(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(1, snapshot.Value.RestorableCount);
        Assert.Equal(0, snapshot.Value.WarningCount);
        Assert.Equal(0, snapshot.Value.DisabledCount);
        var item = Assert.Single(snapshot.Value.Items);
        Assert.True(item.IsRestoreEnabled);
        Assert.Equal(RecentChangeActionState.Enabled, item.RestoreActionState);
        Assert.Equal("Restorable", item.HealthText);
        Assert.Null(item.WarningText);
    }

    [Fact]
    public void GetRecentChangesEnablesWithWarningWhenAppliedIconIsMissing()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var record = CreateRecord(
            temp,
            TargetKind.Folder,
            "folder",
            "missing-icon.ico",
            RestoreRecordStatus.Applied,
            createIcon: false);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);

        var snapshot = new RecentChangesService().GetRecentChanges(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(1, snapshot.Value.RestorableCount);
        Assert.Equal(1, snapshot.Value.WarningCount);
        var item = Assert.Single(snapshot.Value.Items);
        Assert.True(item.IsRestoreEnabled);
        Assert.Equal(RecentChangeActionState.EnabledWithWarning, item.RestoreActionState);
        Assert.Equal("Applied icon missing", item.HealthText);
        Assert.NotNull(item.WarningText);
    }

    [Fact]
    public void GetRecentChangesDisablesMissingTargetAndAlreadyRestoredRecords()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var missingTarget = CreateRecord(
            temp,
            TargetKind.Folder,
            "missing-folder",
            "icon.ico",
            RestoreRecordStatus.Applied,
            createTarget: false);
        var restored = CreateRecord(temp, TargetKind.Folder, "restored-folder", "restored.ico", RestoreRecordStatus.Restored);
        var store = new RestoreRecordStore(paths.RestoreStateFile);
        Assert.True(store.Upsert(missingTarget).Succeeded);
        Assert.True(store.Upsert(restored).Succeeded);

        var snapshot = new RecentChangesService().GetRecentChanges(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(0, snapshot.Value.RestorableCount);
        Assert.Equal(2, snapshot.Value.DisabledCount);
        Assert.Contains(snapshot.Value.Items, item =>
            item.Id == missingTarget.Id &&
            item.RestoreActionState == RecentChangeActionState.Disabled &&
            item.HealthText == "Target missing");
        Assert.Contains(snapshot.Value.Items, item =>
            item.Id == restored.Id &&
            item.RestoreActionState == RecentChangeActionState.Disabled &&
            item.HealthText == "Already restored");
    }

    [Fact]
    public void GetRecentChangesHonorsHistoryFilter()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var restorable = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Applied);
        var restored = CreateRecord(temp, TargetKind.Folder, "restored-folder", "restored.ico", RestoreRecordStatus.Restored);
        var store = new RestoreRecordStore(paths.RestoreStateFile);
        Assert.True(store.Upsert(restorable).Succeeded);
        Assert.True(store.Upsert(restored).Succeeded);

        var snapshot = new RecentChangesService().GetRecentChanges(paths, RestoreHistoryFilter.Restorable);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        var item = Assert.Single(snapshot.Value!.Items);
        Assert.Equal(restorable.Id, item.Id);
        Assert.True(item.IsRestoreEnabled);
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
