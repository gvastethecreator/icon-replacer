using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconChangePreviewServiceTests
{
    [Fact]
    public void PreviewChangeAllowsValidFolderAndIconWithoutApplying()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(sourceIcon);

        var result = new IconChangePreviewService().PreviewChange(folder, sourceIcon, paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.CanApply);
        Assert.Equal(ErrorCode.None, result.Value.Error.Code);
        Assert.Equal(ShellSelectionStatus.Supported, result.Value.Selection.Status);
        Assert.Equal(TargetKind.Folder, result.Value.Selection.Target!.Kind);
        Assert.NotNull(result.Value.IconDetails);
        Assert.Equal("blue", result.Value.IconDetails.DisplayName);
        AssertNoMutationArtifacts(folder, paths);
    }

    [Fact]
    public void PreviewChangeReportsUnsupportedTargetAndStillDescribesValidIcon()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var targetFile = temp.PathFor("notes.txt");
        var sourceIcon = temp.PathFor("source", "blue.ico");
        File.WriteAllText(targetFile, "not a shortcut");
        TestIconFactory.WriteValidIcon(sourceIcon);

        var result = new IconChangePreviewService().PreviewChange(targetFile, sourceIcon, paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.CanApply);
        Assert.Equal(ErrorCode.UnsupportedTarget, result.Value.Error.Code);
        Assert.Equal(ShellSelectionStatus.UnsupportedTarget, result.Value.Selection.Status);
        Assert.Equal(ErrorCode.None, result.Value.IconError.Code);
        Assert.NotNull(result.Value.IconDetails);
        AssertNoMutationArtifacts(Path.GetDirectoryName(targetFile)!, paths);
    }

    [Fact]
    public void PreviewChangeReportsInvalidIconWithoutApplyingToValidTarget()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("target-folder");
        var badIcon = temp.PathFor("source", "bad.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WritePngHeader(badIcon);

        var result = new IconChangePreviewService().PreviewChange(folder, badIcon, paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.CanApply);
        Assert.Equal(ErrorCode.InvalidIcon, result.Value.Error.Code);
        Assert.Equal(ShellSelectionStatus.Supported, result.Value.Selection.Status);
        Assert.Equal(ErrorCode.InvalidIcon, result.Value.IconError.Code);
        Assert.Null(result.Value.IconDetails);
        AssertNoMutationArtifacts(folder, paths);
    }

    private static void AssertNoMutationArtifacts(string targetFolder, IconLibraryPaths paths)
    {
        Assert.False(File.Exists(Path.Combine(targetFolder, "desktop.ini")));
        Assert.False(File.Exists(paths.RestoreStateFile));

        if (Directory.Exists(paths.ImportedRoot))
        {
            Assert.Empty(Directory.EnumerateFiles(paths.ImportedRoot));
        }
    }
}
