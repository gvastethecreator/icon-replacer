using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppRestoreWorkflowService
{
    private readonly RestoreHistoryService _historyService;
    private readonly IconRestorePreviewService _previewService;

    public AppRestoreWorkflowService(
        RestoreHistoryService? historyService = null,
        IconRestorePreviewService? previewService = null)
    {
        _historyService = historyService ?? new RestoreHistoryService();
        _previewService = previewService ?? new IconRestorePreviewService();
    }

    public OperationResult<AppRestoreWorkflowSnapshot> GetWorkflow(
        IconLibraryPaths paths,
        Guid? selectedRecordId = null,
        RestoreHistoryFilter filter = RestoreHistoryFilter.All)
    {
        if (paths is null)
        {
            return OperationResult<AppRestoreWorkflowSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "Icon Library paths are required."));
        }

        var history = _historyService.GetHistory(paths, filter);
        if (!history.Succeeded || history.Value is null)
        {
            return OperationResult<AppRestoreWorkflowSnapshot>.Failure(history.Error);
        }

        if (selectedRecordId is null)
        {
            return OperationResult<AppRestoreWorkflowSnapshot>.Success(new AppRestoreWorkflowSnapshot(
                history.Value,
                Preview: null,
                SelectedRecordId: null,
                AppRestoreWorkflowStep.NeedRecord,
                CanPreview: false,
                CanRestore: false,
                IconReplacerError.None,
                DateTimeOffset.UtcNow));
        }

        var preview = _previewService.PreviewRestore(selectedRecordId.Value, paths);
        if (!preview.Succeeded || preview.Value is null)
        {
            return OperationResult<AppRestoreWorkflowSnapshot>.Success(new AppRestoreWorkflowSnapshot(
                history.Value,
                Preview: null,
                selectedRecordId,
                AppRestoreWorkflowStep.Blocked,
                CanPreview: false,
                CanRestore: false,
                preview.Error,
                DateTimeOffset.UtcNow));
        }

        return OperationResult<AppRestoreWorkflowSnapshot>.Success(new AppRestoreWorkflowSnapshot(
            history.Value,
            preview.Value,
            selectedRecordId,
            preview.Value.CanRestore ? AppRestoreWorkflowStep.ReadyToRestore : AppRestoreWorkflowStep.Blocked,
            CanPreview: true,
            preview.Value.CanRestore,
            preview.Value.Error,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<AppRestoreWorkflowSnapshot> GetWorkflowFromEnvironment(
        Guid? selectedRecordId = null,
        RestoreHistoryFilter filter = RestoreHistoryFilter.All)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppRestoreWorkflowSnapshot>.Failure(paths.Error);
        }

        return GetWorkflow(paths.Value, selectedRecordId, filter);
    }
}
