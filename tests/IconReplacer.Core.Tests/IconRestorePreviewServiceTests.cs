using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconRestorePreviewServiceTests
{
    [Fact]
    public void PreviewRestoreAllowsAppliedFolderWithoutMutatingRecord()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(sourceIcon);
        var apply = new IconApplyService().Apply(folder, sourceIcon, paths);
        Assert.True(apply.Succeeded, apply.Error.Message);

        var preview = new IconRestorePreviewService().PreviewRestore(apply.Value!.RestoreRecord.Id, paths);

        Assert.True(preview.Succeeded, preview.Error.Message);
        Assert.NotNull(preview.Value);
        Assert.True(preview.Value.CanRestore);
        Assert.Equal(ErrorCode.None, preview.Value.Error.Code);
        Assert.Equal(TargetKind.Folder, preview.Value.Record.TargetKind);
        Assert.Equal(RestoreRecordStatus.Applied, preview.Value.Record.Status);
        Assert.Null(preview.Value.WarningText);
        Assert.Contains("desktop.ini", preview.Value.PreviousStateDetail);
        Assert.Contains("desktop.ini", preview.Value.RestoreActionDetail);

        var stored = new RestoreRecordStore(paths.RestoreStateFile).Get(apply.Value.RestoreRecord.Id);
        Assert.True(stored.Succeeded, stored.Error.Message);
        Assert.Equal(RestoreRecordStatus.Applied, stored.Value!.Status);
    }

    [Fact]
    public void PreviewRestoreWarnsWhenAppliedIconIsMissingButTargetCanRestore()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var record = CreateRecord(temp, TargetKind.Folder, "folder", "missing.ico", RestoreRecordStatus.Applied, createIcon: false);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);

        var preview = new IconRestorePreviewService().PreviewRestore(record.Id, paths);

        Assert.True(preview.Succeeded, preview.Error.Message);
        Assert.NotNull(preview.Value);
        Assert.True(preview.Value.CanRestore);
        Assert.Equal(ErrorCode.None, preview.Value.Error.Code);
        Assert.False(preview.Value.Record.AppliedIconExists);
        Assert.Contains("applied icon file is missing", preview.Value.WarningText);
    }

    [Fact]
    public void PreviewRestoreDisablesAlreadyRestoredRecord()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var record = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Restored);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);

        var preview = new IconRestorePreviewService().PreviewRestore(record.Id, paths);

        Assert.True(preview.Succeeded, preview.Error.Message);
        Assert.NotNull(preview.Value);
        Assert.False(preview.Value.CanRestore);
        Assert.Equal(ErrorCode.InvalidArgument, preview.Value.Error.Code);
        Assert.Contains("disabled", preview.Value.RestoreActionDetail);
    }

    [Fact]
    public void PreviewRestoreReportsMissingTarget()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var record = CreateRecord(temp, TargetKind.Folder, "missing-folder", "icon.ico", RestoreRecordStatus.Applied, createTarget: false);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);

        var preview = new IconRestorePreviewService().PreviewRestore(record.Id, paths);

        Assert.True(preview.Succeeded, preview.Error.Message);
        Assert.NotNull(preview.Value);
        Assert.False(preview.Value.CanRestore);
        Assert.Equal(ErrorCode.PathNotFound, preview.Value.Error.Code);
        Assert.False(preview.Value.Record.TargetExists);
    }

    [Fact]
    public void PreviewRestoreMissingRecordReturnsPathNotFound()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var preview = new IconRestorePreviewService().PreviewRestore(Guid.NewGuid(), paths);

        Assert.False(preview.Succeeded);
        Assert.Equal(ErrorCode.PathNotFound, preview.Error.Code);
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
