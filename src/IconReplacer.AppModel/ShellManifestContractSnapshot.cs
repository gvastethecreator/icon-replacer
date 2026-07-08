namespace IconReplacer.AppModel;

public sealed record ShellManifestContractSnapshot(
    string PackageName,
    string ApplicationId,
    string ComServerCategory,
    string FileExplorerContextMenusCategory,
    string ComNamespace,
    string Desktop4Namespace,
    string Desktop5Namespace,
    string ExplorerCommandClsid,
    string SurrogateServerDisplayName,
    string ShellExtensionDllPath,
    string ThreadingModel,
    bool RequiresPackageIdentity,
    bool RequiresExplorerRestartAfterInstall,
    IReadOnlyList<string> RequiredInterfaces,
    IReadOnlyList<ShellManifestContextMenuTarget> Targets,
    string ManifestFragment,
    DateTimeOffset RefreshedAt);
