using System.Text;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class FolderIconServiceTests
{
    [WindowsOnlyFact]
    public void ApplyAndRestoreSupportsDirectorySymbolicLink()
    {
        AssertApplyAndRestoreSupportsDirectoryLink(DirectoryLinkKind.SymbolicLink);
    }

    [WindowsOnlyFact]
    public void ApplyAndRestoreSupportsDirectoryJunction()
    {
        AssertApplyAndRestoreSupportsDirectoryLink(DirectoryLinkKind.Junction);
    }

    [WindowsOnlyFact]
    public void ApplyRejectsRemoteDirectoryLinkBeforeImport()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var link = temp.PathFor("remote-link");
        var icon = temp.PathFor("icons", "new.ico");
        TestIconFactory.WriteValidIcon(icon);

        try
        {
            ReparsePointTestHelper.CreateDirectoryLink(
                DirectoryLinkKind.SymbolicLink,
                link,
                @"\\icon-replacer.invalid\share");

            var result = new FolderIconService().Apply(link, icon, paths);

            Assert.False(result.Succeeded);
            Assert.Equal(ErrorCode.RemotePathUnsupported, result.Error.Code);
            Assert.False(Directory.Exists(paths.ImportedRoot));
            Assert.False(File.Exists(paths.RestoreStateFile));
        }
        finally
        {
            ReparsePointTestHelper.DeleteDirectoryLink(link);
        }
    }

    [Fact]
    public void ApplyCreatesDesktopIniAndRestoreDeletesItWhenItDidNotExistBefore()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target");
        Directory.CreateDirectory(folder);
        var icon = temp.PathFor("icons", "folder.ico");
        TestIconFactory.WriteValidIcon(icon);
        var notifier = new RecordingExplorerChangeNotifier();
        var service = new FolderIconService(changeNotifier: notifier);

        var apply = service.Apply(folder, icon, paths);

        Assert.True(apply.Succeeded, apply.Error.Message);
        Assert.NotNull(apply.Value);
        Assert.True(File.Exists(apply.Value.DesktopIniPath));
        Assert.Contains("IconResource=", File.ReadAllText(apply.Value.DesktopIniPath, Encoding.Unicode));
        Assert.Contains(paths.ImportedRoot, File.ReadAllText(apply.Value.DesktopIniPath, Encoding.Unicode));
        Assert.True((File.GetAttributes(apply.Value.DesktopIniPath) & FileAttributes.Hidden) == FileAttributes.Hidden);
        Assert.True((File.GetAttributes(apply.Value.DesktopIniPath) & FileAttributes.System) == FileAttributes.System);
        Assert.True((File.GetAttributes(folder) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
        Assert.Contains(folder, notifier.UpdatedPaths);

        var restore = service.Restore(apply.Value.RestoreRecord);

        Assert.True(restore.Succeeded, restore.Error.Message);
        Assert.False(File.Exists(apply.Value.DesktopIniPath));
        Assert.Equal([folder, folder], notifier.UpdatedPaths);
        Assert.False((File.GetAttributes(folder) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly);
    }

    [Fact]
    public void ApplyMergesExistingDesktopIniAndRestoreRestoresPreviousIconKeys()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target");
        Directory.CreateDirectory(folder);
        var desktopIni = Path.Combine(folder, "desktop.ini");
        File.WriteAllText(
            desktopIni,
            """
            [.ShellClassInfo]
            InfoTip=Keep me
            IconFile=old.ico
            IconIndex=7

            [Other]
            Value=yes
            """,
            Encoding.Unicode);
        var previousDesktopAttributes = File.GetAttributes(desktopIni);
        var previousFolderAttributes = File.GetAttributes(folder);
        var icon = temp.PathFor("icons", "new.ico");
        TestIconFactory.WriteValidIcon(icon);
        var service = new FolderIconService();

        var apply = service.Apply(folder, icon, paths);

        Assert.True(apply.Succeeded, apply.Error.Message);
        var afterApply = DesktopIniDocument.Load(desktopIni);
        Assert.Equal("Keep me", afterApply.GetValue(".ShellClassInfo", "InfoTip"));
        Assert.Equal("yes", afterApply.GetValue("Other", "Value"));
        Assert.NotNull(afterApply.GetValue(".ShellClassInfo", "IconResource"));
        Assert.Null(afterApply.GetValue(".ShellClassInfo", "IconFile"));
        Assert.Null(afterApply.GetValue(".ShellClassInfo", "IconIndex"));

        var restore = service.Restore(apply.Value!.RestoreRecord);

        Assert.True(restore.Succeeded, restore.Error.Message);
        var afterRestore = DesktopIniDocument.Load(desktopIni);
        Assert.Equal("Keep me", afterRestore.GetValue(".ShellClassInfo", "InfoTip"));
        Assert.Equal("yes", afterRestore.GetValue("Other", "Value"));
        Assert.Equal("old.ico", afterRestore.GetValue(".ShellClassInfo", "IconFile"));
        Assert.Equal("7", afterRestore.GetValue(".ShellClassInfo", "IconIndex"));
        Assert.Null(afterRestore.GetValue(".ShellClassInfo", "IconResource"));
        Assert.Equal(previousDesktopAttributes, File.GetAttributes(desktopIni));
        Assert.Equal(previousFolderAttributes, File.GetAttributes(folder));
    }

    [Fact]
    public void ApplyRejectsMissingFolderBeforeImportingIcon()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var icon = temp.PathFor("icons", "folder.ico");
        TestIconFactory.WriteValidIcon(icon);
        var service = new FolderIconService();

        var apply = service.Apply(temp.PathFor("missing"), icon, paths);

        Assert.False(apply.Succeeded);
        Assert.Equal(ErrorCode.PathNotFound, apply.Error.Code);
        Assert.False(Directory.Exists(paths.ImportedRoot));
    }

    [Fact]
    public void RestoreRejectsNonFolderRestoreRecord()
    {
        var record = RestoreRecord.CreatePending(
            new TargetItem(TargetKind.Shortcut, Path.GetFullPath("sample.lnk")),
            Path.GetFullPath("icon.ico"),
            new ShortcutRestoreSnapshot("old.ico", 0));

        var restore = new FolderIconService().Restore(record);

        Assert.False(restore.Succeeded);
        Assert.Equal(ErrorCode.UnsupportedTarget, restore.Error.Code);
    }

    [Fact]
    public void ApplyPermissionFailureRollsBackDesktopIniAndFolderAttributes()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target");
        Directory.CreateDirectory(folder);
        var desktopIni = Path.Combine(folder, "desktop.ini");
        File.WriteAllText(
            desktopIni,
            """
            [.ShellClassInfo]
            InfoTip=Keep me
            IconFile=old.ico
            IconIndex=7
            """,
            Encoding.Unicode);
        var originalContents = File.ReadAllBytes(desktopIni);
        var originalDesktopIniAttributes = File.GetAttributes(desktopIni);
        var originalFolderAttributes = File.GetAttributes(folder);
        var icon = temp.PathFor("icons", "new.ico");
        TestIconFactory.WriteValidIcon(icon);
        var service = new FolderIconService(changeNotifier: new PermissionDeniedChangeNotifier());

        var apply = service.Apply(folder, icon, paths);

        Assert.False(apply.Succeeded);
        Assert.Equal(ErrorCode.PermissionDenied, apply.Error.Code);
        Assert.Contains("left unchanged", apply.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(originalContents, File.ReadAllBytes(desktopIni));
        Assert.Equal(originalDesktopIniAttributes, File.GetAttributes(desktopIni));
        Assert.Equal(originalFolderAttributes, File.GetAttributes(folder));
    }

    [Fact]
    public void RestorePermissionFailureRollsBackToAppliedState()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target");
        Directory.CreateDirectory(folder);
        var icon = temp.PathFor("icons", "new.ico");
        TestIconFactory.WriteValidIcon(icon);
        var apply = new FolderIconService(changeNotifier: new NoOpExplorerChangeNotifier())
            .Apply(folder, icon, paths);
        Assert.True(apply.Succeeded, apply.Error.Message);
        var desktopIni = apply.Value!.DesktopIniPath;
        var appliedContents = File.ReadAllBytes(desktopIni);
        var appliedDesktopIniAttributes = File.GetAttributes(desktopIni);
        var appliedFolderAttributes = File.GetAttributes(folder);
        var service = new FolderIconService(changeNotifier: new PermissionDeniedChangeNotifier());

        var restore = service.Restore(apply.Value.RestoreRecord);

        Assert.False(restore.Succeeded);
        Assert.Equal(ErrorCode.PermissionDenied, restore.Error.Code);
        Assert.Contains("left unchanged", restore.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(appliedContents, File.ReadAllBytes(desktopIni));
        Assert.Equal(appliedDesktopIniAttributes, File.GetAttributes(desktopIni));
        Assert.Equal(appliedFolderAttributes, File.GetAttributes(folder));
    }

    [Fact]
    public void ApplyLockedDesktopIniReturnsFailureWithoutChangingTarget()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target");
        Directory.CreateDirectory(folder);
        var desktopIni = Path.Combine(folder, "desktop.ini");
        File.WriteAllText(desktopIni, "[.ShellClassInfo]\r\nInfoTip=Keep me\r\n", Encoding.Unicode);
        var originalContents = File.ReadAllBytes(desktopIni);
        var originalFolderAttributes = File.GetAttributes(folder);
        var icon = temp.PathFor("icons", "new.ico");
        TestIconFactory.WriteValidIcon(icon);
        using var lockStream = new FileStream(desktopIni, FileMode.Open, FileAccess.Read, FileShare.None);

        var apply = new FolderIconService().Apply(folder, icon, paths);

        Assert.False(apply.Succeeded);
        Assert.Equal(ErrorCode.PartialFailure, apply.Error.Code);
        Assert.Contains("left unchanged", apply.Error.Message, StringComparison.OrdinalIgnoreCase);
        lockStream.Position = 0;
        var currentContents = new byte[lockStream.Length];
        _ = lockStream.Read(currentContents, 0, currentContents.Length);
        Assert.Equal(originalContents, currentContents);
        Assert.Equal(originalFolderAttributes, File.GetAttributes(folder));
    }

    private sealed class PermissionDeniedChangeNotifier : IExplorerChangeNotifier
    {
        public void NotifyUpdated(string path)
        {
            throw new UnauthorizedAccessException($"Explorer refresh denied for {path}.");
        }
    }

    private static void AssertApplyAndRestoreSupportsDirectoryLink(DirectoryLinkKind kind)
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var target = temp.PathFor("target");
        var link = temp.PathFor(kind.ToString());
        var icon = temp.PathFor("icons", "new.ico");
        Directory.CreateDirectory(target);
        TestIconFactory.WriteValidIcon(icon);
        var notifier = new RecordingExplorerChangeNotifier();
        var service = new FolderIconService(changeNotifier: notifier);

        try
        {
            ReparsePointTestHelper.CreateDirectoryLink(kind, link, target);

            var apply = service.Apply(link, icon, paths);

            Assert.True(apply.Succeeded, apply.Error.Message);
            Assert.Equal(link, apply.Value!.RestoreRecord.Target.FullPath);
            Assert.True(File.Exists(Path.Combine(target, "desktop.ini")));
            Assert.True((File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0);

            var restore = service.Restore(apply.Value.RestoreRecord);

            Assert.True(restore.Succeeded, restore.Error.Message);
            Assert.False(File.Exists(Path.Combine(target, "desktop.ini")));
            Assert.True(Directory.Exists(link));
            Assert.Equal([link, link], notifier.UpdatedPaths);
        }
        finally
        {
            ReparsePointTestHelper.DeleteDirectoryLink(link);
        }
    }
}
