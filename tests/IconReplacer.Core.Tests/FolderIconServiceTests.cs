using System.Text;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class FolderIconServiceTests
{
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
}
