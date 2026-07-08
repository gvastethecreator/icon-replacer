using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class AppLaunchRequestService
{
    public const string ChangeIconVerbName = "change-icon";
    public const string TargetOption = "--target";
    public const string TargetKindOption = "--target-kind";

    private readonly IconPickerRequestService _pickerRequestService;

    public AppLaunchRequestService(IconPickerRequestService? pickerRequestService = null)
    {
        _pickerRequestService = pickerRequestService ?? new IconPickerRequestService();
    }

    public OperationResult<AppLaunchRequestSnapshot> CreateChangeIconRequest(
        string targetPath,
        IconLibraryPaths paths)
    {
        return CreateChangeIconRequest([new ShellSelectionItem(targetPath)], paths);
    }

    public OperationResult<AppLaunchRequestSnapshot> CreateChangeIconRequest(
        IReadOnlyList<ShellSelectionItem> selection,
        IconLibraryPaths paths)
    {
        var picker = _pickerRequestService.CreateRequest(selection, paths);
        if (!picker.Succeeded || picker.Value is null)
        {
            return OperationResult<AppLaunchRequestSnapshot>.Failure(picker.Error);
        }

        var arguments = picker.Value.CanOpenPicker && picker.Value.Selection.Target is not null
            ? BuildArguments(picker.Value.Selection.Target)
            : [];
        return OperationResult<AppLaunchRequestSnapshot>.Success(new AppLaunchRequestSnapshot(
            AppLaunchVerb.ChangeIcon,
            ChangeIconVerbName,
            picker.Value.RequestedTargetPath,
            picker.Value.Selection,
            picker.Value,
            picker.Value.CanOpenPicker,
            arguments,
            FormatArguments(arguments),
            picker.Value.Error,
            DateTimeOffset.UtcNow));
    }

    public OperationResult<AppLaunchRequestSnapshot> CreateChangeIconRequestFromEnvironment(
        string targetPath)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<AppLaunchRequestSnapshot>.Failure(paths.Error);
        }

        return CreateChangeIconRequest(targetPath, paths.Value);
    }

    public OperationResult<AppLaunchRequestSnapshot> ParseArguments(
        IReadOnlyList<string> arguments,
        IconLibraryPaths paths)
    {
        if (arguments is null || arguments.Count == 0)
        {
            return Invalid("A launch verb is required.");
        }

        if (!string.Equals(arguments[0], ChangeIconVerbName, StringComparison.OrdinalIgnoreCase))
        {
            return Invalid("The launch verb is not supported.", arguments[0]);
        }

        string? targetPath = null;
        bool? isDirectory = null;

        for (var index = 1; index < arguments.Count; index++)
        {
            var argument = arguments[index];
            if (string.Equals(argument, TargetOption, StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= arguments.Count)
                {
                    return Invalid("The launch target path is missing.");
                }

                targetPath = arguments[++index];
            }
            else if (string.Equals(argument, TargetKindOption, StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= arguments.Count)
                {
                    return Invalid("The launch target kind is missing.");
                }

                var kind = ParseTargetKind(arguments[++index]);
                if (!kind.Succeeded)
                {
                    return Invalid("The launch target kind is not supported.", arguments[index]);
                }

                isDirectory = kind.Value == TargetKind.Folder;
            }
            else
            {
                return Invalid("The launch argument is not supported.", argument);
            }
        }

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return Invalid("The launch target path is required.");
        }

        return CreateChangeIconRequest([new ShellSelectionItem(targetPath, isDirectory)], paths);

        static OperationResult<AppLaunchRequestSnapshot> Invalid(
            string message,
            string? detail = null)
        {
            return OperationResult<AppLaunchRequestSnapshot>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                message,
                detail));
        }
    }

    private static IReadOnlyList<string> BuildArguments(TargetItem target)
    {
        return
        [
            ChangeIconVerbName,
            TargetOption,
            target.FullPath,
            TargetKindOption,
            FormatTargetKind(target.Kind)
        ];
    }

    private static OperationResult<TargetKind> ParseTargetKind(string value)
    {
        if (string.Equals(value, "folder", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<TargetKind>.Success(TargetKind.Folder);
        }

        if (string.Equals(value, "shortcut", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<TargetKind>.Success(TargetKind.Shortcut);
        }

        return OperationResult<TargetKind>.Failure(new IconReplacerError(
            ErrorCode.InvalidArgument,
            "The target kind must be folder or shortcut.",
            value));
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
