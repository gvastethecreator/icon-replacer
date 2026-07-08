using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconMenuServiceTests
{
    [Fact]
    public void BuildSnapshotGroupsRootAndCategoryIcons()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "root.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Games", "green.ico"));

        var snapshot = new IconMenuService().BuildSnapshot(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal("Change icon...", snapshot.Value.ChangeIconCommandLabel);
        Assert.Equal(3, snapshot.Value.TotalIconCount);
        Assert.Equal(3, snapshot.Value.VisibleIconCount);
        Assert.Equal(0, snapshot.Value.OmittedIconCount);
        Assert.Contains(snapshot.Value.RootIcons, item => item.DisplayName == "root");
        Assert.Contains(snapshot.Value.Categories, category => category.Name == "Work" && category.Items.Single().DisplayName == "blue");
        Assert.Contains(snapshot.Value.Categories, category => category.Name == "Games" && category.Items.Single().DisplayName == "green");
    }

    [Fact]
    public void BuildSnapshotReflectsNewFoldersOnNextScan()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var service = new IconMenuService();

        var emptySnapshot = service.BuildSnapshot(paths);
        Assert.True(emptySnapshot.Succeeded, emptySnapshot.Error.Message);
        Assert.True(emptySnapshot.Value!.IsEmpty);

        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Design", "figma.ico"));

        var refreshedSnapshot = service.BuildSnapshot(paths);

        Assert.True(refreshedSnapshot.Succeeded, refreshedSnapshot.Error.Message);
        Assert.False(refreshedSnapshot.Value!.IsEmpty);
        Assert.Contains(refreshedSnapshot.Value.Categories, category => category.Name == "Design");
    }

    [Fact]
    public void BuildSnapshotReportsOmittedItemsWhenMenuIsCapped()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "one.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "two.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "three.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "only.ico"));

        var snapshot = new IconMenuService().BuildSnapshot(
            paths,
            new IconMenuOptions(
                MaxRootIconItems: 0,
                MaxCategoryCount: 1,
                MaxIconItemsPerCategory: 2));

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(4, snapshot.Value.TotalIconCount);
        Assert.Equal(2, snapshot.Value.VisibleIconCount);
        Assert.Equal(2, snapshot.Value.OmittedIconCount);
        Assert.True(snapshot.Value.IsTruncated);
        var category = Assert.Single(snapshot.Value.Categories);
        Assert.Equal(2, category.Items.Count);
        Assert.True(category.IsTruncated);
    }
}
