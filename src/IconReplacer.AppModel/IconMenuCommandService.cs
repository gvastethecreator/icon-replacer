using System.Security.Cryptography;
using System.Text;
using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconMenuCommandService
{
    public const string ChangeIconCommandId = "change-icon";
    public const string OpenAppCommandId = "open-app";
    public const string TargetPlaceholder = "{target}";
    public const string TargetKindPlaceholder = "{target-kind}";
    public const string MenuApplyVerbName = "menu-apply";

    private readonly IconMenuService _menuService;

    public IconMenuCommandService(IconMenuService? menuService = null)
    {
        _menuService = menuService ?? new IconMenuService();
    }

    public OperationResult<IconMenuCommandSnapshot> BuildCommands(
        IconLibraryPaths paths,
        IconMenuOptions? options = null)
    {
        var menu = _menuService.BuildSnapshot(paths, options);
        if (!menu.Succeeded || menu.Value is null)
        {
            return OperationResult<IconMenuCommandSnapshot>.Failure(menu.Error);
        }

        return OperationResult<IconMenuCommandSnapshot>.Success(FromMenuSnapshot(menu.Value));
    }

    public OperationResult<IconMenuCommandSnapshot> BuildCommandsFromEnvironment(IconMenuOptions? options = null)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconMenuCommandSnapshot>.Failure(paths.Error);
        }

        return BuildCommands(paths.Value, options);
    }

    private static IconMenuCommandSnapshot FromMenuSnapshot(IconMenuSnapshot menu)
    {
        var commands = new List<IconMenuCommand>
        {
            CreateChangeIconCommand(menu.ChangeIconCommandLabel)
        };

        commands.AddRange(menu.RootIcons.Select(item => CreateIconCommand(item, categoryName: null)));

        foreach (var category in menu.Categories)
        {
            commands.AddRange(category.Items.Select(item => CreateIconCommand(item, category.Name)));
        }

        if (menu.IsTruncated)
        {
            commands.Add(CreateOpenAppCommand($"Open Icon Replacer ({menu.OmittedIconCount} hidden)"));
        }
        else if (menu.State is IconMenuState.Empty or IconMenuState.Unavailable)
        {
            commands.Add(CreateOpenAppCommand("Open Icon Replacer"));
        }

        return new IconMenuCommandSnapshot(
            menu.IconLibraryRoot,
            menu.State,
            menu.StatusMessage,
            menu.TotalIconCount,
            menu.VisibleIconCount,
            menu.OmittedIconCount,
            commands,
            menu.Error,
            menu.RefreshedAt);
    }

    private static IconMenuCommand CreateChangeIconCommand(string label)
    {
        var arguments = new[]
        {
            AppLaunchRequestService.ChangeIconVerbName,
            AppLaunchRequestService.TargetOption,
            TargetPlaceholder,
            AppLaunchRequestService.TargetKindOption,
            TargetKindPlaceholder
        };

        return new IconMenuCommand(
            ChangeIconCommandId,
            IconMenuCommandKind.ChangeIcon,
            label,
            CategoryName: null,
            IconPath: null,
            RequiresTarget: true,
            arguments,
            FormatArguments(arguments));
    }

    private static IconMenuCommand CreateIconCommand(IconMenuItem item, string? categoryName)
    {
        var arguments = new[]
        {
            MenuApplyVerbName,
            TargetPlaceholder,
            item.IconPath
        };

        return new IconMenuCommand(
            CreateIconCommandId(item.IconPath),
            IconMenuCommandKind.ApplyLibraryIcon,
            item.DisplayName,
            categoryName,
            item.IconPath,
            RequiresTarget: true,
            arguments,
            FormatArguments(arguments));
    }

    private static IconMenuCommand CreateOpenAppCommand(string label)
    {
        return new IconMenuCommand(
            OpenAppCommandId,
            IconMenuCommandKind.OpenApp,
            label,
            CategoryName: null,
            IconPath: null,
            RequiresTarget: false,
            Array.Empty<string>(),
            string.Empty);
    }

    private static string CreateIconCommandId(string iconPath)
    {
        var normalizedPath = Path.GetFullPath(iconPath).ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath));
        return $"icon:{Convert.ToHexString(hash.AsSpan(0, 6)).ToLowerInvariant()}";
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
