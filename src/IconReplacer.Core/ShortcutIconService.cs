namespace IconReplacer.Core;

public sealed class ShortcutIconService
{
    private readonly IconLibraryImporter _importer;
    private readonly ShellLinkClient _shellLinkClient;
    private readonly IExplorerChangeNotifier _changeNotifier;

    public ShortcutIconService(
        IconLibraryImporter? importer = null,
        ShellLinkClient? shellLinkClient = null,
        IExplorerChangeNotifier? changeNotifier = null)
    {
        _importer = importer ?? new IconLibraryImporter();
        _shellLinkClient = shellLinkClient ?? new ShellLinkClient();
        _changeNotifier = changeNotifier ?? new WindowsExplorerChangeNotifier();
    }

    public OperationResult<ShortcutIconApplyResult> Apply(
        string shortcutPath,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        if (FileSystemPathPolicy.IsRemoteOrUnsupported(shortcutPath))
        {
            return OperationResult<ShortcutIconApplyResult>.Failure(new IconReplacerError(
                ErrorCode.RemotePathUnsupported,
                "Remote or web-backed shortcuts are not supported in V1."));
        }

        var targetResult = TargetItem.FromShellSelection(shortcutPath, isDirectory: false);
        if (!targetResult.Succeeded || targetResult.Value is null)
        {
            return OperationResult<ShortcutIconApplyResult>.Failure(targetResult.Error);
        }

        if (!File.Exists(targetResult.Value.FullPath))
        {
            return OperationResult<ShortcutIconApplyResult>.Failure(new IconReplacerError(
                ErrorCode.PathNotFound,
                "The shortcut target does not exist.",
                targetResult.Value.FullPath));
        }

        var before = _shellLinkClient.Read(targetResult.Value.FullPath);
        if (!before.Succeeded || before.Value is null)
        {
            return OperationResult<ShortcutIconApplyResult>.Failure(before.Error);
        }

        var importedIcon = _importer.Import(iconPath, libraryPaths);
        if (!importedIcon.Succeeded || importedIcon.Value is null)
        {
            return OperationResult<ShortcutIconApplyResult>.Failure(importedIcon.Error);
        }

        var restoreRecord = RestoreRecord.CreatePending(
            targetResult.Value,
            importedIcon.Value.FullPath,
            new ShortcutRestoreSnapshot(before.Value.IconPath, before.Value.IconIndex));

        var after = _shellLinkClient.SetIconLocation(
            targetResult.Value.FullPath,
            importedIcon.Value.FullPath,
            iconIndex: 0);

        if (!after.Succeeded || after.Value is null)
        {
            return OperationResult<ShortcutIconApplyResult>.Failure(after.Error);
        }

        _changeNotifier.NotifyUpdated(targetResult.Value.FullPath);

        return OperationResult<ShortcutIconApplyResult>.Success(new ShortcutIconApplyResult(
            restoreRecord with { Status = RestoreRecordStatus.Applied },
            importedIcon.Value,
            after.Value,
            ExplorerRefreshRequested: true));
    }

    public OperationResult<ShortcutIconRestoreResult> Restore(RestoreRecord restoreRecord)
    {
        if (restoreRecord.Target.Kind != TargetKind.Shortcut ||
            restoreRecord.PreviousState is not ShortcutRestoreSnapshot snapshot)
        {
            return OperationResult<ShortcutIconRestoreResult>.Failure(new IconReplacerError(
                ErrorCode.UnsupportedTarget,
                "The restore record is not for a shortcut target."));
        }

        if (!File.Exists(restoreRecord.Target.FullPath))
        {
            return OperationResult<ShortcutIconRestoreResult>.Failure(new IconReplacerError(
                ErrorCode.PathNotFound,
                "The shortcut target no longer exists.",
                restoreRecord.Target.FullPath));
        }

        var restored = _shellLinkClient.SetIconLocation(
            restoreRecord.Target.FullPath,
            snapshot.PreviousIconPath,
            snapshot.PreviousIconIndex);

        if (!restored.Succeeded || restored.Value is null)
        {
            return OperationResult<ShortcutIconRestoreResult>.Failure(restored.Error);
        }

        _changeNotifier.NotifyUpdated(restoreRecord.Target.FullPath);

        return OperationResult<ShortcutIconRestoreResult>.Success(new ShortcutIconRestoreResult(
            restoreRecord with { Status = RestoreRecordStatus.Restored },
            restored.Value,
            ExplorerRefreshRequested: true));
    }
}
