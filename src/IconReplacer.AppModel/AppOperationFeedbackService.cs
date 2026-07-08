using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppOperationFeedbackService
{
    public AppOperationFeedback FromApply(IconApplyResult result)
    {
        var targetName = Path.GetFileName(result.RestoreRecord.Target.FullPath);
        var title = result.TargetKind == TargetKind.Folder
            ? "Folder icon changed."
            : "Shortcut icon changed.";
        var refreshDetail = result.ExplorerRefreshRequested
            ? "Explorer refresh was requested."
            : "Explorer may need a manual refresh.";

        return new AppOperationFeedback(
            AppOperationFeedbackSeverity.Success,
            title,
            $"{targetName} now uses {result.ImportedIcon.DisplayName}. {refreshDetail}",
            "View history",
            "history");
    }

    public AppOperationFeedback FromRestore(IconRestoreResult result)
    {
        var targetName = Path.GetFileName(result.RestoreRecord.Target.FullPath);
        var title = result.TargetKind == TargetKind.Folder
            ? "Folder icon restored."
            : "Shortcut icon restored.";
        var refreshDetail = result.ExplorerRefreshRequested
            ? "Explorer refresh was requested."
            : "Explorer may need a manual refresh.";

        return new AppOperationFeedback(
            AppOperationFeedbackSeverity.Success,
            title,
            $"{targetName} was restored to its previous icon state. {refreshDetail}",
            "View history",
            "history");
    }

    public AppOperationFeedback FromImport(IconImportResult result)
    {
        return new AppOperationFeedback(
            AppOperationFeedbackSeverity.Success,
            "Icon imported.",
            $"{result.ImportedIcon.DisplayName} is available in the Icon Library.",
            "Browse icons",
            "icon-browser");
    }

    public AppOperationFeedback FromBatchImport(IconBatchImportResult result)
    {
        return FromImportCounts(
            result.RequestedCount,
            result.ImportedCount,
            result.ReusedExistingCount,
            result.FailedCount,
            collectionName: null);
    }

    public AppOperationFeedback FromCollectionImport(IconCollectionImportResult result)
    {
        return FromImportCounts(
            result.RequestedCount,
            result.ImportedCount,
            result.ReusedExistingCount,
            result.FailedCount,
            result.Collection.Name);
    }

    public AppOperationFeedback FromError(IconReplacerError error)
    {
        return new AppOperationFeedback(
            AppOperationFeedbackSeverity.Error,
            string.IsNullOrWhiteSpace(error.Message) ? "Operation failed." : error.Message,
            string.IsNullOrWhiteSpace(error.Detail) ? "No changes were completed." : error.Detail);
    }

    private static AppOperationFeedback FromImportCounts(
        int requestedCount,
        int importedCount,
        int reusedExistingCount,
        int failedCount,
        string? collectionName)
    {
        var target = string.IsNullOrWhiteSpace(collectionName)
            ? "Icon Library"
            : collectionName;
        var detail = $"{importedCount} imported, {reusedExistingCount} reused, {failedCount} failed out of {requestedCount} selected.";

        if (failedCount > 0)
        {
            return new AppOperationFeedback(
                AppOperationFeedbackSeverity.Warning,
                $"Imported icons into {target} with issues.",
                detail,
                "Review results",
                "import-results");
        }

        if (importedCount == 0 && reusedExistingCount > 0)
        {
            return new AppOperationFeedback(
                AppOperationFeedbackSeverity.Info,
                $"All selected icons already exist in {target}.",
                detail,
                "Browse icons",
                "icon-browser");
        }

        return new AppOperationFeedback(
            AppOperationFeedbackSeverity.Success,
            $"Imported icons into {target}.",
            detail,
            "Browse icons",
            "icon-browser");
    }
}
