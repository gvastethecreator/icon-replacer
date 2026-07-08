using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppCommandServiceTests
{
    [Fact]
    public void GetCommandsForHomeIncludesPrimaryNavigation()
    {
        var window = CreateHomeWindow();

        var commands = new AppCommandService().GetCommands(window);

        Assert.True(commands.Succeeded, commands.Error.Message);
        Assert.NotNull(commands.Value);
        Assert.Equal(AppNavigationRouteIds.Home, commands.Value.RouteId);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "import-icons" &&
            command.Kind == AppCommandKind.Navigate &&
            command.TargetRouteId == AppNavigationRouteIds.ImportIcons &&
            command.IsEnabled);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "diagnostics" &&
            command.TargetRouteId == AppNavigationRouteIds.Diagnostics);
    }

    [Fact]
    public void GetCommandsForDiagnosticsReflectsBlockingCount()
    {
        var window = CreateHomeWindow(new WinUiToolingSnapshot(
            IsChecked: true,
            WinUiTemplatesAvailable: true,
            WinAppAvailable: false,
            "WinUI templates are available.",
            "winapp CLI is missing."));

        var commands = new AppCommandService().GetCommands(window, AppNavigationRouteIds.Diagnostics);

        Assert.True(commands.Succeeded, commands.Error.Message);
        Assert.NotNull(commands.Value);
        Assert.Equal(AppNavigationRouteIds.Diagnostics, commands.Value.RouteId);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "review-blockers" &&
            command.IsEnabled &&
            command.Detail.Contains("1 blocking issue", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "open-app-data" &&
            command.Kind == AppCommandKind.OpenLocation &&
            command.LocationKind == AppLocationKind.AppData);
    }

    [Fact]
    public void GetCommandsForChangeIconDisablesApplyUntilIconSelected()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        Directory.CreateDirectory(folder);
        var window = new AppWindowService().GetWindow(
            [
                "change-icon",
                "--target",
                folder,
                "--target-kind",
                "folder"
            ],
            paths,
            ReadyTooling()).Value!;

        var commands = new AppCommandService().GetCommands(window);

        Assert.True(commands.Succeeded, commands.Error.Message);
        Assert.NotNull(commands.Value);
        Assert.Equal(AppNavigationRouteIds.ChangeIcon, commands.Value.RouteId);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "choose-icon" &&
            command.IsEnabled);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "apply-change" &&
            !command.IsEnabled);
    }

    [Fact]
    public void GetCommandsForChangeIconWorkflowWithSelectedIconEnablesPreviewAndApply()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        var icon = temp.PathFor("icons", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(icon);
        string[] activationArgs =
        [
            "change-icon",
            "--target",
            folder,
            "--target-kind",
            "folder"
        ];
        var window = new AppWindowService().GetWindow(activationArgs, paths, ReadyTooling()).Value!;
        var workflow = new AppChangeIconWorkflowService().GetWorkflow(activationArgs, paths, icon).Value!;

        var commands = new AppCommandService().GetCommands(window, changeIconWorkflow: workflow);

        Assert.True(commands.Succeeded, commands.Error.Message);
        Assert.NotNull(commands.Value);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "preview-change" &&
            command.IsEnabled);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "apply-change" &&
            command.IsEnabled);
    }

    [Fact]
    public void GetCommandsForUnsupportedChangeIconDisablesChooseIconWithReason()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var file = temp.PathFor("notes.txt");
        File.WriteAllText(file, "not a shortcut");
        var window = new AppWindowService().GetWindow(
            [
                "change-icon",
                "--target",
                file,
                "--target-kind",
                "shortcut"
            ],
            paths,
            ReadyTooling()).Value!;

        var commands = new AppCommandService().GetCommands(window);

        Assert.True(commands.Succeeded, commands.Error.Message);
        Assert.NotNull(commands.Value);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "choose-icon" &&
            !command.IsEnabled &&
            command.Detail.Contains("folders and .lnk shortcuts", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetCommandsForIconDetailsWithSelectedIconEnablesCopyAndOpenActions()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var icon = Path.Combine(paths.LibraryRoot, "Work", "blue.ico");
        var details = new IconDetailsService().GetDetails(icon, paths).Value!;
        var window = new AppWindowService().GetWindow([], paths, ReadyTooling()).Value!;

        var commands = new AppCommandService().GetCommands(
            window,
            AppNavigationRouteIds.IconDetails,
            iconDetails: details);

        Assert.True(commands.Succeeded, commands.Error.Message);
        Assert.NotNull(commands.Value);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "copy-icon-path" &&
            command.IsEnabled &&
            command.Detail.Contains(icon, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "open-containing-folder" &&
            command.IsEnabled);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "use-selected-icon" &&
            !command.IsEnabled);
    }

    [Fact]
    public void GetCommandsForShellPlanLinksToShellBridge()
    {
        var window = CreateHomeWindow();

        var commands = new AppCommandService().GetCommands(window, AppNavigationRouteIds.ShellPlan);

        Assert.True(commands.Succeeded, commands.Error.Message);
        Assert.NotNull(commands.Value);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "review-shell-bridge" &&
            command.Kind == AppCommandKind.Navigate &&
            command.TargetRouteId == AppNavigationRouteIds.ShellBridge &&
            command.IsEnabled);
    }

    [Fact]
    public void GetCommandsForShellBridgeIncludesReviewAndNavigation()
    {
        var window = CreateHomeWindow();

        var commands = new AppCommandService().GetCommands(window, AppNavigationRouteIds.ShellBridge);

        Assert.True(commands.Succeeded, commands.Error.Message);
        Assert.NotNull(commands.Value);
        Assert.Equal(AppNavigationRouteIds.ShellBridge, commands.Value.RouteId);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "review-resolved-commands" &&
            command.Kind == AppCommandKind.Workflow &&
            command.IsEnabled);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "review-shell-plan" &&
            command.TargetRouteId == AppNavigationRouteIds.ShellPlan);
    }

    [Fact]
    public void GetCommandsForReadyRestoreWorkflowEnablesConfirmRestore()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var record = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Applied);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);
        var workflow = new AppRestoreWorkflowService().GetWorkflow(paths, record.Id).Value!;
        var window = new AppWindowService().GetWindow([], paths, ReadyTooling()).Value!;

        var commands = new AppCommandService().GetCommands(window, AppNavigationRouteIds.RestorePreview, workflow);

        Assert.True(commands.Succeeded, commands.Error.Message);
        Assert.NotNull(commands.Value);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "confirm-restore" &&
            command.IsEnabled);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "review-disabled-reason" &&
            !command.IsEnabled);
    }

    [Fact]
    public void GetCommandsForBlockedRestoreWorkflowKeepsReasonActionAvailable()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var record = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Restored);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);
        var workflow = new AppRestoreWorkflowService().GetWorkflow(paths, record.Id).Value!;
        var window = new AppWindowService().GetWindow([], paths, ReadyTooling()).Value!;

        var commands = new AppCommandService().GetCommands(window, AppNavigationRouteIds.RestorePreview, workflow);

        Assert.True(commands.Succeeded, commands.Error.Message);
        Assert.NotNull(commands.Value);
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "confirm-restore" &&
            !command.IsEnabled &&
            command.Detail.Contains("not currently applied", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(commands.Value.Commands, command =>
            command.Id == "review-disabled-reason" &&
            command.IsEnabled);
    }

    [Fact]
    public void GetCommandsRejectsUnknownRoute()
    {
        var window = CreateHomeWindow();

        var commands = new AppCommandService().GetCommands(window, "missing");

        Assert.False(commands.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, commands.Error.Code);
    }

    private static AppWindowSnapshot CreateHomeWindow(WinUiToolingSnapshot? tooling = null)
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        return new AppWindowService().GetWindow([], paths, tooling ?? ReadyTooling()).Value!;
    }

    private static IconLibraryPaths CreatePathsWithIcon(TempDirectory temp)
    {
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));
        return paths;
    }

    private static RestoreRecord CreateRecord(
        TempDirectory temp,
        TargetKind targetKind,
        string targetName,
        string iconName,
        RestoreRecordStatus status)
    {
        var targetPath = temp.PathFor("targets", targetName);
        if (targetKind == TargetKind.Folder)
        {
            Directory.CreateDirectory(targetPath);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.WriteAllText(targetPath, "shortcut placeholder");
        }

        var iconPath = temp.PathFor("icons", iconName);
        TestIconFactory.WriteValidIcon(iconPath);

        return RestoreRecord.CreatePending(
            new TargetItem(targetKind, targetPath),
            iconPath,
            targetKind == TargetKind.Folder
                ? new FolderRestoreSnapshot(
                    DesktopIniExisted: false,
                    new Dictionary<string, string?>(),
                    FileAttributes.Directory,
                    null)
                : new ShortcutRestoreSnapshot(null, 0)) with { Status = status };
    }

    private static WinUiToolingSnapshot ReadyTooling()
    {
        return new WinUiToolingSnapshot(
            IsChecked: true,
            WinUiTemplatesAvailable: true,
            WinAppAvailable: true,
            "WinUI templates are available.",
            "winapp CLI is available.");
    }
}
