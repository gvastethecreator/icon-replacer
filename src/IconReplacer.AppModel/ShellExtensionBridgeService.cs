using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class ShellExtensionBridgeService
{
    public const string ProtocolVersion = "icon-replacer-shell-bridge-v1";

    private static readonly string[] SafetyRules =
    [
        "Do not mutate files while Explorer is enumerating the context menu.",
        "Resolve target-required commands only for one local folder or .lnk shortcut.",
        "Use AppModel command ids and arguments instead of inventing native-only verbs.",
        "Defer icon mutation to the packaged app or shared apply path.",
        "Respect IconMenuOptions caps when exposing dynamic icon entries."
    ];

    private readonly ShellManifestContractService _manifestContractService;
    private readonly IconMenuCommandService _commandService;
    private readonly ShellSelectionService _selectionService;

    public ShellExtensionBridgeService(
        ShellManifestContractService? manifestContractService = null,
        IconMenuCommandService? commandService = null,
        ShellSelectionService? selectionService = null)
    {
        _manifestContractService = manifestContractService ?? new ShellManifestContractService();
        _commandService = commandService ?? new IconMenuCommandService();
        _selectionService = selectionService ?? new ShellSelectionService();
    }

    public OperationResult<ShellExtensionBridgeSnapshot> BuildBridge(
        IconLibraryPaths paths,
        string? targetPath = null,
        IconMenuOptions? options = null)
    {
        var manifest = _manifestContractService.GetContract();
        var commands = _commandService.BuildCommands(paths, options);
        if (!commands.Succeeded || commands.Value is null)
        {
            return OperationResult<ShellExtensionBridgeSnapshot>.Failure(commands.Error);
        }

        var selection = _selectionService.Evaluate(BuildSelection(targetPath));
        if (!selection.Succeeded || selection.Value is null)
        {
            return OperationResult<ShellExtensionBridgeSnapshot>.Failure(selection.Error);
        }

        var bridgeCommands = commands.Value.Commands
            .Select(command => ToBridgeCommand(command, selection.Value))
            .ToArray();
        var error = commands.Value.Error.Code != ErrorCode.None
            ? commands.Value.Error
            : selection.Value.Error;

        return OperationResult<ShellExtensionBridgeSnapshot>.Success(new ShellExtensionBridgeSnapshot(
            ProtocolVersion,
            manifest.ExplorerCommandClsid,
            manifest.ShellExtensionDllPath,
            manifest.ThreadingModel,
            manifest.RequiredInterfaces,
            manifest.Targets,
            commands.Value.State,
            commands.Value.StatusMessage,
            commands.Value.TotalIconCount,
            commands.Value.VisibleIconCommandCount,
            commands.Value.OmittedIconCount,
            selection.Value,
            bridgeCommands,
            SafetyRules,
            manifest,
            commands.Value,
            error,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<ShellExtensionBridgeSnapshot> BuildBridgeFromEnvironment(
        string? targetPath = null,
        IconMenuOptions? options = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<ShellExtensionBridgeSnapshot>.Failure(paths.Error);
        }

        return BuildBridge(paths.Value, targetPath, options);
    }

    private static IReadOnlyList<ShellSelectionItem> BuildSelection(string? targetPath)
    {
        return string.IsNullOrWhiteSpace(targetPath)
            ? []
            : [new ShellSelectionItem(targetPath)];
    }

    private static ShellExtensionBridgeCommand ToBridgeCommand(
        IconMenuCommand command,
        ShellSelectionEvaluation selection)
    {
        if (!command.RequiresTarget)
        {
            return CreateEnabledCommand(command, command.ArgumentTemplate);
        }

        if (!selection.CanShowChangeIcon || selection.Target is null)
        {
            return new ShellExtensionBridgeCommand(
                command.Id,
                command.Kind,
                command.Label,
                command.CategoryName,
                command.IconPath,
                command.RequiresTarget,
                CanInvoke: false,
                Array.Empty<string>(),
                DisplayArguments: string.Empty,
                selection.Error);
        }

        return CreateEnabledCommand(command, ResolveArguments(command.ArgumentTemplate, selection.Target));
    }

    private static ShellExtensionBridgeCommand CreateEnabledCommand(
        IconMenuCommand command,
        IReadOnlyList<string> resolvedArguments)
    {
        return new ShellExtensionBridgeCommand(
            command.Id,
            command.Kind,
            command.Label,
            command.CategoryName,
            command.IconPath,
            command.RequiresTarget,
            CanInvoke: true,
            resolvedArguments,
            FormatArguments(resolvedArguments),
            IconReplacerError.None);
    }

    private static IReadOnlyList<string> ResolveArguments(
        IReadOnlyList<string> argumentTemplate,
        TargetItem target)
    {
        return argumentTemplate
            .Select(argument => argument switch
            {
                IconMenuCommandService.TargetPlaceholder => target.FullPath,
                IconMenuCommandService.TargetKindPlaceholder => FormatTargetKind(target.Kind),
                _ => argument
            })
            .ToArray();
    }

    private static string FormatTargetKind(TargetKind kind)
    {
        return kind == TargetKind.Folder ? "folder" : "shortcut";
    }

    private static string FormatArguments(IReadOnlyList<string> arguments)
    {
        return string.Join(" ", arguments.Select(QuoteArgument));
    }

    private static string QuoteArgument(string argument)
    {
        if (argument.Length == 0)
        {
            return "\"\"";
        }

        if (!argument.Any(char.IsWhiteSpace) && !argument.Contains('"'))
        {
            return argument;
        }

        return $"\"{argument.Replace("\"", "\\\"")}\"";
    }
}
