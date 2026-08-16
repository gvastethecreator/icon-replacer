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

    [Fact]
    public async Task ConcurrentAppliesToOneFolderRemainFullyRestorable()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        Directory.CreateDirectory(folder);
        var icons = Enumerable.Range(1, 24).Select(index =>
        {
            var icon = temp.PathFor("source", $"icon-{index}.ico");
            TestIconFactory.WriteValidIcon(icon);
            var contents = File.ReadAllBytes(icon);
            contents[^1] = (byte)index;
            File.WriteAllBytes(icon, contents);
            return icon;
        }).ToArray();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var applies = icons.Select(async icon =>
        {
            await start.Task;
            return new IconApplyService().Apply(folder, icon, paths);
        }).ToArray();

        start.SetResult();
        var applyResults = await Task.WhenAll(applies);

        Assert.All(applyResults, result => Assert.True(result.Succeeded, result.Error.Message));
        var records = new RestoreRecordStore(paths.RestoreStateFile).List();
        Assert.True(records.Succeeded, records.Error.Message);
        Assert.Equal(icons.Length, records.Value!.Count);

        foreach (var record in records.Value)
        {
            var restore = new IconRestoreService().Restore(record.Id, paths);
            Assert.True(restore.Succeeded, restore.Error.Message);
        }

        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
    }

    [Fact]
    public void ApplyRollsBackTargetWhenRestoreRecordCannotBeSaved()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(paths.RestoreStateFile);
        TestIconFactory.WriteValidIcon(sourceIcon);
        var originalAttributes = File.GetAttributes(folder);

        var result = new IconApplyService().Apply(folder, sourceIcon, paths);

        Assert.False(result.Succeeded);
        Assert.NotEqual(ErrorCode.PartialFailure, result.Error.Code);
        Assert.Contains("restored", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
        Assert.Equal(originalAttributes, File.GetAttributes(folder));
    }

    [Fact]
    public void ApplyReportsBothFailuresWhenPersistenceAndRollbackFail()
    {
        using var temp = new TempDirectory();
        using var notifier = new DesktopIniLockingNotifier();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(paths.RestoreStateFile);
        TestIconFactory.WriteValidIcon(sourceIcon);
        var service = new IconApplyService(new FolderIconService(changeNotifier: notifier));

        var result = service.Apply(folder, sourceIcon, paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.PartialFailure, result.Error.Code);
        Assert.Contains("automatic rollback failed", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Save failed:", result.Error.Detail, StringComparison.Ordinal);
        Assert.Contains("Rollback failed:", result.Error.Detail, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(folder, "desktop.ini")));
    }

    [WindowsOnlyFact]
    public void ApplyRollsBackShortcutWhenRestoreRecordCannotBeSaved()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var target = temp.PathFor("target.exe");
        var shortcut = temp.PathFor("sample.lnk");
        var oldIcon = temp.PathFor("source", "old.ico");
        var newIcon = temp.PathFor("source", "new.ico");
        File.WriteAllBytes(target, [0]);
        Directory.CreateDirectory(paths.RestoreStateFile);
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
        var service = new IconApplyService(
            shortcutIconService: new ShortcutIconService(
                shellLinkClient: shellLinkClient,
                changeNotifier: new NoOpExplorerChangeNotifier()));

        var result = service.Apply(shortcut, newIcon, paths);

        Assert.False(result.Succeeded);
        Assert.NotEqual(ErrorCode.PartialFailure, result.Error.Code);
        Assert.Contains("restored", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        var restored = shellLinkClient.Read(shortcut);
        Assert.True(restored.Succeeded, restored.Error.Message);
        Assert.Equal(oldIcon, restored.Value!.IconPath);
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
