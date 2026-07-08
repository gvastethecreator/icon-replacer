using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class ActivatedMenuApplyService
{
    private readonly AppActivationService _activationService;
    private readonly IconMenuApplyService _menuApplyService;

    public ActivatedMenuApplyService(
        AppActivationService? activationService = null,
        IconMenuApplyService? menuApplyService = null)
    {
        _activationService = activationService ?? new AppActivationService();
        _menuApplyService = menuApplyService ?? new IconMenuApplyService();
    }

    public OperationResult<ActivatedMenuApplyResult> ApplyActivation(
        IReadOnlyList<string> activationArguments,
        IconLibraryPaths paths)
    {
        var activation = _activationService.Activate(activationArguments, paths);
        if (!activation.Succeeded || activation.Value is null)
        {
            return OperationResult<ActivatedMenuApplyResult>.Failure(activation.Error);
        }

        if (activation.Value.Kind != AppActivationKind.MenuApply ||
            activation.Value.MenuApplyRequest is null)
        {
            return OperationResult<ActivatedMenuApplyResult>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "A menu-apply activation is required."));
        }

        if (!activation.Value.CanContinue)
        {
            return OperationResult<ActivatedMenuApplyResult>.Failure(activation.Value.Error);
        }

        var request = activation.Value.MenuApplyRequest;
        var result = _menuApplyService.ApplyMenuIcon(
            request.RequestedTargetPath,
            request.RequestedIconPath,
            paths);
        if (!result.Succeeded || result.Value is null)
        {
            return OperationResult<ActivatedMenuApplyResult>.Failure(result.Error);
        }

        return OperationResult<ActivatedMenuApplyResult>.Success(new ActivatedMenuApplyResult(
            activation.Value,
            result.Value,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<ActivatedMenuApplyResult> ApplyActivationFromEnvironment(
        IReadOnlyList<string> activationArguments)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<ActivatedMenuApplyResult>.Failure(paths.Error);
        }

        return ApplyActivation(activationArguments, paths.Value);
    }
}
