namespace IconReplacer.Core;

public sealed class FolderIconService
{
    private static readonly string[] IconKeys = ["IconResource", "IconFile", "IconIndex"];
    private readonly IconLibraryImporter _importer;
    private readonly IExplorerChangeNotifier _changeNotifier;

    public FolderIconService(
        IconLibraryImporter? importer = null,
        IExplorerChangeNotifier? changeNotifier = null)
    {
        _importer = importer ?? new IconLibraryImporter();
        _changeNotifier = changeNotifier ?? new WindowsExplorerChangeNotifier();
    }

    public OperationResult<FolderIconApplyResult> Apply(
        string folderPath,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        var folderValidation = FileSystemPathPolicy.ValidateExistingLocalDirectory(folderPath);
        if (!folderValidation.Succeeded || folderValidation.Value is null)
        {
            return OperationResult<FolderIconApplyResult>.Failure(folderValidation.Error);
        }

        var targetResult = TargetItem.FromShellSelection(folderValidation.Value, isDirectory: true);
        if (!targetResult.Succeeded || targetResult.Value is null)
        {
            return OperationResult<FolderIconApplyResult>.Failure(targetResult.Error);
        }

        var importedIcon = _importer.Import(iconPath, libraryPaths);
        if (!importedIcon.Succeeded || importedIcon.Value is null)
        {
            return OperationResult<FolderIconApplyResult>.Failure(importedIcon.Error);
        }

        var desktopIniPath = Path.Combine(targetResult.Value.FullPath, "desktop.ini");
        FolderFileSystemSnapshot? fileSystemSnapshot = null;
        var mutationStarted = false;

        try
        {
            fileSystemSnapshot = CaptureFileSystemState(targetResult.Value.FullPath, desktopIniPath);
            var document = DesktopIniDocument.Load(desktopIniPath);
            var previousValues = document.CaptureValues(DesktopIniDocument.ShellClassInfoSection, IconKeys);
            var snapshot = new FolderRestoreSnapshot(
                fileSystemSnapshot.DesktopIniExisted,
                previousValues,
                fileSystemSnapshot.FolderAttributes,
                fileSystemSnapshot.DesktopIniAttributes);
            var restoreRecord = RestoreRecord.CreatePending(
                targetResult.Value,
                importedIcon.Value.FullPath,
                snapshot);

            mutationStarted = true;
            PrepareDesktopIniForWrite(desktopIniPath);

            document.SetValue(
                DesktopIniDocument.ShellClassInfoSection,
                "IconResource",
                $"{importedIcon.Value.FullPath},0");
            document.RemoveValue(DesktopIniDocument.ShellClassInfoSection, "IconFile");
            document.RemoveValue(DesktopIniDocument.ShellClassInfoSection, "IconIndex");
            document.Save(desktopIniPath);

            File.SetAttributes(desktopIniPath, File.GetAttributes(desktopIniPath) | FileAttributes.Hidden | FileAttributes.System);
            File.SetAttributes(
                targetResult.Value.FullPath,
                fileSystemSnapshot.FolderAttributes | FileAttributes.ReadOnly);

            _changeNotifier.NotifyUpdated(targetResult.Value.FullPath);

            return OperationResult<FolderIconApplyResult>.Success(new FolderIconApplyResult(
                restoreRecord with { Status = RestoreRecordStatus.Applied },
                importedIcon.Value,
                desktopIniPath,
                ExplorerRefreshRequested: true));
        }
        catch (UnauthorizedAccessException ex)
        {
            var rollbackError = mutationStarted && fileSystemSnapshot is not null
                ? TryRestoreFileSystemState(targetResult.Value.FullPath, desktopIniPath, fileSystemSnapshot)
                : null;
            return OperationResult<FolderIconApplyResult>.Failure(new IconReplacerError(
                rollbackError is null ? ErrorCode.PermissionDenied : ErrorCode.PartialFailure,
                rollbackError is null
                    ? "The folder icon could not be changed. The target was left unchanged."
                    : "The folder icon change failed and the previous target state could not be restored completely.",
                FailureDetail(ex, rollbackError)));
        }
        catch (IOException ex)
        {
            var rollbackError = mutationStarted && fileSystemSnapshot is not null
                ? TryRestoreFileSystemState(targetResult.Value.FullPath, desktopIniPath, fileSystemSnapshot)
                : null;
            return OperationResult<FolderIconApplyResult>.Failure(new IconReplacerError(
                ErrorCode.PartialFailure,
                rollbackError is null
                    ? "The folder icon change failed before completion. The target was left unchanged."
                    : "The folder icon change failed and the previous target state could not be restored completely.",
                FailureDetail(ex, rollbackError)));
        }
    }

