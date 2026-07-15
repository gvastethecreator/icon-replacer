using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class ShortcutIconServiceTests
{
    [WindowsOnlyFact]
    public void ApplyChangesShortcutIconAndRestoreRestoresPreviousIconOnly()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var target = temp.PathFor("target.exe");
        File.WriteAllBytes(target, [0]);
        var workingDirectory = temp.PathFor("work");
        Directory.CreateDirectory(workingDirectory);
        var oldIcon = temp.PathFor("icons", "old.ico");
        var newIcon = temp.PathFor("icons", "new.ico");
        TestIconFactory.WriteValidIcon(oldIcon);
        TestIconFactory.WriteValidIcon(newIcon);
        var shortcut = temp.PathFor("sample.lnk");
        var shellLinkClient = new ShellLinkClient();
        var create = shellLinkClient.CreateOrUpdate(new ShellLinkInfo(
            shortcut,
            target,
            "--sample",
            workingDirectory,
            "Sample shortcut",
            65,
            oldIcon,
            0));
        Assert.True(create.Succeeded, create.Error.Message);
        var notifier = new RecordingExplorerChangeNotifier();
        var service = new ShortcutIconService(shellLinkClient: shellLinkClient, changeNotifier: notifier);

        var apply = service.Apply(shortcut, newIcon, paths);

        Assert.True(apply.Succeeded, apply.Error.Message);
        Assert.NotNull(apply.Value);
        Assert.StartsWith(paths.ImportedRoot, apply.Value.ImportedIcon.FullPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(apply.Value.ImportedIcon.FullPath, apply.Value.Shortcut.IconPath);
        Assert.Equal(target, apply.Value.Shortcut.TargetPath);
        Assert.Equal("--sample", apply.Value.Shortcut.Arguments);
        Assert.Equal(workingDirectory, apply.Value.Shortcut.WorkingDirectory);
        Assert.Equal("Sample shortcut", apply.Value.Shortcut.Description);
        Assert.Equal(65, apply.Value.Shortcut.Hotkey);
        Assert.Equal([shortcut], notifier.UpdatedPaths);

        var restore = service.Restore(apply.Value.RestoreRecord);

        Assert.True(restore.Succeeded, restore.Error.Message);
        Assert.NotNull(restore.Value);
        Assert.Equal(oldIcon, restore.Value.Shortcut.IconPath);
        Assert.Equal(target, restore.Value.Shortcut.TargetPath);
        Assert.Equal("--sample", restore.Value.Shortcut.Arguments);
        Assert.Equal(workingDirectory, restore.Value.Shortcut.WorkingDirectory);
        Assert.Equal("Sample shortcut", restore.Value.Shortcut.Description);
        Assert.Equal(65, restore.Value.Shortcut.Hotkey);
        Assert.Equal([shortcut, shortcut], notifier.UpdatedPaths);
    }

    [WindowsOnlyFact]
    public void ApplyRejectsMissingShortcutBeforeImportingIcon()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var icon = temp.PathFor("icons", "new.ico");
        TestIconFactory.WriteValidIcon(icon);
        var service = new ShortcutIconService();

        var apply = service.Apply(temp.PathFor("missing.lnk"), icon, paths);

        Assert.False(apply.Succeeded);
        Assert.Equal(ErrorCode.PathNotFound, apply.Error.Code);
        Assert.False(Directory.Exists(paths.ImportedRoot));
    }

    [Fact]
    public void RestoreRejectsNonShortcutRestoreRecord()
    {
        var record = RestoreRecord.CreatePending(
            new TargetItem(TargetKind.Folder, Path.GetFullPath("sample")),
            Path.GetFullPath("icon.ico"),
            new FolderRestoreSnapshot(
                DesktopIniExisted: false,
                new Dictionary<string, string?>(),
                FileAttributes.Directory,
                null));

        var restore = new ShortcutIconService().Restore(record);

        Assert.False(restore.Succeeded);
        Assert.Equal(ErrorCode.UnsupportedTarget, restore.Error.Code);
    }

    [WindowsOnlyFact]
    public void ApplyPermissionFailureRollsBackShortcutFile()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var target = temp.PathFor("target.exe");
        File.WriteAllBytes(target, [0]);
        var oldIcon = temp.PathFor("icons", "old.ico");
        var newIcon = temp.PathFor("icons", "new.ico");
        TestIconFactory.WriteValidIcon(oldIcon);
        TestIconFactory.WriteValidIcon(newIcon);
        var shortcut = temp.PathFor("sample.lnk");
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
        var originalContents = File.ReadAllBytes(shortcut);
        var service = new ShortcutIconService(
            shellLinkClient: shellLinkClient,
            changeNotifier: new PermissionDeniedChangeNotifier());

        var apply = service.Apply(shortcut, newIcon, paths);

        Assert.False(apply.Succeeded);
        Assert.Equal(ErrorCode.PermissionDenied, apply.Error.Code);
        Assert.Contains("left unchanged", apply.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(originalContents, File.ReadAllBytes(shortcut));
        var after = shellLinkClient.Read(shortcut);
        Assert.True(after.Succeeded, after.Error.Message);
        Assert.Equal(oldIcon, after.Value!.IconPath);
    }

    [WindowsOnlyFact]
    public void RestorePermissionFailureRollsBackToAppliedShortcutFile()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var target = temp.PathFor("target.exe");
        File.WriteAllBytes(target, [0]);
        var oldIcon = temp.PathFor("icons", "old.ico");
        var newIcon = temp.PathFor("icons", "new.ico");
        TestIconFactory.WriteValidIcon(oldIcon);
        TestIconFactory.WriteValidIcon(newIcon);
        var shortcut = temp.PathFor("sample.lnk");
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
        var apply = new ShortcutIconService(
            shellLinkClient: shellLinkClient,
            changeNotifier: new NoOpExplorerChangeNotifier()).Apply(shortcut, newIcon, paths);
        Assert.True(apply.Succeeded, apply.Error.Message);
        var appliedContents = File.ReadAllBytes(shortcut);
        var service = new ShortcutIconService(
            shellLinkClient: shellLinkClient,
            changeNotifier: new PermissionDeniedChangeNotifier());

        var restore = service.Restore(apply.Value!.RestoreRecord);

        Assert.False(restore.Succeeded);
        Assert.Equal(ErrorCode.PermissionDenied, restore.Error.Code);
        Assert.Contains("left unchanged", restore.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(appliedContents, File.ReadAllBytes(shortcut));
        var after = shellLinkClient.Read(shortcut);
        Assert.True(after.Succeeded, after.Error.Message);
        Assert.Equal(apply.Value.ImportedIcon.FullPath, after.Value!.IconPath);
    }

    private sealed class PermissionDeniedChangeNotifier : IExplorerChangeNotifier
    {
        public void NotifyUpdated(string path)
        {
            throw new UnauthorizedAccessException($"Explorer refresh denied for {path}.");
        }
    }
}
