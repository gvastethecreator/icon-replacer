using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class IconImportPickerRequestServiceTests
{
    [Fact]
    public void CreateRequestBuildsMultiIconImportPicker()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var request = new IconImportPickerRequestService().CreateRequest(paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanOpenPicker);
        Assert.Equal("Import icons", request.Value.Title);
        Assert.Equal(paths.LibraryRoot, request.Value.InitialDirectory);
        Assert.Equal("Imported", request.Value.DestinationCollectionName);
        Assert.Equal(paths.ImportedRoot, request.Value.DestinationDirectory);
        Assert.True(request.Value.AllowMultiple);
        Assert.Equal("Icon files (*.ico)", request.Value.FileTypeLabel);
        Assert.Contains(".ico", request.Value.FileExtensions);
        Assert.True(Directory.Exists(paths.LibraryRoot));
        Assert.True(Directory.Exists(paths.ImportedRoot));
    }

    [Fact]
    public void CreateRequestCreatesCollectionDestinationWhenRequested()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var request = new IconImportPickerRequestService().CreateRequest(paths, "Design Tools");

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanOpenPicker);
        Assert.Equal("Import icons into Design Tools", request.Value.Title);
        Assert.Equal("Design Tools", request.Value.DestinationCollectionName);
        Assert.Equal(Path.Combine(paths.LibraryRoot, "Design Tools"), request.Value.DestinationDirectory);
        Assert.True(Directory.Exists(request.Value.DestinationDirectory));
    }

    [Fact]
    public void CreateRequestRejectsCollectionNameThatSanitizesToEmpty()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var request = new IconImportPickerRequestService().CreateRequest(paths, "...");

        Assert.False(request.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, request.Error.Code);
    }
}
