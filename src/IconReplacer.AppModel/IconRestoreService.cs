using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconRestoreService
{
    private readonly FolderIconService _folderIconService;
    private readonly ShortcutIconService _shortcutIconService;

    public IconRestoreService(
        FolderIconService? folderIconService = null,
        ShortcutIconService? shortcutIconService = null)
    {
        _folderIconService = folderIconService ?? new FolderIconService();
        _shortcutIconService = shortcutIconService ?? new ShortcutIconService();
    }

    public OperationResult<IconRestoreResult> Restore(Guid recordId, IconLibraryPaths libraryPaths)
    {
        var store = new RestoreRecordStore(libraryPaths.RestoreStateFile);
        var record = store.Get(recordId);
        if (!record.Succeeded)
        {
            return OperationResult<IconRestoreResult>.Failure(record.Error);
        }

        if (record.Value is null)
        {
            return OperationResult<IconRestoreResult>.Failure(new IconReplacerError(
                ErrorCode.PathNotFound,
                "The restore record was not found.",
                recordId.ToString()));
        }

        var restore = Restore(record.Value);
        if (!restore.Succeeded || restore.Value is null)
        {
            return restore;
        }

        var save = store.Upsert(restore.Value.RestoreRecord);
        if (!save.Succeeded)
        {
            return OperationResult<IconRestoreResult>.Failure(new IconReplacerError(
                ErrorCode.PartialFailure,
                "The icon was restored, but the restore state could not be updated.",
                save.Error.Detail ?? save.Error.Message));
        }

        return restore;
    }

    public OperationResult<IconRestoreResult> Restore(RestoreRecord restoreRecord)
    {
        if (restoreRecord.Status != RestoreRecordStatus.Applied)
        {
            return OperationResult<IconRestoreResult>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The restore record is not currently applied.",
                restoreRecord.Status.ToString()));
        }

        return restoreRecord.Target.Kind switch
        {
            TargetKind.Folder => RestoreFolder(restoreRecord),
            TargetKind.Shortcut => RestoreShortcut(restoreRecord),
            _ => OperationResult<IconRestoreResult>.Failure(new IconReplacerError(
                ErrorCode.UnsupportedTarget,
                "The restore record target type is not supported."))
        };
    }

    private OperationResult<IconRestoreResult> RestoreFolder(RestoreRecord restoreRecord)
    {
        var result = _folderIconService.Restore(restoreRecord);
        if (!result.Succeeded || result.Value is null)
        {
            return OperationResult<IconRestoreResult>.Failure(result.Error);
        }

        return OperationResult<IconRestoreResult>.Success(new IconRestoreResult(
            TargetKind.Folder,
            result.Value.RestoreRecord,
            result.Value.DesktopIniPath,
            Shortcut: null,
            ExplorerRefreshRequested: result.Value.ExplorerRefreshRequested));
    }

    private OperationResult<IconRestoreResult> RestoreShortcut(RestoreRecord restoreRecord)
    {
        var result = _shortcutIconService.Restore(restoreRecord);
        if (!result.Succeeded || result.Value is null)
        {
            return OperationResult<IconRestoreResult>.Failure(result.Error);
        }

        return OperationResult<IconRestoreResult>.Success(new IconRestoreResult(
            TargetKind.Shortcut,
            result.Value.RestoreRecord,
            DesktopIniPath: null,
            Shortcut: result.Value.Shortcut,
            ExplorerRefreshRequested: result.Value.ExplorerRefreshRequested));
    }
}
