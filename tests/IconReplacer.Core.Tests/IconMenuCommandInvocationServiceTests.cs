using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconMenuCommandInvocationServiceTests
{
    [Fact]
    public void PreviewInvocationResolvesChangeIconArgumentsForSupportedFolder()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target folder");
        Directory.CreateDirectory(folder);

        var preview = new IconMenuCommandInvocationService().PreviewInvocation("change-icon", folder, paths);

        Assert.True(preview.Succeeded, preview.Error.Message);
        Assert.NotNull(preview.Value);
        Assert.True(preview.Value.CanInvoke);
        Assert.Equal(IconMenuCommandKind.ChangeIcon, preview.Value.Command.Kind);
        Assert.Equal(ShellSelectionStatus.Supported, preview.Value.Selection!.Status);
        Assert.Equal(
            ["change-icon", "--target", folder, "--target-kind", "folder"],
            preview.Value.ResolvedArguments);
        Assert.Contains($"\"{folder}\"", preview.Value.DisplayArguments);
    }

    [Fact]
    public void PreviewInvocationResolvesIconCommandArgumentsForSupportedShortcut()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var icon = Path.Combine(paths.LibraryRoot, "Work", "blue.ico");
        var shortcut = temp.PathFor("sample.lnk");
        TestIconFactory.WriteValidIcon(icon);
        File.WriteAllText(shortcut, "placeholder shortcut");
        var command = new IconMenuCommandService()
            .BuildCommands(paths)
            .Value!
            .Commands
            .Single(item => item.IconPath == icon);

        var preview = new IconMenuCommandInvocationService().PreviewInvocation(command.Id, shortcut, paths);

        Assert.True(preview.Succeeded, preview.Error.Message);
        Assert.NotNull(preview.Value);
        Assert.True(preview.Value.CanInvoke);
        Assert.Equal(IconMenuCommandKind.ApplyLibraryIcon, preview.Value.Command.Kind);
        Assert.Equal(
            ["menu-apply", shortcut, icon],
            preview.Value.ResolvedArguments);
    }

    [Fact]
    public void PreviewInvocationDisablesTargetCommandForUnsupportedSelection()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var unsupported = temp.PathFor("notes.txt");
        File.WriteAllText(unsupported, "not a shortcut");

        var preview = new IconMenuCommandInvocationService().PreviewInvocation("change-icon", unsupported, paths);

        Assert.True(preview.Succeeded, preview.Error.Message);
        Assert.NotNull(preview.Value);
        Assert.False(preview.Value.CanInvoke);
        Assert.Equal(ShellSelectionStatus.UnsupportedTarget, preview.Value.Selection!.Status);
        Assert.Empty(preview.Value.ResolvedArguments);
        Assert.Equal(ErrorCode.UnsupportedTarget, preview.Value.Error.Code);
    }

    [Fact]
    public void PreviewInvocationAllowsOpenAppOverflowWithoutTarget()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "one.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "two.ico"));

        var preview = new IconMenuCommandInvocationService().PreviewInvocation(
            "open-app",
            targetPath: null,
            paths,
            new IconMenuOptions(
                MaxRootIconItems: 0,
                MaxCategoryCount: 1,
                MaxIconItemsPerCategory: 1));

        Assert.True(preview.Succeeded, preview.Error.Message);
        Assert.NotNull(preview.Value);
        Assert.True(preview.Value.CanInvoke);
        Assert.Equal(IconMenuCommandKind.OpenApp, preview.Value.Command.Kind);
        Assert.Null(preview.Value.Selection);
        Assert.Empty(preview.Value.ResolvedArguments);
    }

    [Fact]
    public void PreviewInvocationRejectsUnknownCommandId()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var preview = new IconMenuCommandInvocationService().PreviewInvocation("icon:missing", targetPath: null, paths);

        Assert.False(preview.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, preview.Error.Code);
    }
}
