using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppMenuApplyActivationServiceTests
{
    [Fact]
    public void PreviewActivationResolvesDirectMenuApplyWithoutMutatingTarget()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        var icon = Path.Combine(paths.LibraryRoot, "Work", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(icon);

        var preview = new AppMenuApplyActivationService().PreviewActivation(
            [IconMenuCommandService.MenuApplyVerbName, folder, icon],
            paths);

        Assert.True(preview.Succeeded, preview.Error.Message);
        Assert.NotNull(preview.Value);
        Assert.True(preview.Value.CanApply);
        Assert.Equal(IconMenuCommandService.MenuApplyVerbName, preview.Value.VerbName);
        Assert.Equal(icon, preview.Value.RequestedIconPath);
        Assert.Equal(IconMenuCommandKind.ApplyLibraryIcon, preview.Value.Command!.Kind);
        Assert.Equal(
            [IconMenuCommandService.MenuApplyVerbName, folder, icon],
            preview.Value.AppArguments);
        Assert.Equal(ShellSelectionStatus.Supported, preview.Value.Invocation!.Selection!.Status);
        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void PreviewActivationDisablesUnsupportedTargetWithoutMutation()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var unsupported = temp.PathFor("notes.txt");
        var icon = Path.Combine(paths.LibraryRoot, "Work", "blue.ico");
        File.WriteAllText(unsupported, "not a shortcut");
        TestIconFactory.WriteValidIcon(icon);

        var preview = new AppMenuApplyActivationService().PreviewActivation(
            [IconMenuCommandService.MenuApplyVerbName, unsupported, icon],
            paths);

        Assert.True(preview.Succeeded, preview.Error.Message);
        Assert.NotNull(preview.Value);
        Assert.False(preview.Value.CanApply);
        Assert.Equal(ErrorCode.UnsupportedTarget, preview.Value.Error.Code);
        Assert.Equal(ShellSelectionStatus.UnsupportedTarget, preview.Value.Invocation!.Selection!.Status);
        Assert.Empty(preview.Value.AppArguments);
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void PreviewActivationDisablesIconOutsideCurrentMenu()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        var externalIcon = temp.PathFor("external", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(externalIcon);

        var preview = new AppMenuApplyActivationService().PreviewActivation(
            [IconMenuCommandService.MenuApplyVerbName, folder, externalIcon],
            paths);

        Assert.True(preview.Succeeded, preview.Error.Message);
        Assert.NotNull(preview.Value);
        Assert.False(preview.Value.CanApply);
        Assert.Null(preview.Value.Command);
        Assert.Null(preview.Value.Invocation);
        Assert.Equal(ErrorCode.InvalidIcon, preview.Value.Error.Code);
        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void PreviewActivationRejectsMalformedArguments()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var preview = new AppMenuApplyActivationService().PreviewActivation(["menu-apply"], paths);

        Assert.False(preview.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, preview.Error.Code);
    }
}
