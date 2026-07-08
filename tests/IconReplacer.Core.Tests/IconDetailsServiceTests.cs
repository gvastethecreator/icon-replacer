using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconDetailsServiceTests
{
    [Fact]
    public void GetDetailsDescribesCatalogIcon()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var icon = Path.Combine(paths.LibraryRoot, "Work", "blue.ico");
        TestIconFactory.WriteValidIcon(icon);

        var details = new IconDetailsService().GetDetails(icon, paths);

        Assert.True(details.Succeeded, details.Error.Message);
        Assert.NotNull(details.Value);
        Assert.True(details.Value.IsInIconLibrary);
        Assert.Equal("blue", details.Value.DisplayName);
        Assert.Equal("Work", details.Value.CategoryName);
        Assert.Equal(1, details.Value.ImageCount);
        Assert.Equal(32, details.Value.RecommendedImage.Width);
        Assert.True(details.Value.RecommendedImage.HasUsefulSize);
    }

    [Fact]
    public void GetDetailsDescribesExternalValidIcon()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var icon = temp.PathFor("external", "green.ico");
        TestIconFactory.WriteValidIcon(icon, width: 48, height: 48);

        var details = new IconDetailsService().GetDetails(icon, paths);

        Assert.True(details.Succeeded, details.Error.Message);
        Assert.NotNull(details.Value);
        Assert.False(details.Value.IsInIconLibrary);
        Assert.Equal("green", details.Value.DisplayName);
        Assert.Equal(IconDetailsService.ExternalCategoryName, details.Value.CategoryName);
        Assert.Equal(48, details.Value.RecommendedImage.Width);
    }

    [Fact]
    public void GetDetailsRejectsInvalidIcon()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var icon = temp.PathFor("external", "bad.ico");
        TestIconFactory.WritePngHeader(icon);

        var details = new IconDetailsService().GetDetails(icon, paths);

        Assert.False(details.Succeeded);
        Assert.Equal(ErrorCode.InvalidIcon, details.Error.Code);
    }
}
