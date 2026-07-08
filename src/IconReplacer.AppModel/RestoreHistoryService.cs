using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class RestoreHistoryService
{
    public OperationResult<RestoreHistorySnapshot> GetHistory(
        IconLibraryPaths paths,
        RestoreHistoryFilter filter = RestoreHistoryFilter.All)
    {
        var records = new RestoreRecordStore(paths.RestoreStateFile).List();
        if (!records.Succeeded || records.Value is null)
        {
            return OperationResult<RestoreHistorySnapshot>.Failure(records.Error);
        }

        var summaries = records.Value
            .Select(RestoreRecordSummary.FromRecord)
            .OrderByDescending(record => record.CreatedAt)
            .ToArray();
        var filtered = summaries
            .Where(record => MatchesFilter(record, filter))
            .ToArray();

        return OperationResult<RestoreHistorySnapshot>.Success(new RestoreHistorySnapshot(
            paths.RestoreStateFile,
            filter,
            summaries.Length,
            summaries.Count(record => record.Status == RestoreRecordStatus.Applied),
            summaries.Count(record => record.Status == RestoreRecordStatus.Restored),
            summaries.Count(record => record.CanRestore),
            summaries.Count(IsStale),
            filtered));
    }

    public OperationResult<RestoreHistorySnapshot> GetHistoryFromEnvironment(
        RestoreHistoryFilter filter = RestoreHistoryFilter.All)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<RestoreHistorySnapshot>.Failure(paths.Error);
        }

        return GetHistory(paths.Value, filter);
    }

    private static bool MatchesFilter(RestoreRecordSummary record, RestoreHistoryFilter filter)
    {
        return filter switch
        {
            RestoreHistoryFilter.All => true,
            RestoreHistoryFilter.Restorable => record.CanRestore,
            RestoreHistoryFilter.Applied => record.Status == RestoreRecordStatus.Applied,
            RestoreHistoryFilter.Restored => record.Status == RestoreRecordStatus.Restored,
            RestoreHistoryFilter.Stale => IsStale(record),
            _ => true
        };
    }

    private static bool IsStale(RestoreRecordSummary record)
    {
        return !record.TargetExists || !record.AppliedIconExists;
    }
}
