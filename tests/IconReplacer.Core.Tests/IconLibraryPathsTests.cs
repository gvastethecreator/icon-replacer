using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconLibraryPathsTests
{
    [Fact]
    public void FromRootsBuildsCanonicalIconLibraryLocations()
    {
        var result = IconLibraryPaths.FromRoots(
            Path.Combine(Path.GetTempPath(), "ir-user"),
            Path.Combine(Path.GetTempPath(), "ir-appdata"));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.EndsWith(Path.Combine("ir-user", ".icons"), result.Value.LibraryRoot);
        Assert.EndsWith(Path.Combine("ir-user", ".icons", "Imported"), result.Value.ImportedRoot);
        Assert.EndsWith(Path.Combine("ir-appdata", "Icon Replacer", "state.json"), result.Value.RestoreStateFile);
    }

    [Fact]
    public void FromRootsRejectsMissingUserProfile()
    {
        var result = IconLibraryPaths.FromRoots("", Path.GetTempPath());

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.PathNotFound, result.Error.Code);
    }

    [Fact]
    public void FromRootsRejectsMissingAppData()
    {
        var result = IconLibraryPaths.FromRoots(Path.GetTempPath(), "");

        Assert.False(result.Succeeded);
        Assert.Equal(ErrorCode.PathNotFound, result.Error.Code);
    }
}

