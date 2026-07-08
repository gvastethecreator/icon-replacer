using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class RecentChangesService
{
    private readonly RestoreHistoryService _restoreHistoryService;

    public RecentChangesService(RestoreHistoryService? restoreHistoryService = null)
    {
        _restoreHistoryService = restoreHistoryService ?? new RestoreHistoryService();
    }

    public OperationResult<RecentChangesSnapshot> GetRecentChanges(
        IconLibraryPaths paths,
        RestoreHistoryFilter filter = RestoreHistoryFilter.All)
    {
        var history = _restoreHistoryService.GetHistory(paths, filter);
        if (!history.Succeeded || history.Value is null)
        {
            return OperationResult<RecentChangesSnapshot>.Failure(history.Error);
        }

        var items = history.Value.Records
            .Select(RecentChangeItem.FromSummary)
            .ToArray();

        return OperationResult<RecentChangesSnapshot>.Success(new RecentChangesSnapshot(
            history.Value.RestoreStateFile,
            history.Value.Filter,
            history.Value.TotalCount,
            items.Length,
            items.Count(item => item.IsRestoreEnabled),
            items.Count(item => item.RestoreActionState == RecentChangeActionState.EnabledWithWarning),
            items.Count(item => item.RestoreActionState == RecentChangeActionState.Disabled),
            items));
    }

    public OperationResult<RecentChangesSnapshot> GetRecentChangesFromEnvironment(
        RestoreHistoryFilter filter = RestoreHistoryFilter.All)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<RecentChangesSnapshot>.Failure(paths.Error);
        }

        return GetRecentChanges(paths.Value, filter);
    }
}
