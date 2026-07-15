using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconRestoreService
{
    private readonly FolderIconService _folderIconService;
    private readonly ShortcutIconService _shortcutIconService;
    private readonly TargetMutationCoordinator _mutationCoordinator = new();

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

        return _mutationCoordinator.Run(
            record.Value.Target.FullPath,
            () => RestoreFromStore(recordId, store, libraryPaths));
    }

    private OperationResult<IconRestoreResult> RestoreFromStore(
        Guid recordId,
        RestoreRecordStore store,
        IconLibraryPaths libraryPaths)
    {
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

        var restore = RestoreCore(record.Value);
        if (!restore.Succeeded || restore.Value is null)
        {
            return restore;
        }

        var save = store.Upsert(restore.Value.RestoreRecord);
        if (!save.Succeeded)
        {
            return PersistenceFailure(record.Value, libraryPaths, save.Error);
        }

        return restore;
    }

    private OperationResult<IconRestoreResult> PersistenceFailure(
        RestoreRecord appliedRecord,
        IconLibraryPaths libraryPaths,
        IconReplacerError saveError)
    {
        var reapply = ReapplyTarget(appliedRecord, libraryPaths);
        if (reapply.Succeeded)
        {
            return OperationResult<IconRestoreResult>.Failure(new IconReplacerError(
                saveError.Code,
                "The restored status could not be recorded, so the applied icon was reapplied.",
                saveError.Detail ?? saveError.Message));
        }

        return OperationResult<IconRestoreResult>.Failure(new IconReplacerError(
            ErrorCode.PartialFailure,
            "The icon was restored, but its status could not be saved and the applied icon could not be reapplied.",
            $"Save failed: {saveError.Detail ?? saveError.Message} Reapply failed: {reapply.Error.Detail ?? reapply.Error.Message}"));
    }

    private OperationResult ReapplyTarget(
        RestoreRecord appliedRecord,
        IconLibraryPaths libraryPaths)
    {
        return appliedRecord.Target.Kind switch
        {
            TargetKind.Folder => ToOperationResult(_folderIconService.Apply(
                appliedRecord.Target.FullPath,
                appliedRecord.AppliedIconPath,
                libraryPaths)),
            TargetKind.Shortcut => ToOperationResult(_shortcutIconService.Apply(
                appliedRecord.Target.FullPath,
                appliedRecord.AppliedIconPath,
                libraryPaths)),
            _ => OperationResult.Failure(new IconReplacerError(
                ErrorCode.UnsupportedTarget,
                "The restored target type cannot be reapplied."))
        };
    }

    private static OperationResult ToOperationResult<T>(OperationResult<T> result) =>
        result.Succeeded
            ? OperationResult.Success()
            : OperationResult.Failure(result.Error);

    public OperationResult<IconRestoreResult> Restore(RestoreRecord restoreRecord)
    {
        return _mutationCoordinator.Run(
            restoreRecord.Target.FullPath,
            () => RestoreCore(restoreRecord));
    }

    private OperationResult<IconRestoreResult> RestoreCore(RestoreRecord restoreRecord)
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
