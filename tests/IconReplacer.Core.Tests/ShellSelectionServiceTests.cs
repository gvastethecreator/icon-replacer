using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class ShellSelectionServiceTests
{
    [Fact]
    public void EvaluatePathSupportsExistingFolder()
    {
        using var temp = new TempDirectory();
        var folder = temp.PathFor("Projects");
        Directory.CreateDirectory(folder);

        var result = new ShellSelectionService().EvaluatePath(folder);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(ShellSelectionStatus.Supported, result.Value.Status);
        Assert.True(result.Value.CanShowChangeIcon);
        Assert.NotNull(result.Value.Target);
        Assert.Equal(TargetKind.Folder, result.Value.Target.Kind);
        Assert.Equal(folder, result.Value.Target.FullPath);
    }

    [Fact]
    public void EvaluatePathSupportsExistingShortcut()
    {
        using var temp = new TempDirectory();
        var shortcut = temp.PathFor("Project.lnk");
        File.WriteAllText(shortcut, "placeholder shortcut");

        var result = new ShellSelectionService().EvaluatePath(shortcut);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(ShellSelectionStatus.Supported, result.Value.Status);
        Assert.True(result.Value.CanShowChangeIcon);
        Assert.NotNull(result.Value.Target);
        Assert.Equal(TargetKind.Shortcut, result.Value.Target.Kind);
        Assert.Equal(shortcut, result.Value.Target.FullPath);
    }

    [WindowsOnlyFact]
    public void EvaluatePathSupportsDirectorySymbolicLink()
    {
        AssertSupportsDirectoryLink(DirectoryLinkKind.SymbolicLink);
    }

    [WindowsOnlyFact]
    public void EvaluatePathSupportsDirectoryJunction()
    {
        AssertSupportsDirectoryLink(DirectoryLinkKind.Junction);
    }

    [Fact]
    public void EvaluatePathRejectsExistingUnsupportedFile()
    {
        using var temp = new TempDirectory();
        var file = temp.PathFor("notes.txt");
        File.WriteAllText(file, "not a shortcut");

        var result = new ShellSelectionService().EvaluatePath(file);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(ShellSelectionStatus.UnsupportedTarget, result.Value.Status);
        Assert.False(result.Value.CanShowChangeIcon);
        Assert.Null(result.Value.Target);
        Assert.Equal(ErrorCode.UnsupportedTarget, result.Value.Error.Code);
    }

    [Fact]
    public void EvaluateRejectsMultipleSelection()
    {
        var result = new ShellSelectionService().Evaluate(
            [
                new ShellSelectionItem("first", IsDirectory: true),
                new ShellSelectionItem("second.lnk", IsDirectory: false)
            ]);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(ShellSelectionStatus.MultipleSelectionUnsupported, result.Value.Status);
        Assert.False(result.Value.CanShowChangeIcon);
        Assert.Equal(ErrorCode.UnsupportedTarget, result.Value.Error.Code);
    }

    [Fact]
    public void EvaluateRejectsRemoteSelection()
    {
        var result = new ShellSelectionService().Evaluate(
            [new ShellSelectionItem(@"\\server\share\Project", IsDirectory: true)]);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(ShellSelectionStatus.RemotePathUnsupported, result.Value.Status);
        Assert.False(result.Value.CanShowChangeIcon);
        Assert.Equal(ErrorCode.RemotePathUnsupported, result.Value.Error.Code);
    }

    [Fact]
    public void EvaluatePathRejectsMissingTarget()
    {
        using var temp = new TempDirectory();
        var missing = temp.PathFor("missing-folder");

        var result = new ShellSelectionService().EvaluatePath(missing);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(ShellSelectionStatus.MissingTarget, result.Value.Status);
        Assert.False(result.Value.CanShowChangeIcon);
        Assert.Equal(ErrorCode.PathNotFound, result.Value.Error.Code);
    }

    private static void AssertSupportsDirectoryLink(DirectoryLinkKind kind)
    {
        using var temp = new TempDirectory();
        var target = temp.PathFor("target");
        var link = temp.PathFor(kind.ToString());
        Directory.CreateDirectory(target);

        try
        {
            ReparsePointTestHelper.CreateDirectoryLink(kind, link, target);

            var result = new ShellSelectionService().EvaluatePath(link);

            Assert.True(result.Succeeded, result.Error.Message);
            Assert.NotNull(result.Value);
            Assert.Equal(ShellSelectionStatus.Supported, result.Value.Status);
            Assert.True(result.Value.CanShowChangeIcon);
            Assert.Equal(TargetKind.Folder, result.Value.Target!.Kind);
            Assert.Equal(link, result.Value.Target.FullPath);
        }
        finally
        {
            ReparsePointTestHelper.DeleteDirectoryLink(link);
        }
    }
}
