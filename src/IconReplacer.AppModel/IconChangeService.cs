using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconChangeService
{
    private readonly ShellSelectionService _selectionService;
    private readonly IconApplyService _applyService;

    public IconChangeService(
        ShellSelectionService? selectionService = null,
        IconApplyService? applyService = null)
    {
        _selectionService = selectionService ?? new ShellSelectionService();
        _applyService = applyService ?? new IconApplyService();
    }

    public OperationResult<IconChangeResult> ChangeIcon(
        string targetPath,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        return ChangeIcon([new ShellSelectionItem(targetPath)], iconPath, libraryPaths);
    }

    public OperationResult<IconChangeResult> ChangeIcon(
        IReadOnlyList<ShellSelectionItem> selection,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        var selectionResult = _selectionService.Evaluate(selection);
        if (!selectionResult.Succeeded || selectionResult.Value is null)
        {
            return OperationResult<IconChangeResult>.Failure(selectionResult.Error);
        }

        if (!selectionResult.Value.CanShowChangeIcon || selectionResult.Value.Target is null)
        {
            return OperationResult<IconChangeResult>.Failure(selectionResult.Value.Error);
        }

        var target = selectionResult.Value.Target;
        var applyResult = _applyService.ApplyToShellSelection(
            target.FullPath,
            target.Kind == TargetKind.Folder,
            iconPath,
            libraryPaths);
        if (!applyResult.Succeeded || applyResult.Value is null)
        {
            return OperationResult<IconChangeResult>.Failure(applyResult.Error);
        }

        return OperationResult<IconChangeResult>.Success(new IconChangeResult(
            selectionResult.Value,
            applyResult.Value));
    }
}
