using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class CatalogWarningsServiceTests
{
    [Fact]
    public void GetWarningsListsInvalidRootAndCategoryIcons()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "valid.ico"));
        TestIconFactory.WritePngHeader(Path.Combine(paths.LibraryRoot, "bad-root.ico"));
        TestIconFactory.WritePngHeader(Path.Combine(paths.LibraryRoot, "Work", "bad-work.ico"));

        var snapshot = new CatalogWarningsService().GetWarnings(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.True(snapshot.Value.HasWarnings);
        Assert.Equal(1, snapshot.Value.TotalIconCount);
        Assert.Equal(2, snapshot.Value.WarningCount);
        Assert.Contains(snapshot.Value.Warnings, warning =>
            warning.DisplayName == "bad-root" &&
            warning.CategoryName is null &&
            warning.ErrorCode == ErrorCode.InvalidIcon);
        Assert.Contains(snapshot.Value.Warnings, warning =>
            warning.DisplayName == "bad-work" &&
            warning.CategoryName == "Work" &&
            warning.ErrorCode == ErrorCode.InvalidIcon);
    }

    [Fact]
    public void GetWarningsReturnsEmptySnapshotForCleanCatalog()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "valid.ico"));

        var snapshot = new CatalogWarningsService().GetWarnings(paths);

        Assert.True(snapshot.Succeeded, snapshot.Error.Message);
        Assert.NotNull(snapshot.Value);
        Assert.False(snapshot.Value.HasWarnings);
        Assert.Equal(1, snapshot.Value.TotalIconCount);
        Assert.Equal(0, snapshot.Value.WarningCount);
        Assert.Empty(snapshot.Value.Warnings);
    }
}
