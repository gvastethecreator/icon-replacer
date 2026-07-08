using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppLocationService
{
    private readonly IconLibraryService _iconLibraryService;

    public AppLocationService(IconLibraryService? iconLibraryService = null)
    {
        _iconLibraryService = iconLibraryService ?? new IconLibraryService();
    }

    public OperationResult<IReadOnlyList<AppLocationTarget>> GetLocations(IconLibraryPaths paths)
    {
        var status = _iconLibraryService.EnsureLibrary(paths);
        if (!status.Succeeded)
        {
            return OperationResult<IReadOnlyList<AppLocationTarget>>.Failure(status.Error);
        }

        var appDataRoot = Path.GetDirectoryName(paths.RestoreStateFile) ?? paths.AppDataRoot;
        return OperationResult<IReadOnlyList<AppLocationTarget>>.Success([
            DirectoryTarget(AppLocationKind.IconLibrary, "Icon Library", paths.LibraryRoot),
            DirectoryTarget(AppLocationKind.ImportedIcons, "Imported Icons", paths.ImportedRoot),
            DirectoryTarget(AppLocationKind.AppData, "App Data", appDataRoot),
            FileTarget(AppLocationKind.RestoreState, "Restore State", paths.RestoreStateFile)
        ]);
    }

    public OperationResult<IReadOnlyList<AppLocationTarget>> GetLocationsFromEnvironment()
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IReadOnlyList<AppLocationTarget>>.Failure(paths.Error);
        }

        return GetLocations(paths.Value);
    }

    public OperationResult<AppLocationOpenRequest> CreateOpenRequest(
        AppLocationKind kind,
        IconLibraryPaths paths)
    {
        if (!Enum.IsDefined(kind))
        {
            return OperationResult<AppLocationOpenRequest>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The app location kind is not valid.",
                kind.ToString()));
        }

        var locations = GetLocations(paths);
        if (!locations.Succeeded || locations.Value is null)
        {
            return OperationResult<AppLocationOpenRequest>.Failure(locations.Error);
        }

        var location = locations.Value.First(item => item.Kind == kind);
        if (!location.Exists)
        {
            return OperationResult<AppLocationOpenRequest>.Success(new AppLocationOpenRequest(
                location,
                CanOpen: false,
                location.FullPath,
                ShellVerb: "open",
                new IconReplacerError(
                    ErrorCode.PathNotFound,
                    "The app location does not exist yet.",
                    location.FullPath)));
        }

        return OperationResult<AppLocationOpenRequest>.Success(new AppLocationOpenRequest(
            location,
            CanOpen: true,
            location.FullPath,
            location.IsDirectory ? "open" : "open-file",
            IconReplacerError.None));
    }

    public OperationResult<AppLocationOpenRequest> CreateOpenRequestFromEnvironment(AppLocationKind kind)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppLocationOpenRequest>.Failure(paths.Error);
        }

        return CreateOpenRequest(kind, paths.Value);
    }

    private static AppLocationTarget DirectoryTarget(AppLocationKind kind, string label, string path)
    {
        return new AppLocationTarget(
            kind,
            label,
            Path.GetFullPath(path),
            Directory.Exists(path),
            IsDirectory: true);
    }

    private static AppLocationTarget FileTarget(AppLocationKind kind, string label, string path)
    {
        return new AppLocationTarget(
            kind,
            label,
            Path.GetFullPath(path),
            File.Exists(path),
            IsDirectory: false);
    }
}
