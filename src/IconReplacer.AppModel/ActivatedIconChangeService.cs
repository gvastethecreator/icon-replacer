using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class ActivatedIconChangeService
{
    private readonly AppActivationService _activationService;
    private readonly IconChangePreviewService _previewService;
    private readonly IconChangeService _changeService;

    public ActivatedIconChangeService(
        AppActivationService? activationService = null,
        IconChangePreviewService? previewService = null,
        IconChangeService? changeService = null)
    {
        _activationService = activationService ?? new AppActivationService();
        _previewService = previewService ?? new IconChangePreviewService();
        _changeService = changeService ?? new IconChangeService();
    }

    public OperationResult<ActivatedIconChangePreviewSnapshot> PreviewSelectedIcon(
        IReadOnlyList<string> activationArguments,
        string selectedIconPath,
        IconLibraryPaths paths)
    {
        var activation = _activationService.Activate(activationArguments, paths);
        if (!activation.Succeeded || activation.Value is null)
        {
            return OperationResult<ActivatedIconChangePreviewSnapshot>.Failure(activation.Error);
        }

        var selection = GetSelectionItem(activation.Value);
        if (!selection.Succeeded || selection.Value is null)
        {
            return OperationResult<ActivatedIconChangePreviewSnapshot>.Failure(selection.Error);
        }

        var preview = _previewService.PreviewChange([selection.Value], selectedIconPath, paths);
        if (!preview.Succeeded || preview.Value is null)
        {
            return OperationResult<ActivatedIconChangePreviewSnapshot>.Failure(preview.Error);
        }

        return OperationResult<ActivatedIconChangePreviewSnapshot>.Success(
            new ActivatedIconChangePreviewSnapshot(
                activation.Value,
                preview.Value,
                preview.Value.CanApply,
                preview.Value.Error,
                DateTimeOffset.UtcNow));
    }

    public OperationResult<ActivatedIconChangePreviewSnapshot> PreviewSelectedIconFromEnvironment(
        IReadOnlyList<string> activationArguments,
        string selectedIconPath)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<ActivatedIconChangePreviewSnapshot>.Failure(paths.Error);
        }

        return PreviewSelectedIcon(activationArguments, selectedIconPath, paths.Value);
    }

    public OperationResult<ActivatedIconChangeApplyResult> ApplySelectedIcon(
        IReadOnlyList<string> activationArguments,
        string selectedIconPath,
        IconLibraryPaths paths)
    {
        var activation = _activationService.Activate(activationArguments, paths);
        if (!activation.Succeeded || activation.Value is null)
        {
            return OperationResult<ActivatedIconChangeApplyResult>.Failure(activation.Error);
        }

        var selection = GetSelectionItem(activation.Value);
        if (!selection.Succeeded || selection.Value is null)
        {
            return OperationResult<ActivatedIconChangeApplyResult>.Failure(selection.Error);
        }

        var change = _changeService.ChangeIcon([selection.Value], selectedIconPath, paths);
        if (!change.Succeeded || change.Value is null)
        {
            return OperationResult<ActivatedIconChangeApplyResult>.Failure(change.Error);
        }

        return OperationResult<ActivatedIconChangeApplyResult>.Success(
            new ActivatedIconChangeApplyResult(
                activation.Value,
                change.Value,
                DateTimeOffset.UtcNow));
    }

    public OperationResult<ActivatedIconChangeApplyResult> ApplySelectedIconFromEnvironment(
        IReadOnlyList<string> activationArguments,
        string selectedIconPath)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<ActivatedIconChangeApplyResult>.Failure(paths.Error);
        }

        return ApplySelectedIcon(activationArguments, selectedIconPath, paths.Value);
    }

    private static OperationResult<ShellSelectionItem> GetSelectionItem(AppActivationSnapshot activation)
    {
        if (activation.Kind != AppActivationKind.ChangeIcon || activation.LaunchRequest is null)
        {
            return OperationResult<ShellSelectionItem>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "A change-icon activation is required."));
        }

        if (!activation.CanContinue || activation.LaunchRequest.Selection.Target is null)
        {
            return OperationResult<ShellSelectionItem>.Failure(
                activation.Error.Code == ErrorCode.None
                    ? new IconReplacerError(
                        ErrorCode.InvalidArgument,
                        "The change-icon activation cannot continue.")
                    : activation.Error);
        }

        var target = activation.LaunchRequest.Selection.Target;
        return OperationResult<ShellSelectionItem>.Success(new ShellSelectionItem(
            target.FullPath,
            target.Kind == TargetKind.Folder));
    }
}
