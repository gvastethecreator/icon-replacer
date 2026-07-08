using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppChangeIconWorkflowService
{
    private readonly AppActivationService _activationService;
    private readonly ActivatedIconChangeService _activatedIconChangeService;

    public AppChangeIconWorkflowService(
        AppActivationService? activationService = null,
        ActivatedIconChangeService? activatedIconChangeService = null)
    {
        _activationService = activationService ?? new AppActivationService();
        _activatedIconChangeService = activatedIconChangeService ?? new ActivatedIconChangeService();
    }

    public OperationResult<AppChangeIconWorkflowSnapshot> GetWorkflow(
        IReadOnlyList<string> activationArguments,
        IconLibraryPaths paths,
        string? selectedIconPath = null)
    {
        if (activationArguments is null)
        {
            return OperationResult<AppChangeIconWorkflowSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "Activation arguments are required."));
        }

        var activation = _activationService.Activate(activationArguments, paths);
        if (!activation.Succeeded || activation.Value is null)
        {
            return OperationResult<AppChangeIconWorkflowSnapshot>.Failure(activation.Error);
        }

        if (activation.Value.Kind != AppActivationKind.ChangeIcon || activation.Value.LaunchRequest is null)
        {
            return OperationResult<AppChangeIconWorkflowSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "A change-icon activation is required."));
        }

        if (!activation.Value.CanContinue)
        {
            return OperationResult<AppChangeIconWorkflowSnapshot>.Success(new AppChangeIconWorkflowSnapshot(
                activation.Value,
                activation.Value.LaunchRequest,
                Preview: null,
                selectedIconPath,
                AppChangeIconWorkflowStep.Blocked,
                CanOpenPicker: false,
                CanPreview: false,
                CanApply: false,
                activation.Value.Error,
                DateTimeOffset.UtcNow));
        }

        if (string.IsNullOrWhiteSpace(selectedIconPath))
        {
            var picker = activation.Value.LaunchRequest.PickerRequest;
            return OperationResult<AppChangeIconWorkflowSnapshot>.Success(new AppChangeIconWorkflowSnapshot(
                activation.Value,
                activation.Value.LaunchRequest,
                Preview: null,
                SelectedIconPath: null,
                picker.CanOpenPicker ? AppChangeIconWorkflowStep.NeedIcon : AppChangeIconWorkflowStep.Blocked,
                picker.CanOpenPicker,
                CanPreview: false,
                CanApply: false,
                picker.CanOpenPicker ? IconReplacerError.None : picker.Error,
                DateTimeOffset.UtcNow));
        }

        var preview = _activatedIconChangeService.PreviewSelectedIcon(activationArguments, selectedIconPath, paths);
        if (!preview.Succeeded || preview.Value is null)
        {
            return OperationResult<AppChangeIconWorkflowSnapshot>.Failure(preview.Error);
        }

        return OperationResult<AppChangeIconWorkflowSnapshot>.Success(new AppChangeIconWorkflowSnapshot(
            activation.Value,
            activation.Value.LaunchRequest,
            preview.Value,
            selectedIconPath,
            preview.Value.CanApply ? AppChangeIconWorkflowStep.ReadyToApply : AppChangeIconWorkflowStep.Blocked,
            CanOpenPicker: true,
            CanPreview: true,
            preview.Value.CanApply,
            preview.Value.Error,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<AppChangeIconWorkflowSnapshot> GetWorkflowFromEnvironment(
        IReadOnlyList<string> activationArguments,
        string? selectedIconPath = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppChangeIconWorkflowSnapshot>.Failure(paths.Error);
        }

        return GetWorkflow(activationArguments, paths.Value, selectedIconPath);
    }
}
