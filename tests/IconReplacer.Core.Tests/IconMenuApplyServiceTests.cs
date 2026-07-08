using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconMenuApplyServiceTests
{
    [Fact]
    public void ApplyMenuIconAppliesCatalogIconAndStoresRestoreRecord()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var target = temp.PathFor("target-folder");
        var category = Path.Combine(paths.LibraryRoot, "Design");
        var menuIcon = Path.Combine(category, "blue.ico");
        Directory.CreateDirectory(target);
        TestIconFactory.WriteValidIcon(menuIcon);

        var result = new IconMenuApplyService().ApplyMenuIcon(target, menuIcon, paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal("blue", result.Value.MenuItem.DisplayName);
        Assert.Equal(menuIcon, result.Value.MenuItem.IconPath);
        Assert.Equal(TargetKind.Folder, result.Value.ChangeResult.ApplyResult.TargetKind);
        Assert.True(File.Exists(result.Value.ChangeResult.ApplyResult.DesktopIniPath));
        Assert.True(File.Exists(result.Value.ChangeResult.ApplyResult.ImportedIcon.FullPath));

        var records = new RestoreRecordStore(paths.RestoreStateFile).List();
        Assert.True(records.Succeeded, records.Error.Message);
        Assert.Single(records.Value!);
    }

    [Fact]
    public void ApplyMenuIconRejectsUnsupportedTargetBeforeScanningCatalog()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var unsupportedTarget = temp.PathFor("notes.txt");
        var externalIcon = temp.PathFor("external", "blue.ico");
        File.WriteAllText(unsupportedTarget, "not a shortcut");
        TestIconFactory.WriteValidIcon(externalIcon);

        var result = new IconMenuApplyService().ApplyMenuIcon(unsupportedTarget, externalIcon, paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.UnsupportedTarget, result.Error.Code);
        Assert.False(Directory.Exists(paths.ImportedRoot));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void ApplyMenuIconRejectsExternalIconBeforeMutation()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var target = temp.PathFor("target-folder");
        var externalIcon = temp.PathFor("external", "blue.ico");
        Directory.CreateDirectory(target);
        TestIconFactory.WriteValidIcon(externalIcon);

        var result = new IconMenuApplyService().ApplyMenuIcon(target, externalIcon, paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidIcon, result.Error.Code);
        Assert.False(File.Exists(Path.Combine(target, "desktop.ini")));
        Assert.False(File.Exists(paths.RestoreStateFile));
        Assert.Empty(Directory.EnumerateFiles(paths.ImportedRoot));
    }
}
