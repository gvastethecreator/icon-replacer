using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconRestorePreviewService
{
    public OperationResult<IconRestorePreviewSnapshot> PreviewRestore(
        Guid recordId,
        IconLibraryPaths libraryPaths)
    {
        if (libraryPaths is null)
        {
            return OperationResult<IconRestorePreviewSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "Icon Library paths are required."));
        }

        var store = new RestoreRecordStore(libraryPaths.RestoreStateFile);
        var record = store.Get(recordId);
        if (!record.Succeeded)
        {
            return OperationResult<IconRestorePreviewSnapshot>.Failure(record.Error);
        }

        if (record.Value is null)
        {
            return OperationResult<IconRestorePreviewSnapshot>.Failure(new IconReplacerError(
                ErrorCode.PathNotFound,
                "The restore record was not found.",
                recordId.ToString()));
        }

        return OperationResult<IconRestorePreviewSnapshot>.Success(CreateSnapshot(record.Value));
    }

    public OperationResult<IconRestorePreviewSnapshot> PreviewRestoreFromEnvironment(Guid recordId)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconRestorePreviewSnapshot>.Failure(paths.Error);
        }

        return PreviewRestore(recordId, paths.Value);
    }

    private static IconRestorePreviewSnapshot CreateSnapshot(RestoreRecord record)
    {
        var summary = RestoreRecordSummary.FromRecord(record);
        var error = GetBlockingError(summary);
        var canRestore = error.Code == ErrorCode.None && summary.CanRestore;

        return new IconRestorePreviewSnapshot(
            summary,
            DescribePreviousState(record),
            DescribeRestoreAction(summary, canRestore),
            canRestore,
            GetWarningText(summary),
            error,
            DateTimeOffset.UtcNow);
    }

    private static IconReplacerError GetBlockingError(RestoreRecordSummary summary)
    {
        if (summary.Status != RestoreRecordStatus.Applied)
        {
            return new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The restore record is not currently applied.",
                summary.Status.ToString());
        }

        if (!summary.TargetExists)
        {
            return new IconReplacerError(
                ErrorCode.PathNotFound,
                "The restore target does not exist.",
                summary.TargetPath);
        }

        return IconReplacerError.None;
    }

    private static string? GetWarningText(RestoreRecordSummary summary)
    {
        if (summary.Status == RestoreRecordStatus.Applied &&
            summary.TargetExists &&
            !summary.AppliedIconExists)
        {
            return "The applied icon file is missing, but restore can use the saved previous state.";
        }

        return null;
    }

    private static string DescribeRestoreAction(RestoreRecordSummary summary, bool canRestore)
    {
        if (!canRestore)
        {
            return "Restore is disabled until the blocking issue is resolved.";
        }

        return summary.TargetKind switch
        {
            TargetKind.Folder => "Restore folder icon metadata from the saved desktop.ini snapshot.",
            TargetKind.Shortcut => "Restore the shortcut icon location from the saved .lnk snapshot.",
            _ => "Restore the target icon from the saved state."
        };
    }

    private static string DescribePreviousState(RestoreRecord record)
    {
        return record.PreviousState switch
        {
            FolderRestoreSnapshot folder => DescribeFolderPreviousState(folder),
            ShortcutRestoreSnapshot shortcut => DescribeShortcutPreviousState(shortcut),
            _ => "Unsupported previous state."
        };
    }

    private static string DescribeFolderPreviousState(FolderRestoreSnapshot snapshot)
    {
        if (!snapshot.DesktopIniExisted)
        {
            return "No previous desktop.ini existed; restore will remove Icon Replacer's desktop.ini when safe.";
        }

        if (snapshot.PreviousShellClassInfoValues.Count == 0)
        {
            return "Previous desktop.ini existed without tracked icon values.";
        }

        return $"Previous desktop.ini existed with {snapshot.PreviousShellClassInfoValues.Count} tracked ShellClassInfo value(s).";
    }

    private static string DescribeShortcutPreviousState(ShortcutRestoreSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot.PreviousIconPath))
        {
            return "Shortcut had no custom icon path; restore will clear the custom icon location.";
        }

        return $"Shortcut icon was {snapshot.PreviousIconPath},{snapshot.PreviousIconIndex}.";
    }
}
