using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconMenuCommandInvocationService
{
    private readonly IconMenuCommandService _commandService;
    private readonly ShellSelectionService _selectionService;

    public IconMenuCommandInvocationService(
        IconMenuCommandService? commandService = null,
        ShellSelectionService? selectionService = null)
    {
        _commandService = commandService ?? new IconMenuCommandService();
        _selectionService = selectionService ?? new ShellSelectionService();
    }

    public OperationResult<IconMenuCommandInvocationSnapshot> PreviewInvocation(
        string commandId,
        string? targetPath,
        IconLibraryPaths paths,
        IconMenuOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            return OperationResult<IconMenuCommandInvocationSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "A menu command id is required."));
        }

        var commands = _commandService.BuildCommands(paths, options);
        if (!commands.Succeeded || commands.Value is null)
        {
            return OperationResult<IconMenuCommandInvocationSnapshot>.Failure(commands.Error);
        }

        var command = commands.Value.Commands.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, commandId, StringComparison.OrdinalIgnoreCase));
        if (command is null)
        {
            return OperationResult<IconMenuCommandInvocationSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The menu command was not found in the current Icon Library snapshot.",
                commandId));
        }

        if (!command.RequiresTarget)
        {
            return OperationResult<IconMenuCommandInvocationSnapshot>.Success(
                CreateEnabledSnapshot(command, selection: null, resolvedArguments: command.ArgumentTemplate));
        }

        var selection = _selectionService.Evaluate(BuildSelection(targetPath));
        if (!selection.Succeeded || selection.Value is null)
        {
            return OperationResult<IconMenuCommandInvocationSnapshot>.Failure(selection.Error);
        }

        if (!selection.Value.CanShowChangeIcon || selection.Value.Target is null)
        {
            return OperationResult<IconMenuCommandInvocationSnapshot>.Success(new IconMenuCommandInvocationSnapshot(
                command,
                selection.Value,
                CanInvoke: false,
                ResolvedArguments: Array.Empty<string>(),
                DisplayArguments: string.Empty,
                selection.Value.Error,
                DateTimeOffset.UtcNow));
        }

        var resolvedArguments = ResolveArguments(command.ArgumentTemplate, selection.Value.Target);
        return OperationResult<IconMenuCommandInvocationSnapshot>.Success(
            CreateEnabledSnapshot(command, selection.Value, resolvedArguments));
    }

    public OperationResult<IconMenuCommandInvocationSnapshot> PreviewInvocationFromEnvironment(
        string commandId,
        string? targetPath,
        IconMenuOptions? options = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconMenuCommandInvocationSnapshot>.Failure(paths.Error);
        }

        return PreviewInvocation(commandId, targetPath, paths.Value, options);
    }

    private static IReadOnlyList<ShellSelectionItem> BuildSelection(string? targetPath)
    {
        return string.IsNullOrWhiteSpace(targetPath)
            ? []
            : [new ShellSelectionItem(targetPath)];
    }

    private static IconMenuCommandInvocationSnapshot CreateEnabledSnapshot(
        IconMenuCommand command,
        ShellSelectionEvaluation? selection,
        IReadOnlyList<string> resolvedArguments)
    {
        return new IconMenuCommandInvocationSnapshot(
            command,
            selection,
            CanInvoke: true,
            resolvedArguments,
            FormatArguments(resolvedArguments),
            IconReplacerError.None,
            DateTimeOffset.UtcNow);
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
