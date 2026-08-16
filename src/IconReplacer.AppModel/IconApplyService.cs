using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconApplyService
{
    private readonly FolderIconService _folderIconService;
    private readonly ShortcutIconService _shortcutIconService;
    private readonly TargetMutationCoordinator _mutationCoordinator = new();

    public IconApplyService(
        FolderIconService? folderIconService = null,
        ShortcutIconService? shortcutIconService = null)
    {
        _folderIconService = folderIconService ?? new FolderIconService();
        _shortcutIconService = shortcutIconService ?? new ShortcutIconService();
    }

    public OperationResult<IconApplyResult> Apply(
        string targetPath,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return OperationResult<IconApplyResult>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "A target path is required."));
        }

        if (FileSystemPathPolicy.IsRemoteOrUnsupported(targetPath))
        {
            return OperationResult<IconApplyResult>.Failure(new IconReplacerError(
                ErrorCode.RemotePathUnsupported,
                "Remote or web-backed targets are not supported in V1."));
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(targetPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return OperationResult<IconApplyResult>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The target path is not valid.",
                ex.Message));
        }

        if (Directory.Exists(fullPath))
        {
            return ApplyToShellSelection(fullPath, isDirectory: true, iconPath, libraryPaths);
        }

        if (!File.Exists(fullPath))
        {
            return OperationResult<IconApplyResult>.Failure(new IconReplacerError(
                ErrorCode.PathNotFound,
                "The target does not exist.",
                fullPath));
        }

        if (!string.Equals(Path.GetExtension(fullPath), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<IconApplyResult>.Failure(new IconReplacerError(
                ErrorCode.UnsupportedTarget,
                "Icon Replacer supports local folders, directory links, and .lnk shortcuts in V1.",
                fullPath));
        }

        return ApplyToShellSelection(fullPath, isDirectory: false, iconPath, libraryPaths);
    }

    public OperationResult<IconApplyResult> ApplyToShellSelection(
        string targetPath,
        bool isDirectory,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        return _mutationCoordinator.Run(
            targetPath,
            () => ApplyToShellSelectionCore(targetPath, isDirectory, iconPath, libraryPaths));
    }

    private OperationResult<IconApplyResult> ApplyToShellSelectionCore(
        string targetPath,
        bool isDirectory,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        var applyResult = isDirectory
            ? ApplyFolder(targetPath, iconPath, libraryPaths)
            : ApplyShortcut(targetPath, iconPath, libraryPaths);

        if (!applyResult.Succeeded || applyResult.Value is null)
        {
            return applyResult;
        }

        var save = new RestoreRecordStore(libraryPaths.RestoreStateFile).Upsert(applyResult.Value.RestoreRecord);
        if (!save.Succeeded)
        {
            return PersistenceFailure(applyResult.Value, save.Error);
        }

        return applyResult;
    }

    private OperationResult<IconApplyResult> PersistenceFailure(
        IconApplyResult applyResult,
        IconReplacerError saveError)
    {
        var rollback = RestoreTarget(applyResult);
        if (rollback.Succeeded)
        {
            return OperationResult<IconApplyResult>.Failure(new IconReplacerError(
                saveError.Code,
                "The icon change could not be recorded, so the target was restored to its previous state.",
                saveError.Detail ?? saveError.Message));
        }

        return OperationResult<IconApplyResult>.Failure(new IconReplacerError(
            ErrorCode.PartialFailure,
            "The icon was changed, but its restore record could not be saved and automatic rollback failed.",
            $"Save failed: {saveError.Detail ?? saveError.Message} Rollback failed: {rollback.Error.Detail ?? rollback.Error.Message}"));
    }

    private OperationResult RestoreTarget(IconApplyResult applyResult)
    {
        return applyResult.TargetKind switch
        {
            TargetKind.Folder => ToOperationResult(_folderIconService.Restore(applyResult.RestoreRecord)),
            TargetKind.Shortcut => ToOperationResult(_shortcutIconService.Restore(applyResult.RestoreRecord)),
            _ => OperationResult.Failure(new IconReplacerError(
                ErrorCode.UnsupportedTarget,
                "The changed target type cannot be rolled back."))
        };
    }

    private static OperationResult ToOperationResult<T>(OperationResult<T> result) =>
        result.Succeeded
            ? OperationResult.Success()
            : OperationResult.Failure(result.Error);

    private OperationResult<IconApplyResult> ApplyFolder(
        string targetPath,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        var result = _folderIconService.Apply(targetPath, iconPath, libraryPaths);
        if (!result.Succeeded || result.Value is null)
        {
            return OperationResult<IconApplyResult>.Failure(result.Error);
        }

        return OperationResult<IconApplyResult>.Success(new IconApplyResult(
            TargetKind.Folder,
            result.Value.RestoreRecord,
            result.Value.ImportedIcon,
            result.Value.DesktopIniPath,
            Shortcut: null,
            ExplorerRefreshRequested: result.Value.ExplorerRefreshRequested));
    }

    private OperationResult<IconApplyResult> ApplyShortcut(
        string targetPath,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        var result = _shortcutIconService.Apply(targetPath, iconPath, libraryPaths);
        if (!result.Succeeded || result.Value is null)
        {
            return OperationResult<IconApplyResult>.Failure(result.Error);
        }

        return OperationResult<IconApplyResult>.Success(new IconApplyResult(
            TargetKind.Shortcut,
            result.Value.RestoreRecord,
            result.Value.ImportedIcon,
            DesktopIniPath: null,
            Shortcut: result.Value.Shortcut,
            ExplorerRefreshRequested: result.Value.ExplorerRefreshRequested));
    }
}
