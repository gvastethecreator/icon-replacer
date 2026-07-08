using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconImportPickerRequestService
{
    public const string FileTypeLabel = IconPickerRequestService.FileTypeLabel;

    private static readonly string[] FileExtensions = [".ico"];

    private readonly IconCollectionService _collectionService;

    public IconImportPickerRequestService(IconCollectionService? collectionService = null)
    {
        _collectionService = collectionService ?? new IconCollectionService();
    }

    public OperationResult<IconImportPickerRequestSnapshot> CreateRequest(
        IconLibraryPaths paths,
        string? collectionName = null)
    {
        if (paths is null)
        {
            return OperationResult<IconImportPickerRequestSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "Icon Library paths are required."));
        }

        try
        {
            Directory.CreateDirectory(paths.LibraryRoot);
            Directory.CreateDirectory(paths.ImportedRoot);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Failure(ErrorCode.PermissionDenied, ex.Message);
        }
        catch (IOException ex)
        {
            return Failure(ErrorCode.Unknown, ex.Message);
        }

        var collection = ResolveCollection(paths, collectionName);
        if (!collection.Succeeded || collection.Value is null)
        {
            return OperationResult<IconImportPickerRequestSnapshot>.Failure(collection.Error);
        }

        return OperationResult<IconImportPickerRequestSnapshot>.Success(new IconImportPickerRequestSnapshot(
            CanOpenPicker: true,
            GetTitle(collection.Value),
            paths.LibraryRoot,
            collection.Value.Name,
            collection.Value.FullPath,
            FileTypeLabel,
            FileExtensions,
            AllowMultiple: true,
            IconReplacerError.None,
            DateTimeOffset.UtcNow));

        static OperationResult<IconImportPickerRequestSnapshot> Failure(ErrorCode code, string detail)
        {
            return OperationResult<IconImportPickerRequestSnapshot>.Failure(new IconReplacerError(
                code,
                "The Icon Library could not be prepared for import.",
                detail));
        }
    }

    public OperationResult<IconImportPickerRequestSnapshot> CreateRequestFromEnvironment(
        string? collectionName = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconImportPickerRequestSnapshot>.Failure(paths.Error);
        }

        return CreateRequest(paths.Value, collectionName);
    }

    private OperationResult<IconCollectionSummary> ResolveCollection(
        IconLibraryPaths paths,
        string? collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            return OperationResult<IconCollectionSummary>.Success(new IconCollectionSummary(
                IconLibraryPaths.ImportedFolderName,
                paths.ImportedRoot,
                Directory.EnumerateFiles(paths.ImportedRoot, "*.ico").Count(),
                IsImportedCollection: true));
        }

        var collection = _collectionService.CreateCollection(collectionName, paths);
        if (!collection.Succeeded || collection.Value is null)
        {
            return OperationResult<IconCollectionSummary>.Failure(collection.Error);
        }

        return OperationResult<IconCollectionSummary>.Success(collection.Value.Collection);
    }

    private static string GetTitle(IconCollectionSummary collection)
    {
        return collection.IsImportedCollection
            ? "Import icons"
            : $"Import icons into {collection.Name}";
    }
}
