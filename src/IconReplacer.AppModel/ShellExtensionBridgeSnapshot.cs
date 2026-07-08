using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed record ShellExtensionBridgeSnapshot(
    string ProtocolVersion,
    string ExplorerCommandClsid,
    string ShellExtensionDllPath,
    string ThreadingModel,
    IReadOnlyList<string> RequiredInterfaces,
    IReadOnlyList<ShellManifestContextMenuTarget> Targets,
    IconMenuState MenuState,
    string StatusMessage,
    int TotalIconCount,
    int VisibleIconCommandCount,
    int OmittedIconCount,
    ShellSelectionEvaluation Selection,
    IReadOnlyList<ShellExtensionBridgeCommand> Commands,
    IReadOnlyList<string> SafetyRules,
    ShellManifestContractSnapshot Manifest,
    IconMenuCommandSnapshot MenuCommands,
    IconReplacerError Error,
    DateTimeOffset RefreshedAt)
{
    public int CommandCount => Commands.Count;

    public int InvocableCommandCount => Commands.Count(command => command.CanInvoke);

    public bool CanShowContextMenu => MenuCommands.IsAvailable;

    public bool HasSupportedTarget => Selection.CanShowChangeIcon;
}
