using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppLocationServiceTests
{
    [Fact]
    public void GetLocationsEnsuresLibraryFolders()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var locations = new AppLocationService().GetLocations(paths);

        Assert.True(locations.Succeeded, locations.Error.Message);
        Assert.NotNull(locations.Value);
        Assert.Contains(locations.Value, location =>
            location.Kind == AppLocationKind.IconLibrary &&
            location.Exists &&
            location.IsDirectory &&
            location.FullPath == paths.LibraryRoot);
        Assert.Contains(locations.Value, location =>
            location.Kind == AppLocationKind.ImportedIcons &&
            location.Exists &&
            location.IsDirectory &&
            location.FullPath == paths.ImportedRoot);
    }

    [Fact]
    public void GetLocationsReportsRestoreStateExistence()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var store = new RestoreRecordStore(paths.RestoreStateFile);
        var record = RestoreRecord.CreatePending(
            new TargetItem(TargetKind.Folder, temp.PathFor("target")),
            temp.PathFor("icon.ico"),
            new FolderRestoreSnapshot(
                DesktopIniExisted: false,
                new Dictionary<string, string?>(),
                FileAttributes.Directory,
                null)) with { Status = RestoreRecordStatus.Restored };
        Assert.True(store.Upsert(record).Succeeded);

        var locations = new AppLocationService().GetLocations(paths);

        Assert.True(locations.Succeeded, locations.Error.Message);
        Assert.Contains(locations.Value!, location =>
            location.Kind == AppLocationKind.RestoreState &&
            location.Exists &&
            !location.IsDirectory &&
            location.FullPath == paths.RestoreStateFile);
    }

    [Fact]
    public void GetLocationsReportsMissingRestoreStateWithoutCreatingFile()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var locations = new AppLocationService().GetLocations(paths);

        Assert.True(locations.Succeeded, locations.Error.Message);
        Assert.Contains(locations.Value!, location =>
            location.Kind == AppLocationKind.RestoreState &&
            !location.Exists &&
            !location.IsDirectory &&
            location.FullPath == paths.RestoreStateFile);
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void CreateOpenRequestEnablesExistingDirectoryLocation()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var request = new AppLocationService().CreateOpenRequest(AppLocationKind.IconLibrary, paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanOpen);
        Assert.Equal(AppLocationKind.IconLibrary, request.Value.Location.Kind);
        Assert.Equal(paths.LibraryRoot, request.Value.TargetPath);
        Assert.Equal("open", request.Value.ShellVerb);
    }

    [Fact]
    public void CreateOpenRequestDisablesMissingRestoreStateFileWithoutCreatingIt()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var request = new AppLocationService().CreateOpenRequest(AppLocationKind.RestoreState, paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.False(request.Value.CanOpen);
        Assert.Equal(ErrorCode.PathNotFound, request.Value.Error.Code);
        Assert.Equal(paths.RestoreStateFile, request.Value.TargetPath);
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void CreateOpenRequestRejectsInvalidLocationKind()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var request = new AppLocationService().CreateOpenRequest((AppLocationKind)999, paths);

        Assert.False(request.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, request.Error.Code);
    }
}
