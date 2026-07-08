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
        _changeNotifier = changeNotifier ?? new NoOpExplorerChangeNotifier();
    }

    public OperationResult<FolderIconApplyResult> Apply(
        string folderPath,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        if (FileSystemPathPolicy.IsRemoteOrUnsupported(folderPath))
        {
            return OperationResult<FolderIconApplyResult>.Failure(new IconReplacerError(
                ErrorCode.RemotePathUnsupported,
                "Remote or web-backed folders are not supported in V1."));
        }

        var targetResult = TargetItem.FromShellSelection(folderPath, isDirectory: true);
        if (!targetResult.Succeeded || targetResult.Value is null)
        {
            return OperationResult<FolderIconApplyResult>.Failure(targetResult.Error);
        }

        if (!Directory.Exists(targetResult.Value.FullPath))
        {
            return OperationResult<FolderIconApplyResult>.Failure(new IconReplacerError(
                ErrorCode.PathNotFound,
                "The folder target does not exist.",
                targetResult.Value.FullPath));
        }

        var importedIcon = _importer.Import(iconPath, libraryPaths);
        if (!importedIcon.Succeeded || importedIcon.Value is null)
        {
            return OperationResult<FolderIconApplyResult>.Failure(importedIcon.Error);
        }

        var desktopIniPath = Path.Combine(targetResult.Value.FullPath, "desktop.ini");
        var desktopIniExisted = File.Exists(desktopIniPath);
        var previousDesktopIniAttributes = desktopIniExisted ? File.GetAttributes(desktopIniPath) : (FileAttributes?)null;
        var previousFolderAttributes = File.GetAttributes(targetResult.Value.FullPath);
        var document = DesktopIniDocument.Load(desktopIniPath);
        var previousValues = document.CaptureValues(DesktopIniDocument.ShellClassInfoSection, IconKeys);
        var snapshot = new FolderRestoreSnapshot(
            desktopIniExisted,
            previousValues,
            previousFolderAttributes,
            previousDesktopIniAttributes);

        var restoreRecord = RestoreRecord.CreatePending(
            targetResult.Value,
            importedIcon.Value.FullPath,
            snapshot);

        try
        {
            PrepareDesktopIniForWrite(desktopIniPath);

            document.SetValue(
                DesktopIniDocument.ShellClassInfoSection,
                "IconResource",
                $"{importedIcon.Value.FullPath},0");
            document.RemoveValue(DesktopIniDocument.ShellClassInfoSection, "IconFile");
            document.RemoveValue(DesktopIniDocument.ShellClassInfoSection, "IconIndex");
            document.Save(desktopIniPath);

            File.SetAttributes(desktopIniPath, File.GetAttributes(desktopIniPath) | FileAttributes.Hidden | FileAttributes.System);
            File.SetAttributes(targetResult.Value.FullPath, previousFolderAttributes | FileAttributes.ReadOnly);

            _changeNotifier.NotifyUpdated(targetResult.Value.FullPath);

            return OperationResult<FolderIconApplyResult>.Success(new FolderIconApplyResult(
                restoreRecord with { Status = RestoreRecordStatus.Applied },
                importedIcon.Value,
                desktopIniPath,
                ExplorerRefreshRequested: true));
        }
        catch (UnauthorizedAccessException ex)
        {
            return OperationResult<FolderIconApplyResult>.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "The folder icon could not be changed.",
                ex.Message));
        }
        catch (IOException ex)
        {
            return OperationResult<FolderIconApplyResult>.Failure(new IconReplacerError(
                ErrorCode.PartialFailure,
                "The folder icon change failed before completion.",
                ex.Message));
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

        if (!Directory.Exists(restoreRecord.Target.FullPath))
        {
            return OperationResult<FolderIconRestoreResult>.Failure(new IconReplacerError(
                ErrorCode.PathNotFound,
                "The folder target no longer exists.",
                restoreRecord.Target.FullPath));
        }

        var desktopIniPath = Path.Combine(restoreRecord.Target.FullPath, "desktop.ini");
        var document = DesktopIniDocument.Load(desktopIniPath);

        try
        {
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
            return OperationResult<FolderIconRestoreResult>.Failure(new IconReplacerError(
                ErrorCode.PermissionDenied,
                "The folder icon could not be restored.",
                ex.Message));
        }
        catch (IOException ex)
        {
            return OperationResult<FolderIconRestoreResult>.Failure(new IconReplacerError(
                ErrorCode.PartialFailure,
                "The folder icon restore failed before completion.",
                ex.Message));
        }
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
}
