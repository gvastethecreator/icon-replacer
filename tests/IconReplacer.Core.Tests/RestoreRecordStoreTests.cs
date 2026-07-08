using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class RestoreRecordStoreTests
{
    [Fact]
    public void ListReturnsEmptyWhenStateFileDoesNotExist()
    {
        using var temp = new TempDirectory();
        var store = new RestoreRecordStore(temp.PathFor("state", "missing.json"));

        var list = store.List();

        Assert.True(list.Succeeded, list.Error.Message);
        Assert.Empty(list.Value!);
    }

    [Fact]
    public void UpsertAndGetRoundTripsFolderRestoreRecord()
    {
        using var temp = new TempDirectory();
        var store = new RestoreRecordStore(temp.PathFor("state", "state.json"));
        var target = new TargetItem(TargetKind.Folder, temp.PathFor("target"));
        var record = RestoreRecord.CreatePending(
            target,
            temp.PathFor("icons", "folder.ico"),
            new FolderRestoreSnapshot(
                DesktopIniExisted: true,
                new Dictionary<string, string?> { ["IconResource"] = "old.ico,0", ["IconFile"] = null },
                FileAttributes.Directory | FileAttributes.ReadOnly,
                FileAttributes.Hidden | FileAttributes.System));

        var save = store.Upsert(record with { Status = RestoreRecordStatus.Applied });
        var read = store.Get(record.Id);

        Assert.True(save.Succeeded, save.Error.Message);
        Assert.True(read.Succeeded, read.Error.Message);
        Assert.NotNull(read.Value);
        Assert.Equal(RestoreRecordStatus.Applied, read.Value.Status);
        Assert.Equal(target, read.Value.Target);
        var snapshot = Assert.IsType<FolderRestoreSnapshot>(read.Value.PreviousState);
        Assert.True(snapshot.DesktopIniExisted);
        Assert.Equal("old.ico,0", snapshot.PreviousShellClassInfoValues["IconResource"]);
        Assert.Null(snapshot.PreviousShellClassInfoValues["IconFile"]);
        Assert.Equal(FileAttributes.Directory | FileAttributes.ReadOnly, snapshot.PreviousFolderAttributes);
        Assert.Equal(FileAttributes.Hidden | FileAttributes.System, snapshot.PreviousDesktopIniAttributes);
    }

    [Fact]
    public void UpsertAndGetRoundTripsShortcutRestoreRecord()
    {
        using var temp = new TempDirectory();
        var store = new RestoreRecordStore(temp.PathFor("state", "state.json"));
        var target = new TargetItem(TargetKind.Shortcut, temp.PathFor("target.lnk"));
        var record = RestoreRecord.CreatePending(
            target,
            temp.PathFor("icons", "shortcut.ico"),
            new ShortcutRestoreSnapshot(temp.PathFor("icons", "old.ico"), 2));

        var save = store.Upsert(record with { Status = RestoreRecordStatus.Applied });
        var read = store.Get(record.Id);

        Assert.True(save.Succeeded, save.Error.Message);
        Assert.True(read.Succeeded, read.Error.Message);
        Assert.NotNull(read.Value);
        Assert.Equal(RestoreRecordStatus.Applied, read.Value.Status);
        Assert.Equal(target, read.Value.Target);
        var snapshot = Assert.IsType<ShortcutRestoreSnapshot>(read.Value.PreviousState);
        Assert.Equal(temp.PathFor("icons", "old.ico"), snapshot.PreviousIconPath);
        Assert.Equal(2, snapshot.PreviousIconIndex);
    }

    [Fact]
    public void UpsertReplacesExistingRecordById()
    {
        using var temp = new TempDirectory();
        var store = new RestoreRecordStore(temp.PathFor("state", "state.json"));
        var target = new TargetItem(TargetKind.Shortcut, temp.PathFor("target.lnk"));
        var record = RestoreRecord.CreatePending(
            target,
            temp.PathFor("icons", "shortcut.ico"),
            new ShortcutRestoreSnapshot(null, 0));

        var firstSave = store.Upsert(record with { Status = RestoreRecordStatus.Applied });
        var secondSave = store.Upsert(record with { Status = RestoreRecordStatus.Restored });
        var list = store.List();

        Assert.True(firstSave.Succeeded, firstSave.Error.Message);
        Assert.True(secondSave.Succeeded, secondSave.Error.Message);
        Assert.True(list.Succeeded, list.Error.Message);
        Assert.Single(list.Value!);
        Assert.Equal(RestoreRecordStatus.Restored, list.Value![0].Status);
    }

    [Fact]
    public void ListReturnsNewestRecordsFirst()
    {
        using var temp = new TempDirectory();
        var store = new RestoreRecordStore(temp.PathFor("state", "state.json"));
        var older = RestoreRecord.CreatePending(
            new TargetItem(TargetKind.Shortcut, temp.PathFor("older.lnk")),
            temp.PathFor("icons", "older.ico"),
            new ShortcutRestoreSnapshot(null, 0));
        var newer = RestoreRecord.CreatePending(
            new TargetItem(TargetKind.Shortcut, temp.PathFor("newer.lnk")),
            temp.PathFor("icons", "newer.ico"),
            new ShortcutRestoreSnapshot(null, 0));

        var olderSave = store.Upsert(older with { CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10) });
        var newerSave = store.Upsert(newer with { CreatedAt = DateTimeOffset.UtcNow });
        var list = store.List();

        Assert.True(olderSave.Succeeded, olderSave.Error.Message);
        Assert.True(newerSave.Succeeded, newerSave.Error.Message);
        Assert.True(list.Succeeded, list.Error.Message);
        Assert.Equal(newer.Id, list.Value![0].Id);
        Assert.Equal(older.Id, list.Value![1].Id);
    }
}
