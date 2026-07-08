using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppMenuApplyActivationService
{
    private readonly IconMenuCommandService _commandService;
    private readonly IconMenuCommandInvocationService _invocationService;

    public AppMenuApplyActivationService(
        IconMenuCommandService? commandService = null,
        IconMenuCommandInvocationService? invocationService = null)
    {
        _commandService = commandService ?? new IconMenuCommandService();
        _invocationService = invocationService ?? new IconMenuCommandInvocationService();
    }

    public OperationResult<AppMenuApplyActivationSnapshot> PreviewActivation(
        IReadOnlyList<string> arguments,
        IconLibraryPaths paths,
        IconMenuOptions? options = null)
    {
        if (arguments is null)
        {
            return Invalid("Activation arguments are required.");
        }

        if (arguments.Count != 3 ||
            !string.Equals(arguments[0], IconMenuCommandService.MenuApplyVerbName, StringComparison.OrdinalIgnoreCase))
        {
            return Invalid("A menu-apply activation must be: menu-apply <target> <icon-from-library.ico>.");
        }

        return PreviewActivation(arguments[1], arguments[2], paths, arguments.ToArray(), options);
    }

    public OperationResult<AppMenuApplyActivationSnapshot> PreviewActivation(
        string targetPath,
        string iconPath,
        IconLibraryPaths paths,
        IconMenuOptions? options = null)
    {
        return PreviewActivation(
            targetPath,
            iconPath,
            paths,
            [
                IconMenuCommandService.MenuApplyVerbName,
                targetPath,
                iconPath
            ],
            options);
    }

    public OperationResult<AppMenuApplyActivationSnapshot> PreviewActivationFromEnvironment(
        IReadOnlyList<string> arguments,
        IconMenuOptions? options = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppMenuApplyActivationSnapshot>.Failure(paths.Error);
        }

        return PreviewActivation(arguments, paths.Value, options);
    }

    private OperationResult<AppMenuApplyActivationSnapshot> PreviewActivation(
        string targetPath,
        string iconPath,
        IconLibraryPaths paths,
        IReadOnlyList<string> canonicalArguments,
        IconMenuOptions? options)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return Invalid("A menu-apply target path is required.");
        }

        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return Invalid("A menu-apply icon path is required.");
        }

        var fullIconPath = GetFullPath(iconPath);
        if (!fullIconPath.Succeeded || string.IsNullOrWhiteSpace(fullIconPath.Value))
        {
            return OperationResult<AppMenuApplyActivationSnapshot>.Success(CreateDisabledSnapshot(
                targetPath,
                iconPath,
                Command: null,
                Invocation: null,
                fullIconPath.Error,
                canonicalArguments));
        }

        var commands = _commandService.BuildCommands(paths, options);
        if (!commands.Succeeded || commands.Value is null)
        {
            return OperationResult<AppMenuApplyActivationSnapshot>.Failure(commands.Error);
        }

        var command = commands.Value.Commands.FirstOrDefault(candidate =>
            candidate.Kind == IconMenuCommandKind.ApplyLibraryIcon &&
            string.Equals(candidate.IconPath, fullIconPath.Value, StringComparison.OrdinalIgnoreCase));
        if (command is null)
        {
            return OperationResult<AppMenuApplyActivationSnapshot>.Success(CreateDisabledSnapshot(
                targetPath,
                fullIconPath.Value,
                Command: null,
                Invocation: null,
                new IconReplacerError(
                    ErrorCode.InvalidIcon,
                    "The selected menu icon is not available in the current Icon Library menu.",
                    fullIconPath.Value),
                canonicalArguments));
        }

        var invocation = _invocationService.PreviewInvocation(command.Id, targetPath, paths, options);
        if (!invocation.Succeeded || invocation.Value is null)
        {
            return OperationResult<AppMenuApplyActivationSnapshot>.Failure(invocation.Error);
        }

        var canApply = invocation.Value.CanInvoke &&
            invocation.Value.Command.Kind == IconMenuCommandKind.ApplyLibraryIcon;
        return OperationResult<AppMenuApplyActivationSnapshot>.Success(new AppMenuApplyActivationSnapshot(
            IconMenuCommandService.MenuApplyVerbName,
            targetPath,
            fullIconPath.Value,
            command,
            invocation.Value,
            canApply,
            canApply ? invocation.Value.ResolvedArguments : Array.Empty<string>(),
            canApply ? invocation.Value.DisplayArguments : string.Empty,
            canApply ? IconReplacerError.None : invocation.Value.Error,
            DateTimeOffset.UtcNow));
    }

    private static AppMenuApplyActivationSnapshot CreateDisabledSnapshot(
        string targetPath,
        string iconPath,
        IconMenuCommand? Command,
        IconMenuCommandInvocationSnapshot? Invocation,
        IconReplacerError error,
        IReadOnlyList<string> fallbackArguments)
    {
        return new AppMenuApplyActivationSnapshot(
            IconMenuCommandService.MenuApplyVerbName,
            targetPath,
            iconPath,
            Command,
            Invocation,
            CanApply: false,
            Array.Empty<string>(),
            FormatArguments(fallbackArguments),
            error,
            DateTimeOffset.UtcNow);
    }

    private static OperationResult<string> GetFullPath(string path)
    {
        try
        {
            return OperationResult<string>.Success(Path.GetFullPath(path));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return OperationResult<string>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The menu icon path is not valid.",
                ex.Message));
        }
    }

    private static OperationResult<AppMenuApplyActivationSnapshot> Invalid(
        string message,
        string? detail = null)
    {
        return OperationResult<AppMenuApplyActivationSnapshot>.Failure(new IconReplacerError(
            ErrorCode.InvalidArgument,
            message,
            detail));
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
