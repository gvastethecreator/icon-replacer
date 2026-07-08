using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconChangePreviewService
{
    private readonly ShellSelectionService _selectionService;
    private readonly IconDetailsService _detailsService;

    public IconChangePreviewService(
        ShellSelectionService? selectionService = null,
        IconDetailsService? detailsService = null)
    {
        _selectionService = selectionService ?? new ShellSelectionService();
        _detailsService = detailsService ?? new IconDetailsService();
    }

    public OperationResult<IconChangePreviewSnapshot> PreviewChange(
        string targetPath,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        return PreviewChange([new ShellSelectionItem(targetPath)], iconPath, libraryPaths);
    }

    public OperationResult<IconChangePreviewSnapshot> PreviewChange(
        IReadOnlyList<ShellSelectionItem> selection,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        if (libraryPaths is null)
        {
            return OperationResult<IconChangePreviewSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "Icon Library paths are required."));
        }

        var selectionResult = _selectionService.Evaluate(selection);
        if (!selectionResult.Succeeded || selectionResult.Value is null)
        {
            return OperationResult<IconChangePreviewSnapshot>.Failure(selectionResult.Error);
        }

        var iconDetailsResult = _detailsService.GetDetails(iconPath, libraryPaths);
        var iconDetails = iconDetailsResult.Succeeded ? iconDetailsResult.Value : null;
        var iconError = iconDetailsResult.Succeeded
            ? IconReplacerError.None
            : iconDetailsResult.Error;
        var error = GetBlockingError(selectionResult.Value, iconError);
        var canApply = error.Code == ErrorCode.None &&
            selectionResult.Value.Target is not null &&
            iconDetails is not null;

        return OperationResult<IconChangePreviewSnapshot>.Success(new IconChangePreviewSnapshot(
            GetRequestedTargetPath(selection),
            iconPath,
            selectionResult.Value,
            iconDetails,
            iconError,
            canApply,
            error,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<IconChangePreviewSnapshot> PreviewChangeFromEnvironment(
        string targetPath,
        string iconPath)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconChangePreviewSnapshot>.Failure(paths.Error);
        }

        return PreviewChange(targetPath, iconPath, paths.Value);
    }

    private static IconReplacerError GetBlockingError(
        ShellSelectionEvaluation selection,
        IconReplacerError iconError)
    {
        if (!selection.CanShowChangeIcon || selection.Target is null)
        {
            return selection.Error;
        }

        if (iconError.Code != ErrorCode.None)
        {
            return iconError;
        }

        return IconReplacerError.None;
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
