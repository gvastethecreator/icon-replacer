using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconCollectionServiceTests
{
    [Fact]
    public void CreateCollectionSanitizesNameAndUpdatesCatalogStatus()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var result = new IconCollectionService().CreateCollection("  Design:Tools?  ", paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.Created);
        Assert.Equal("Design-Tools", result.Value.Collection.Name);
        Assert.True(Directory.Exists(result.Value.Collection.FullPath));
        Assert.Equal(2, result.Value.LibraryStatus.CategoryCount);
        Assert.Equal(0, result.Value.LibraryStatus.IconCount);
    }

    [Fact]
    public void CreateCollectionIsIdempotent()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var service = new IconCollectionService();

        var first = service.CreateCollection("Projects", paths);
        var second = service.CreateCollection("Projects", paths);

        Assert.True(first.Succeeded, first.Error.Message);
        Assert.True(second.Succeeded, second.Error.Message);
        Assert.True(first.Value!.Created);
        Assert.False(second.Value!.Created);
        Assert.Equal(first.Value.Collection.FullPath, second.Value.Collection.FullPath);
    }

    [Fact]
    public void CreateCollectionRejectsEmptyName()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var result = new IconCollectionService().CreateCollection(" .. ", paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, result.Error.Code);
    }

    [Fact]
    public void ListCollectionsIncludesEmptyCollectionsAndImported()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));
        Directory.CreateDirectory(Path.Combine(paths.LibraryRoot, "Empty"));

        var result = new IconCollectionService().ListCollections(paths);

        Assert.True(result.Succeeded, result.Error.Message);
        Assert.NotNull(result.Value);
        Assert.Contains(result.Value, item =>
            item.Name == "Work" &&
            item.IconCount == 1 &&
            !item.IsImportedCollection);
        Assert.Contains(result.Value, item =>
            item.Name == "Empty" &&
            item.IconCount == 0 &&
            !item.IsImportedCollection);
        Assert.Contains(result.Value, item =>
            item.Name == IconLibraryPaths.ImportedFolderName &&
            item.IconCount == 0 &&
            item.IsImportedCollection);
    }
}
