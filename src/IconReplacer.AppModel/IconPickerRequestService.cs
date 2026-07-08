using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconPickerRequestService
{
    public const string FileTypeLabel = "Icon files (*.ico)";

    private static readonly string[] FileExtensions = [".ico"];

    private readonly ShellSelectionService _selectionService;

    public IconPickerRequestService(ShellSelectionService? selectionService = null)
    {
        _selectionService = selectionService ?? new ShellSelectionService();
    }

    public OperationResult<IconPickerRequestSnapshot> CreateRequest(
        string targetPath,
        IconLibraryPaths paths)
    {
        return CreateRequest([new ShellSelectionItem(targetPath)], paths);
    }

    public OperationResult<IconPickerRequestSnapshot> CreateRequest(
        IReadOnlyList<ShellSelectionItem> selection,
        IconLibraryPaths paths)
    {
        if (paths is null)
        {
            return OperationResult<IconPickerRequestSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "Icon Library paths are required."));
        }

        var selectionResult = _selectionService.Evaluate(selection);
        if (!selectionResult.Succeeded || selectionResult.Value is null)
        {
            return OperationResult<IconPickerRequestSnapshot>.Failure(selectionResult.Error);
        }

        var canOpenPicker = selectionResult.Value.CanShowChangeIcon &&
            selectionResult.Value.Target is not null;
        if (canOpenPicker)
        {
            var ensure = EnsurePickerDirectories(paths);
            if (!ensure.Succeeded)
            {
                return OperationResult<IconPickerRequestSnapshot>.Failure(ensure.Error);
            }
        }

        var title = selectionResult.Value.Target is null
            ? "Select icon"
            : $"Select icon for {Path.GetFileName(selectionResult.Value.Target.FullPath)}";

        return OperationResult<IconPickerRequestSnapshot>.Success(new IconPickerRequestSnapshot(
            GetRequestedTargetPath(selection),
            selectionResult.Value,
            canOpenPicker,
            title,
            paths.LibraryRoot,
            FileTypeLabel,
            FileExtensions,
            AllowMultiple: false,
            canOpenPicker ? IconReplacerError.None : selectionResult.Value.Error,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<IconPickerRequestSnapshot> CreateRequestFromEnvironment(string targetPath)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconPickerRequestSnapshot>.Failure(paths.Error);
        }

        return CreateRequest(targetPath, paths.Value);
    }

    private static OperationResult EnsurePickerDirectories(IconLibraryPaths paths)
    {
        try
        {
            Directory.CreateDirectory(paths.LibraryRoot);
            Directory.CreateDirectory(paths.ImportedRoot);
            return OperationResult.Success();
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "The Icon Library could not be prepared for the file picker.",
                ex.Message));
        }
        catch (IOException ex)
        {
            return OperationResult.Failure(new IconReplacerError(
                ErrorCode.Unknown,
                "The Icon Library could not be prepared for the file picker.",
                ex.Message));
        }
    }

    private static string GetRequestedTargetPath(IReadOnlyList<ShellSelectionItem> selection)
    {
        return selection.Count switch
        {
            0 => string.Empty,
            1 => selection[0].Path,
            _ => $"{selection.Count} selected items"
        };
    }
}
