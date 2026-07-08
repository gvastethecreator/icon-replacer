namespace IconReplacer.AppModel;

public sealed record SetupReadinessSnapshot(
    string IconLibraryRoot,
    string ImportedIconsRoot,
    string RestoreStateFile,
    bool IconLibraryExists,
    bool ImportedIconsFolderExists,
    bool CanUseCoreFeatures,
    bool HasIcons,
    int CategoryCount,
    int IconCount,
    int CatalogWarningCount,
    int RestoreRecordCount,
    int RestorableRecordCount,
    int MissingTargetRecordCount,
    ShellIntegrationReadiness ShellIntegration,
    IReadOnlyList<SetupAction> Actions)
{
    public bool NeedsAttention => Actions.Any(action => action.Severity != SetupActionSeverity.Info);
}
