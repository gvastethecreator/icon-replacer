using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class TargetItemTests
{
    [Fact]
    public void FromShellSelectionTreatsDirectoriesAsFolderTargets()
    {
        var result = TargetItem.FromShellSelection("Projects", isDirectory: true);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(TargetKind.Folder, result.Value.Kind);
        Assert.Equal(Path.GetFullPath("Projects"), result.Value.FullPath);
    }

    [Fact]
    public void FromShellSelectionTreatsLnkFilesAsShortcutTargets()
    {
        var result = TargetItem.FromShellSelection("Example.lnk", isDirectory: false);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(TargetKind.Shortcut, result.Value.Kind);
        Assert.Equal(Path.GetFullPath("Example.lnk"), result.Value.FullPath);
    }

    [Fact]
    public void FromShellSelectionRejectsUnsupportedFiles()
    {
        var result = TargetItem.FromShellSelection("Example.txt", isDirectory: false);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.UnsupportedTarget, result.Error.Code);
    }

    [Fact]
    public void FromShellSelectionRejectsEmptyPath()
    {
        var result = TargetItem.FromShellSelection(" ", isDirectory: false);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, result.Error.Code);
    }
}

