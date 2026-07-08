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
