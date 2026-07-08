using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconCollectionImportServiceTests
{
    [Fact]
    public void ImportIntoCollectionCreatesCollectionAndCopiesValidIcons()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var sourceIcon = temp.PathFor("source", "blue.ico");
        TestIconFactory.WriteValidIcon(sourceIcon);

        var result = new IconCollectionImportService().ImportIntoCollection("Projects", [sourceIcon], paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal("Projects", result.Value.Collection.Name);
        Assert.Equal(1, result.Value.ImportedCount);
        Assert.Equal(0, result.Value.ReusedExistingCount);
        Assert.Equal(0, result.Value.FailedCount);
        Assert.Equal(1, result.Value.Collection.IconCount);
        Assert.Single(Directory.EnumerateFiles(result.Value.Collection.FullPath, "*.ico"));
        Assert.Equal("Projects", result.Value.Items.Single().ImportedIcon!.Category!.Name);
    }

    [Fact]
    public void ImportIntoCollectionReusesDuplicateBytesWithinCollection()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var sourceIcon = temp.PathFor("source", "blue.ico");
        TestIconFactory.WriteValidIcon(sourceIcon);

        var result = new IconCollectionImportService().ImportIntoCollection(
            "Projects",
            [sourceIcon, sourceIcon],
            paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.ImportedCount);
        Assert.Equal(1, result.Value.ReusedExistingCount);
        Assert.Equal(0, result.Value.FailedCount);
        Assert.Equal(1, result.Value.Collection.IconCount);
        Assert.Single(Directory.EnumerateFiles(result.Value.Collection.FullPath, "*.ico"));
    }

    [Fact]
    public void ImportIntoCollectionReturnsPerFileFailures()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var validIcon = temp.PathFor("source", "blue.ico");
        var invalidIcon = temp.PathFor("source", "fake.ico");
        TestIconFactory.WriteValidIcon(validIcon);
        TestIconFactory.WritePngHeader(invalidIcon);

        var result = new IconCollectionImportService().ImportIntoCollection(
            "Projects",
            [validIcon, invalidIcon],
            paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.ImportedCount);
        Assert.Equal(0, result.Value.ReusedExistingCount);
        Assert.Equal(1, result.Value.FailedCount);
        Assert.Contains(result.Value.Items, item =>
            item.SourcePath == invalidIcon &&
            item.Status == IconBatchImportItemStatus.Failed &&
            item.Error.Code == ErrorCode.InvalidIcon);
    }

    [Fact]
    public void ImportIntoCollectionRejectsNullSourceList()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var result = new IconCollectionImportService().ImportIntoCollection("Projects", null!, paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, result.Error.Code);
    }
}
