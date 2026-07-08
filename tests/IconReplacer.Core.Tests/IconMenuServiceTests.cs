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
        Assert.Equal(IconMenuState.Ready, snapshot.Value.State);
        Assert.Equal("The Icon Library menu is ready.", snapshot.Value.StatusMessage);
        Assert.Null(snapshot.Value.RecommendedActionLabel);
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
        Assert.Equal(IconMenuState.Empty, emptySnapshot.Value.State);
        Assert.Equal("Import icons", emptySnapshot.Value.RecommendedActionLabel);

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
        Assert.Equal(IconMenuState.Truncated, snapshot.Value.State);
        Assert.Equal("Open Icon Replacer", snapshot.Value.RecommendedActionLabel);
        Assert.True(snapshot.Value.IsTruncated);
        var category = Assert.Single(snapshot.Value.Categories);
        Assert.Equal(2, category.Items.Count);
        Assert.True(category.IsTruncated);
    }

    [Fact]
    public void BuildSnapshotReportsWarningsWhenInvalidIconsAreHidden()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "valid.ico"));
        TestIconFactory.WritePngHeader(Path.Combine(paths.LibraryRoot, "bad.ico"));

        var snapshot = new IconMenuService().BuildSnapshot(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.Equal(IconMenuState.HasWarnings, snapshot.Value.State);
        Assert.Equal("Review catalog warnings", snapshot.Value.RecommendedActionLabel);
        Assert.Single(snapshot.Value.Warnings);
        Assert.Equal(1, snapshot.Value.VisibleIconCount);
    }

    [Fact]
    public void BuildSnapshotReturnsUnavailableStateWhenCatalogCannotBeScanned()
    {
        using var temp = new TempDirectory();
        var userRoot = temp.PathFor("user");
        Directory.CreateDirectory(userRoot);
        File.WriteAllText(Path.Combine(userRoot, IconLibraryPaths.LibraryFolderName), "not a directory");
        var paths = IconLibraryPaths.FromRoots(userRoot, temp.PathFor("appdata")).Value!;

        var snapshot = new IconMenuService().BuildSnapshot(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.False(snapshot.Value.IsAvailable);
        Assert.Equal(IconMenuState.Unavailable, snapshot.Value.State);
        Assert.Equal("Open diagnostics", snapshot.Value.RecommendedActionLabel);
        Assert.Equal(0, snapshot.Value.TotalIconCount);
        Assert.NotEqual(ErrorCode.None, snapshot.Value.Error.Code);
    }
}
