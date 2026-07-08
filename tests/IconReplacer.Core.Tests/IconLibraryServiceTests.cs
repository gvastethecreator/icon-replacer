using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconLibraryServiceTests
{
    [Fact]
    public void ImportIconCopiesToImportedAndReturnsUpdatedStatus()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var sourceIcon = temp.PathFor("source", "blue.ico");
        TestIconFactory.WriteValidIcon(sourceIcon);

        var result = new IconLibraryService().ImportIcon(sourceIcon, paths, "My Blue Icon");

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.True(File.Exists(result.Value.ImportedIcon.FullPath));
        Assert.StartsWith("My Blue Icon-", result.Value.ImportedIcon.DisplayName, StringComparison.Ordinal);
        Assert.Equal(paths.ImportedRoot, result.Value.ImportedIcon.Category!.FullPath);
        Assert.Equal(1, result.Value.LibraryStatus.CategoryCount);
        Assert.Equal(1, result.Value.LibraryStatus.IconCount);
        Assert.Equal(0, result.Value.LibraryStatus.WarningCount);
    }

    [Fact]
    public void ImportIconDedupesSameContent()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var sourceIcon = temp.PathFor("source", "blue.ico");
        TestIconFactory.WriteValidIcon(sourceIcon);
        var service = new IconLibraryService();

        var first = service.ImportIcon(sourceIcon, paths, "Blue");
        var second = service.ImportIcon(sourceIcon, paths, "Different Blue");

        Assert.True(first.Succeeded, first.Error.Message);
        Assert.True(second.Succeeded, second.Error.Message);
        Assert.Equal(first.Value!.ImportedIcon.FullPath, second.Value!.ImportedIcon.FullPath);
        Assert.Equal(1, second.Value.LibraryStatus.IconCount);
    }

    [Fact]
    public void ImportIconRejectsInvalidIcon()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var sourceIcon = temp.PathFor("source", "fake.ico");
        TestIconFactory.WritePngHeader(sourceIcon);

        var result = new IconLibraryService().ImportIcon(sourceIcon, paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidIcon, result.Error.Code);
    }

    [Fact]
    public void ImportIconsReturnsPerFileResults()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var validIcon = temp.PathFor("source", "blue.ico");
        var invalidIcon = temp.PathFor("source", "fake.ico");
        TestIconFactory.WriteValidIcon(validIcon);
        TestIconFactory.WritePngHeader(invalidIcon);

        var result = new IconLibraryService().ImportIcons([validIcon, invalidIcon], paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.RequestedCount);
        Assert.Equal(1, result.Value.ImportedCount);
        Assert.Equal(0, result.Value.ReusedExistingCount);
        Assert.Equal(1, result.Value.FailedCount);
        Assert.Contains(result.Value.Items, item =>
            item.SourcePath == validIcon &&
            item.Status == IconBatchImportItemStatus.Imported &&
            item.ImportedIcon is not null);
        Assert.Contains(result.Value.Items, item =>
            item.SourcePath == invalidIcon &&
            item.Status == IconBatchImportItemStatus.Failed &&
            item.Error.Code == ErrorCode.InvalidIcon);
        Assert.Equal(1, result.Value.LibraryStatus.IconCount);
    }

    [Fact]
    public void ImportIconsMarksDuplicatesAsReused()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var sourceIcon = temp.PathFor("source", "blue.ico");
        TestIconFactory.WriteValidIcon(sourceIcon);

        var result = new IconLibraryService().ImportIcons([sourceIcon, sourceIcon], paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.ImportedCount);
        Assert.Equal(1, result.Value.ReusedExistingCount);
        Assert.Equal(0, result.Value.FailedCount);
        Assert.Equal(1, result.Value.LibraryStatus.IconCount);
        Assert.Single(Directory.EnumerateFiles(paths.ImportedRoot, "*.ico"));
    }

    [Fact]
    public void ImportIconsRejectsNullSourceList()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var result = new IconLibraryService().ImportIcons(null!, paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, result.Error.Code);
    }
}
