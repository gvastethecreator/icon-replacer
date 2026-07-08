using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppOperationFeedbackServiceTests
{
    [Fact]
    public void FromApplyCreatesSuccessFeedbackWithHistoryAction()
    {
        var target = new TargetItem(TargetKind.Folder, Path.Combine(Path.GetTempPath(), "target-folder"));
        var result = new IconApplyResult(
            TargetKind.Folder,
            RestoreRecord.CreatePending(
                target,
                Path.Combine(Path.GetTempPath(), "blue.ico"),
                new FolderRestoreSnapshot(
                    DesktopIniExisted: false,
                    new Dictionary<string, string?>(),
                    FileAttributes.Directory,
                    null)) with { Status = RestoreRecordStatus.Applied },
            new IconLibraryEntry(
                FullPath: Path.Combine(Path.GetTempPath(), "blue.ico"),
                DisplayName: "blue",
                Category: null,
                LengthBytes: 128),
            DesktopIniPath: Path.Combine(target.FullPath, "desktop.ini"),
            Shortcut: null,
            ExplorerRefreshRequested: true);

        var feedback = new AppOperationFeedbackService().FromApply(result);

        Assert.Equal(AppOperationFeedbackSeverity.Success, feedback.Severity);
        Assert.Equal("Folder icon changed.", feedback.Title);
        Assert.Contains("blue", feedback.Detail);
        Assert.Equal("View history", feedback.ActionLabel);
        Assert.Equal("history", feedback.ActionTarget);
    }

    [Fact]
    public void FromRestoreCreatesShortcutRestoreFeedback()
    {
        var target = new TargetItem(TargetKind.Shortcut, Path.Combine(Path.GetTempPath(), "sample.lnk"));
        var result = new IconRestoreResult(
            TargetKind.Shortcut,
            RestoreRecord.CreatePending(
                target,
                Path.Combine(Path.GetTempPath(), "blue.ico"),
                new ShortcutRestoreSnapshot(null, 0)) with { Status = RestoreRecordStatus.Restored },
            DesktopIniPath: null,
            Shortcut: new ShellLinkInfo(target.FullPath, "C:\\Windows\\notepad.exe", string.Empty, string.Empty, string.Empty, 0, null, 0),
            ExplorerRefreshRequested: true);

        var feedback = new AppOperationFeedbackService().FromRestore(result);

        Assert.Equal(AppOperationFeedbackSeverity.Success, feedback.Severity);
        Assert.Equal("Shortcut icon restored.", feedback.Title);
        Assert.Contains("previous icon state", feedback.Detail);
    }

    [Fact]
    public void FromBatchImportReportsWarningsWhenAnyFileFails()
    {
        var result = new IconBatchImportResult(
            [
                new IconBatchImportItem(
                    "good.ico",
                    IconBatchImportItemStatus.Imported,
                    new IconLibraryEntry("good.ico", "good", null, 128),
                    IconReplacerError.None),
                new IconBatchImportItem(
                    "bad.ico",
                    IconBatchImportItemStatus.Failed,
                    ImportedIcon: null,
                    new IconReplacerError(ErrorCode.InvalidIcon, "Invalid icon."))
            ],
            new IconLibraryStatus("library", "imported", 1, 1, 0, []));

        var feedback = new AppOperationFeedbackService().FromBatchImport(result);

        Assert.Equal(AppOperationFeedbackSeverity.Warning, feedback.Severity);
        Assert.Contains("with issues", feedback.Title);
        Assert.Contains("1 failed", feedback.Detail);
    }

    [Fact]
    public void FromBatchImportReportsInfoWhenEverythingWasReused()
    {
        var result = new IconBatchImportResult(
            [
                new IconBatchImportItem(
                    "same.ico",
                    IconBatchImportItemStatus.ReusedExisting,
                    new IconLibraryEntry("same.ico", "same", null, 128),
                    IconReplacerError.None)
            ],
            new IconLibraryStatus("library", "imported", 1, 1, 0, []));

        var feedback = new AppOperationFeedbackService().FromBatchImport(result);

        Assert.Equal(AppOperationFeedbackSeverity.Info, feedback.Severity);
        Assert.Contains("already exist", feedback.Title);
    }

    [Fact]
    public void FromErrorUsesErrorMessageAndDetail()
    {
        var feedback = new AppOperationFeedbackService().FromError(new IconReplacerError(
            ErrorCode.PermissionDenied,
            "Access denied.",
            "Folder is read-only."));

        Assert.Equal(AppOperationFeedbackSeverity.Error, feedback.Severity);
        Assert.Equal("Access denied.", feedback.Title);
        Assert.Equal("Folder is read-only.", feedback.Detail);
    }
}
