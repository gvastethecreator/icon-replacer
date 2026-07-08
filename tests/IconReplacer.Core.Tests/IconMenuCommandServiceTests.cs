using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconMenuCommandServiceTests
{
    [Fact]
    public void BuildCommandsIncludesChangeIconAndVisibleIconCommands()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var rootIcon = Path.Combine(paths.LibraryRoot, "root.ico");
        var workIcon = Path.Combine(paths.LibraryRoot, "Work Tools", "blue.ico");
        TestIconFactory.WriteValidIcon(rootIcon);
        TestIconFactory.WriteValidIcon(workIcon);

        var snapshot = new IconMenuCommandService().BuildCommands(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(IconMenuState.Ready, snapshot.Value.State);
        Assert.Equal(2, snapshot.Value.TotalIconCount);
        Assert.Equal(2, snapshot.Value.VisibleIconCommandCount);
        Assert.Equal(3, snapshot.Value.Commands.Count);
        Assert.Equal(IconMenuCommandKind.ChangeIcon, snapshot.Value.Commands[0].Kind);
        Assert.Equal("change-icon", snapshot.Value.Commands[0].Id);
        Assert.Equal("change-icon --target {target} --target-kind {target-kind}", snapshot.Value.Commands[0].DisplayArguments);

        var rootCommand = snapshot.Value.Commands.Single(command => command.IconPath == rootIcon);
        Assert.Equal(IconMenuCommandKind.ApplyLibraryIcon, rootCommand.Kind);
        Assert.Null(rootCommand.CategoryName);
        Assert.Equal("menu-apply", rootCommand.ArgumentTemplate[0]);
        Assert.Equal("{target}", rootCommand.ArgumentTemplate[1]);
        Assert.Equal(rootIcon, rootCommand.ArgumentTemplate[2]);

        var workCommand = snapshot.Value.Commands.Single(command => command.IconPath == workIcon);
        Assert.Equal("Work Tools", workCommand.CategoryName);
        Assert.Contains("\"", workCommand.DisplayArguments);
    }

    [Fact]
    public void BuildCommandsCreatesStableSafeIconCommandIds()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var icon = Path.Combine(paths.LibraryRoot, "Design", "figma.ico");
        TestIconFactory.WriteValidIcon(icon);
        var service = new IconMenuCommandService();

        var first = service.BuildCommands(paths);
        var second = service.BuildCommands(paths);

        Assert.True(first.Succeeded, first.Error.Message);
        Assert.True(second.Succeeded, second.Error.Message);
        var firstId = first.Value!.Commands.Single(command => command.IconPath == icon).Id;
        var secondId = second.Value!.Commands.Single(command => command.IconPath == icon).Id;
        Assert.Equal(firstId, secondId);
        Assert.Matches("^icon:[0-9a-f]{12}$", firstId);
    }

    [Fact]
    public void BuildCommandsAddsOpenAppOverflowCommandWhenMenuIsTruncated()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "one.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "two.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "three.ico"));

        var snapshot = new IconMenuCommandService().BuildCommands(
            paths,
            new IconMenuOptions(
                MaxRootIconItems: 0,
                MaxCategoryCount: 1,
                MaxIconItemsPerCategory: 1));

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.True(snapshot.Value.IsTruncated);
        Assert.Equal(1, snapshot.Value.VisibleIconCommandCount);
        Assert.Equal(2, snapshot.Value.OmittedIconCount);
        var overflow = snapshot.Value.Commands.Last();
        Assert.Equal(IconMenuCommandKind.OpenApp, overflow.Kind);
        Assert.Equal("open-app", overflow.Id);
        Assert.False(overflow.RequiresTarget);
        Assert.Contains("2 hidden", overflow.Label);
    }

    [Fact]
    public void BuildCommandsAddsOpenAppCommandForEmptyMenu()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var snapshot = new IconMenuCommandService().BuildCommands(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(IconMenuState.Empty, snapshot.Value.State);
        Assert.Equal(0, snapshot.Value.VisibleIconCommandCount);
        Assert.Equal(2, snapshot.Value.Commands.Count);
        Assert.Equal(IconMenuCommandKind.ChangeIcon, snapshot.Value.Commands[0].Kind);
        Assert.Equal(IconMenuCommandKind.OpenApp, snapshot.Value.Commands[1].Kind);
        Assert.False(snapshot.Value.Commands[1].RequiresTarget);
    }

    [Fact]
    public void BuildCommandsDegradesToChangeIconAndOpenAppWhenCatalogFails()
    {
        using var temp = new TempDirectory();
        var userRoot = temp.PathFor("user");
        Directory.CreateDirectory(userRoot);
        File.WriteAllText(Path.Combine(userRoot, IconLibraryPaths.LibraryFolderName), "not a directory");
        var paths = IconLibraryPaths.FromRoots(userRoot, temp.PathFor("appdata")).Value!;

        var snapshot = new IconMenuCommandService().BuildCommands(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.False(snapshot.Value.IsAvailable);
        Assert.Equal(IconMenuState.Unavailable, snapshot.Value.State);
        Assert.Equal(2, snapshot.Value.Commands.Count);
        Assert.Equal(IconMenuCommandKind.ChangeIcon, snapshot.Value.Commands[0].Kind);
        Assert.Equal(IconMenuCommandKind.OpenApp, snapshot.Value.Commands[1].Kind);
        Assert.NotEqual(ErrorCode.None, snapshot.Value.Error.Code);
    }
}
