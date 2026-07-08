using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconBrowserServiceTests
{
    [Fact]
    public void BrowseFiltersBySearchAndCategory()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue-folder.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Games", "blue-game.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "green-folder.ico"));

        var snapshot = new IconBrowserService().Browse(
            paths,
            new IconBrowserOptions(
                SearchText: "blue",
                CategoryName: "Work"));

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(3, snapshot.Value.TotalIconCount);
        Assert.Equal(1, snapshot.Value.MatchedIconCount);
        var item = Assert.Single(snapshot.Value.Items);
        Assert.Equal("blue-folder", item.DisplayName);
        Assert.Equal("Work", item.CategoryName);
    }

    [Fact]
    public void BrowseCapsVisibleItems()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "one.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "two.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "three.ico"));

        var snapshot = new IconBrowserService().Browse(
            paths,
            new IconBrowserOptions(MaxItems: 2));

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(3, snapshot.Value.MatchedIconCount);
        Assert.Equal(2, snapshot.Value.VisibleIconCount);
        Assert.Equal(1, snapshot.Value.OmittedIconCount);
        Assert.True(snapshot.Value.IsTruncated);
    }

    [Fact]
    public void BrowseReportsCatalogWarnings()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WritePngHeader(Path.Combine(paths.LibraryRoot, "bad.ico"));

        var snapshot = new IconBrowserService().Browse(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(0, snapshot.Value.TotalIconCount);
        Assert.False(snapshot.Value.HasMatches);
        Assert.Single(snapshot.Value.Warnings);
    }
}
