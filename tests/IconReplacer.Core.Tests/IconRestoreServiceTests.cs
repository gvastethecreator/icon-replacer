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

    [Fact]
    public async Task ConcurrentRestoresOfOneRecordAllowExactlyOneRestore()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(sourceIcon);
        var apply = new IconApplyService().Apply(folder, sourceIcon, paths);
        Assert.True(apply.Succeeded, apply.Error.Message);
        var recordId = apply.Value!.RestoreRecord.Id;
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var restores = Enumerable.Range(0, 24).Select(async _ =>
        {
            await start.Task;
            return new IconRestoreService().Restore(recordId, paths);
        }).ToArray();

        start.SetResult();
        var results = await Task.WhenAll(restores);

        Assert.Single(results, result => result.Succeeded);
        Assert.All(
            results.Where(result => !result.Succeeded),
            result => Assert.Equal(ErrorCode.InvalidArgument, result.Error.Code));
        var stored = new RestoreRecordStore(paths.RestoreStateFile).Get(recordId);
        Assert.True(stored.Succeeded, stored.Error.Message);
        Assert.Equal(RestoreRecordStatus.Restored, stored.Value!.Status);
        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
    }

    [Fact]
    public void RestoreReappliesIconWhenRestoredStatusCannotBeSaved()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(sourceIcon);
        var apply = new IconApplyService().Apply(folder, sourceIcon, paths);
        Assert.True(apply.Succeeded, apply.Error.Message);

        OperationResult<IconRestoreResult> restore;
        using (File.Open(paths.RestoreStateFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            restore = new IconRestoreService().Restore(apply.Value!.RestoreRecord.Id, paths);
        }

        Assert.False(restore.Succeeded);
        Assert.NotEqual(ErrorCode.PartialFailure, restore.Error.Code);
        Assert.Contains("reapplied", restore.Error.Message, StringComparison.OrdinalIgnoreCase);
        var desktopIni = Path.Combine(folder, "desktop.ini");
        Assert.True(File.Exists(desktopIni));
        Assert.Contains(apply.Value.ImportedIcon.FullPath, File.ReadAllText(desktopIni), StringComparison.OrdinalIgnoreCase);
        var stored = new RestoreRecordStore(paths.RestoreStateFile).Get(apply.Value.RestoreRecord.Id);
        Assert.True(stored.Succeeded, stored.Error.Message);
        Assert.Equal(RestoreRecordStatus.Applied, stored.Value!.Status);
    }

    [WindowsOnlyFact]
    public void RestoreReappliesShortcutIconWhenRestoredStatusCannotBeSaved()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var target = temp.PathFor("target.exe");
        var shortcut = temp.PathFor("sample.lnk");
        var oldIcon = temp.PathFor("source", "old.ico");
        var newIcon = temp.PathFor("source", "new.ico");
        File.WriteAllBytes(target, [0]);
        TestIconFactory.WriteValidIcon(oldIcon);
        TestIconFactory.WriteValidIcon(newIcon);
        var shellLinkClient = new ShellLinkClient();
        var create = shellLinkClient.CreateOrUpdate(new ShellLinkInfo(
            shortcut,
            target,
            string.Empty,
            temp.FullPath,
            "Transactional shortcut",
            0,
            oldIcon,
            0));
        Assert.True(create.Succeeded, create.Error.Message);
        var shortcutService = new ShortcutIconService(
            shellLinkClient: shellLinkClient,
            changeNotifier: new NoOpExplorerChangeNotifier());
        var apply = new IconApplyService(shortcutIconService: shortcutService).Apply(shortcut, newIcon, paths);
        Assert.True(apply.Succeeded, apply.Error.Message);

        OperationResult<IconRestoreResult> restore;
        using (File.Open(paths.RestoreStateFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            restore = new IconRestoreService(shortcutIconService: shortcutService)
                .Restore(apply.Value!.RestoreRecord.Id, paths);
        }

        Assert.False(restore.Succeeded);
        Assert.NotEqual(ErrorCode.PartialFailure, restore.Error.Code);
        Assert.Contains("reapplied", restore.Error.Message, StringComparison.OrdinalIgnoreCase);
        var reapplied = shellLinkClient.Read(shortcut);
        Assert.True(reapplied.Succeeded, reapplied.Error.Message);
        Assert.Equal(apply.Value.ImportedIcon.FullPath, reapplied.Value!.IconPath);
        var stored = new RestoreRecordStore(paths.RestoreStateFile).Get(apply.Value.RestoreRecord.Id);
        Assert.True(stored.Succeeded, stored.Error.Message);
        Assert.Equal(RestoreRecordStatus.Applied, stored.Value!.Status);
    }

    [Fact]
    public void RestoreReportsBothFailuresWhenStatusSaveAndReapplyFail()
    {
        using var temp = new TempDirectory();
        using var notifier = new DesktopIniLockingNotifier();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        var desktopIni = Path.Combine(folder, "desktop.ini");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        Directory.CreateDirectory(folder);
        File.WriteAllText(desktopIni, "[Other]\r\nKeep=1\r\n");
        TestIconFactory.WriteValidIcon(sourceIcon);
        var apply = new IconApplyService().Apply(folder, sourceIcon, paths);
        Assert.True(apply.Succeeded, apply.Error.Message);
        var restoreService = new IconRestoreService(
            folderIconService: new FolderIconService(changeNotifier: notifier));

        OperationResult<IconRestoreResult> restore;
        using (File.Open(paths.RestoreStateFile, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            restore = restoreService.Restore(apply.Value!.RestoreRecord.Id, paths);
        }

        Assert.False(restore.Succeeded);
        Assert.Equal(ErrorCode.PartialFailure, restore.Error.Code);
        Assert.Contains("could not be reapplied", restore.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Save failed:", restore.Error.Detail, StringComparison.Ordinal);
        Assert.Contains("Reapply failed:", restore.Error.Detail, StringComparison.Ordinal);
    }

    private sealed class DesktopIniLockingNotifier : IExplorerChangeNotifier, IDisposable
    {
        private FileStream? _desktopIniLock;

        public void NotifyUpdated(string path)
        {
            if (_desktopIniLock is not null || !Directory.Exists(path))
            {
                return;
            }

            _desktopIniLock = File.Open(
                Path.Combine(path, "desktop.ini"),
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
        }

        public void Dispose()
        {
            _desktopIniLock?.Dispose();
        }
    }
}
