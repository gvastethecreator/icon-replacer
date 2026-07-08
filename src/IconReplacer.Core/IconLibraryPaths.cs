namespace IconReplacer.Core;

public sealed record IconLibraryPaths(
    string UserProfileRoot,
    string LibraryRoot,
    string ImportedRoot,
    string AppDataRoot,
    string RestoreStateFile)
{
    public const string LibraryFolderName = ".icons";
    public const string ImportedFolderName = "Imported";
    public const string AppDataFolderName = "Icon Replacer";
    public const string RestoreStateFileName = "state.json";

    public static OperationResult<IconLibraryPaths> FromEnvironment()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return FromRoots(userProfile, appData);
    }

    public static OperationResult<IconLibraryPaths> FromRoots(string? userProfileRoot, string? appDataRoot)
    {
        if (string.IsNullOrWhiteSpace(userProfileRoot))
        {
            return OperationResult<IconLibraryPaths>.Failure(new IconReplacerError(
                ErrorCode.PathNotFound,
                "The user profile folder could not be resolved."));
        }

        if (string.IsNullOrWhiteSpace(appDataRoot))
        {
            return OperationResult<IconLibraryPaths>.Failure(new IconReplacerError(
                ErrorCode.PathNotFound,
                "The AppData folder could not be resolved."));
        }

        var libraryRoot = Path.Combine(userProfileRoot, LibraryFolderName);
        var importedRoot = Path.Combine(libraryRoot, ImportedFolderName);
        var appRoot = Path.Combine(appDataRoot, AppDataFolderName);
        var restoreStateFile = Path.Combine(appRoot, RestoreStateFileName);

        return OperationResult<IconLibraryPaths>.Success(new IconLibraryPaths(
            Path.GetFullPath(userProfileRoot),
            Path.GetFullPath(libraryRoot),
            Path.GetFullPath(importedRoot),
            Path.GetFullPath(appRoot),
            Path.GetFullPath(restoreStateFile)));
    }
}

