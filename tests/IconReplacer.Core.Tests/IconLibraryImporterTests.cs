using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconLibraryImporterTests
{
    [Fact]
    public void ImportCopiesValidIconIntoImportedFolder()
    {
        using var temp = new TempDirectory();
        var source = temp.PathFor("source", "Project Icon.ico");
        TestIconFactory.WriteValidIcon(source);
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var result = new IconLibraryImporter().Import(source, paths);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.StartsWith(paths.ImportedRoot, result.Value.FullPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(result.Value.FullPath));
        Assert.Equal("Imported", result.Value.Category?.Name);
    }

    [Fact]
    public void ImportDedupesByHash()
    {
        using var temp = new TempDirectory();
        var source = temp.PathFor("source", "same.ico");
        TestIconFactory.WriteValidIcon(source);
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var importer = new IconLibraryImporter();

        var first = importer.Import(source, paths, preferredName: "same");
        var second = importer.Import(source, paths, preferredName: "same");

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.Value!.FullPath, second.Value!.FullPath);
        Assert.Single(Directory.EnumerateFiles(paths.ImportedRoot, "*.ico"));
    }

    [Fact]
    public void ImportDedupesByHashWhenPreferredNameDiffers()
    {
        using var temp = new TempDirectory();
        var source = temp.PathFor("source", "same.ico");
        TestIconFactory.WriteValidIcon(source);
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var importer = new IconLibraryImporter();

        var first = importer.Import(source, paths, preferredName: "first");
        var second = importer.Import(source, paths, preferredName: "second");

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.Value!.FullPath, second.Value!.FullPath);
        Assert.Single(Directory.EnumerateFiles(paths.ImportedRoot, "*.ico"));
    }

    [Fact]
    public void ImportRejectsInvalidIconBeforeCopy()
    {
        using var temp = new TempDirectory();
        var source = temp.PathFor("source", "bad.ico");
        TestIconFactory.WritePngHeader(source);
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var result = new IconLibraryImporter().Import(source, paths);

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.InvalidIcon, result.Error.Code);
        Assert.False(Directory.Exists(paths.ImportedRoot));
    }
}