    public OperationResult<FolderIconRestoreResult> Restore(RestoreRecord restoreRecord)
    {
        if (restoreRecord.Target.Kind != TargetKind.Folder ||
            restoreRecord.PreviousState is not FolderRestoreSnapshot snapshot)
        {
            return OperationResult<FolderIconRestoreResult>.Failure(new IconReplacerError(
                ErrorCode.UnsupportedTarget,
                "The restore record is not for a folder target."));
        }

        var folderValidation = FileSystemPathPolicy.ValidateExistingLocalDirectory(
            restoreRecord.Target.FullPath);
        if (!folderValidation.Succeeded)
        {
            return OperationResult<FolderIconRestoreResult>.Failure(folderValidation.Error);
        }

        var desktopIniPath = Path.Combine(restoreRecord.Target.FullPath, "desktop.ini");
        FolderFileSystemSnapshot? fileSystemSnapshot = null;
        var mutationStarted = false;

        try
        {
            fileSystemSnapshot = CaptureFileSystemState(restoreRecord.Target.FullPath, desktopIniPath);
            var document = DesktopIniDocument.Load(desktopIniPath);
            mutationStarted = true;
            PrepareDesktopIniForWrite(desktopIniPath);

            foreach (var pair in snapshot.PreviousShellClassInfoValues)
            {
                if (pair.Value is null)
                {
                    document.RemoveValue(DesktopIniDocument.ShellClassInfoSection, pair.Key);
                }
                else
                {
                    document.SetValue(DesktopIniDocument.ShellClassInfoSection, pair.Key, pair.Value);
                }
            }

            if (!snapshot.DesktopIniExisted && !document.HasAnyValues)
            {
                if (File.Exists(desktopIniPath))
                {
                    File.Delete(desktopIniPath);
                }
            }
            else
            {
                document.Save(desktopIniPath);

                if (snapshot.PreviousDesktopIniAttributes is { } previousDesktopIniAttributes)
                {
                    File.SetAttributes(desktopIniPath, previousDesktopIniAttributes);
                }
                else if (File.Exists(desktopIniPath))
                {
                    File.SetAttributes(desktopIniPath, FileAttributes.Hidden | FileAttributes.System);
                }
            }

            if (snapshot.PreviousFolderAttributes is { } previousFolderAttributes)
            {
                File.SetAttributes(restoreRecord.Target.FullPath, previousFolderAttributes);
            }

            _changeNotifier.NotifyUpdated(restoreRecord.Target.FullPath);

            return OperationResult<FolderIconRestoreResult>.Success(new FolderIconRestoreResult(
                restoreRecord with { Status = RestoreRecordStatus.Restored },
                desktopIniPath,
                ExplorerRefreshRequested: true));
        }
        catch (UnauthorizedAccessException ex)
        {
            var rollbackError = mutationStarted && fileSystemSnapshot is not null
                ? TryRestoreFileSystemState(restoreRecord.Target.FullPath, desktopIniPath, fileSystemSnapshot)
                : null;
            return OperationResult<FolderIconRestoreResult>.Failure(new IconReplacerError(
                rollbackError is null ? ErrorCode.PermissionDenied : ErrorCode.PartialFailure,
                rollbackError is null
                    ? "The folder icon could not be restored. The applied state was left unchanged."
                    : "The folder icon restore failed and the applied target state could not be recovered completely.",
                FailureDetail(ex, rollbackError)));
        }
        catch (IOException ex)
        {
            var rollbackError = mutationStarted && fileSystemSnapshot is not null
                ? TryRestoreFileSystemState(restoreRecord.Target.FullPath, desktopIniPath, fileSystemSnapshot)
                : null;
            return OperationResult<FolderIconRestoreResult>.Failure(new IconReplacerError(
                ErrorCode.PartialFailure,
                rollbackError is null
                    ? "The folder icon restore failed before completion. The applied state was left unchanged."
                    : "The folder icon restore failed and the applied target state could not be recovered completely.",
                FailureDetail(ex, rollbackError)));
        }
    }

    private static FolderFileSystemSnapshot CaptureFileSystemState(
        string folderPath,
        string desktopIniPath)
    {
        var desktopIniExisted = File.Exists(desktopIniPath);
        return new FolderFileSystemSnapshot(
            desktopIniExisted,
            desktopIniExisted ? File.ReadAllBytes(desktopIniPath) : null,
            desktopIniExisted ? File.GetAttributes(desktopIniPath) : null,
            File.GetAttributes(folderPath));
    }

    private static string? TryRestoreFileSystemState(
        string folderPath,
        string desktopIniPath,
        FolderFileSystemSnapshot snapshot)
    {
        try
        {
            if (snapshot.DesktopIniExisted)
            {
                PrepareDesktopIniForWrite(desktopIniPath);
                File.WriteAllBytes(desktopIniPath, snapshot.DesktopIniContents ?? []);
                if (snapshot.DesktopIniAttributes is { } desktopIniAttributes)
                {
                    File.SetAttributes(desktopIniPath, desktopIniAttributes);
                }
            }
            else if (File.Exists(desktopIniPath))
            {
                PrepareDesktopIniForWrite(desktopIniPath);
                File.Delete(desktopIniPath);
            }

            File.SetAttributes(folderPath, snapshot.FolderAttributes);
            return null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return ex.Message;
        }
    }

    private static string FailureDetail(Exception exception, string? rollbackError)
    {
        return rollbackError is null
            ? exception.Message
            : $"{exception.Message} Rollback failed: {rollbackError}";
    }

    private static void PrepareDesktopIniForWrite(string desktopIniPath)
    {
        if (!File.Exists(desktopIniPath))
        {
            return;
        }

        var attributes = File.GetAttributes(desktopIniPath);
        var writeBlockingAttributes = FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System;
        if ((attributes & writeBlockingAttributes) != 0)
        {
            File.SetAttributes(desktopIniPath, attributes & ~writeBlockingAttributes);
        }
    }

    private sealed record FolderFileSystemSnapshot(
        bool DesktopIniExisted,
        byte[]? DesktopIniContents,
        FileAttributes? DesktopIniAttributes,
        FileAttributes FolderAttributes);
}
