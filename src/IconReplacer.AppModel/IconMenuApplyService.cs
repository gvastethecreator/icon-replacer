using IconReplacer.Core;

namespace IconReplacer.AppModel;

public sealed class IconMenuApplyService
{
    private readonly ShellSelectionService _selectionService;
    private readonly IconCatalogService _catalogService;
    private readonly IconChangeService _changeService;

    public IconMenuApplyService(
        ShellSelectionService? selectionService = null,
        IconCatalogService? catalogService = null,
        IconChangeService? changeService = null)
    {
        _selectionService = selectionService ?? new ShellSelectionService();
        _catalogService = catalogService ?? new IconCatalogService();
        _changeService = changeService ?? new IconChangeService();
    }

    public OperationResult<IconMenuApplyResult> ApplyMenuIcon(
        string targetPath,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        return ApplyMenuIcon([new ShellSelectionItem(targetPath)], iconPath, libraryPaths);
    }

    public OperationResult<IconMenuApplyResult> ApplyMenuIcon(
        IReadOnlyList<ShellSelectionItem> selection,
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        var selectionResult = _selectionService.Evaluate(selection);
        if (!selectionResult.Succeeded || selectionResult.Value is null)
        {
            return OperationResult<IconMenuApplyResult>.Failure(selectionResult.Error);
        }

        if (!selectionResult.Value.CanShowChangeIcon || selectionResult.Value.Target is null)
        {
            return OperationResult<IconMenuApplyResult>.Failure(selectionResult.Value.Error);
        }

        var menuIcon = ResolveMenuIcon(iconPath, libraryPaths);
        if (!menuIcon.Succeeded || menuIcon.Value is null)
        {
            return OperationResult<IconMenuApplyResult>.Failure(menuIcon.Error);
        }

        var target = selectionResult.Value.Target;
        var change = _changeService.ChangeIcon(
            [new ShellSelectionItem(target.FullPath, target.Kind == TargetKind.Folder)],
            menuIcon.Value.FullPath,
            libraryPaths);
        if (!change.Succeeded || change.Value is null)
        {
            return OperationResult<IconMenuApplyResult>.Failure(change.Error);
        }

        return OperationResult<IconMenuApplyResult>.Success(new IconMenuApplyResult(
            new IconMenuItem(menuIcon.Value.DisplayName, menuIcon.Value.FullPath),
            change.Value));
    }

    public OperationResult<IconMenuApplyResult> ApplyMenuIconFromEnvironment(
        string targetPath,
        string iconPath)
    {
        var paths = IconLibraryPaths.FromEnvironment();
        if (!paths.Succeeded || paths.Value is null)
        {
            return OperationResult<IconMenuApplyResult>.Failure(paths.Error);
        }

        return ApplyMenuIcon(targetPath, iconPath, paths.Value);
    }

    private OperationResult<IconLibraryEntry> ResolveMenuIcon(
        string iconPath,
        IconLibraryPaths libraryPaths)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return OperationResult<IconLibraryEntry>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "A menu icon path is required."));
        }

        if (FileSystemPathPolicy.IsRemoteOrUnsupported(iconPath))
        {
            return OperationResult<IconLibraryEntry>.Failure(new IconReplacerError(
                ErrorCode.RemotePathUnsupported,
                "Menu icons must come from the local Icon Library.",
                iconPath));
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(iconPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return OperationResult<IconLibraryEntry>.Failure(new IconReplacerError(
                ErrorCode.InvalidArgument,
                "The menu icon path is not valid.",
                ex.Message));
        }

        var catalog = _catalogService.Scan(libraryPaths);
        if (!catalog.Succeeded || catalog.Value is null)
        {
            return OperationResult<IconLibraryEntry>.Failure(catalog.Error);
        }

        var entry = catalog.Value.Entries.FirstOrDefault(icon =>
            string.Equals(icon.FullPath, fullPath, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            return OperationResult<IconLibraryEntry>.Failure(new IconReplacerError(
                ErrorCode.InvalidIcon,
                "The selected menu icon is not in the Icon Library.",
                fullPath));
        }

        return OperationResult<IconLibraryEntry>.Success(entry);
    }
}
