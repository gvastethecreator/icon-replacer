using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconCatalogServiceTests
{
    [Fact]
    public void ScanCreatesLibraryAndImportedFolders()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var service = new IconCatalogService();

        var result = service.Scan(paths);

        Assert.True(result.Succeeded);
        Assert.True(Directory.Exists(paths.LibraryRoot));
        Assert.True(Directory.Exists(paths.ImportedRoot));
    }

    [Fact]
    public void ScanReturnsRootAndCategoryIcons()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "root.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));

        var result = new IconCatalogService().Scan(paths);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Entries.Count);
        Assert.Contains(result.Value.Entries, entry => entry.DisplayName == "root" && entry.Category is null);
        Assert.Contains(result.Value.Entries, entry => entry.DisplayName == "blue" && entry.Category?.Name == "Work");
        Assert.Contains(result.Value.Categories, category => category.Name == "Work");
    }

    [Fact]
    public void ScanSkipsInvalidIconsAndReportsWarnings()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "valid.ico"));
        TestIconFactory.WritePngHeader(Path.Combine(paths.LibraryRoot, "bad.ico"));

        var result = new IconCatalogService().Scan(paths);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value.Entries);
        Assert.Single(result.Value.Warnings);
        Assert.Equal(ErrorCode.InvalidIcon, result.Value.Warnings[0].Error.Code);
    }
}

