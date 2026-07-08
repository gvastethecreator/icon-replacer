using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class ShellExtensionBridgeServiceTests
{
    [Fact]
    public void BuildBridgeForSupportedFolderResolvesChangeAndIconCommands()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var folder = temp.PathFor("Project");
        var icon = Path.Combine(paths.LibraryRoot, "Work", "blue.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(icon);

        var bridge = new ShellExtensionBridgeService().BuildBridge(paths, folder);

        Assert.True(bridge.Succeeded, bridge.Error.Message);
        Assert.NotNull(bridge.Value);
        Assert.Equal(ShellExtensionBridgeService.ProtocolVersion, bridge.Value.ProtocolVersion);
        Assert.Equal(ShellManifestContractService.ExplorerCommandClsid, bridge.Value.ExplorerCommandClsid);
        Assert.Equal(ShellManifestContractService.ShellExtensionDllPath, bridge.Value.ShellExtensionDllPath);
        Assert.Contains("IExplorerCommand", bridge.Value.RequiredInterfaces);
        Assert.Contains(bridge.Value.Targets, target => target.ItemType == "Directory");
        Assert.Contains(bridge.Value.Targets, target => target.ItemType == ".lnk");
        Assert.Equal(ShellSelectionStatus.Supported, bridge.Value.Selection.Status);
        Assert.Equal(2, bridge.Value.CommandCount);
        Assert.Equal(2, bridge.Value.InvocableCommandCount);

        var changeIcon = bridge.Value.Commands.Single(command => command.Kind == IconMenuCommandKind.ChangeIcon);
        Assert.True(changeIcon.CanInvoke);
        Assert.Equal(["change-icon", "--target", folder, "--target-kind", "folder"], changeIcon.ResolvedArguments);

        var applyIcon = bridge.Value.Commands.Single(command => command.Kind == IconMenuCommandKind.ApplyLibraryIcon);
        Assert.True(applyIcon.CanInvoke);
        Assert.Equal(["menu-apply", folder, icon], applyIcon.ResolvedArguments);
        Assert.Equal("Work", applyIcon.CategoryName);

        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void BuildBridgeDisablesTargetCommandsForUnsupportedTargetButKeepsOpenApp()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var unsupported = temp.PathFor("notes.txt");
        File.WriteAllText(unsupported, "not a shortcut");

        var bridge = new ShellExtensionBridgeService().BuildBridge(paths, unsupported);

        Assert.True(bridge.Succeeded, bridge.Error.Message);
        Assert.NotNull(bridge.Value);
        Assert.Equal(IconMenuState.Empty, bridge.Value.MenuState);
        Assert.Equal(ShellSelectionStatus.UnsupportedTarget, bridge.Value.Selection.Status);
        Assert.Equal(2, bridge.Value.CommandCount);
        Assert.Equal(1, bridge.Value.InvocableCommandCount);

        var changeIcon = bridge.Value.Commands.Single(command => command.Kind == IconMenuCommandKind.ChangeIcon);
        Assert.False(changeIcon.CanInvoke);
        Assert.Equal(ErrorCode.UnsupportedTarget, changeIcon.Error.Code);
        Assert.Empty(changeIcon.ResolvedArguments);

        var openApp = bridge.Value.Commands.Single(command => command.Kind == IconMenuCommandKind.OpenApp);
        Assert.True(openApp.CanInvoke);
        Assert.Empty(openApp.ResolvedArguments);
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void BuildBridgeReportsMenuCapsAndLeavesTargetCommandsDisabledWithoutSelection()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "one.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "two.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Apps", "three.ico"));

        var bridge = new ShellExtensionBridgeService().BuildBridge(
            paths,
            targetPath: null,
            new IconMenuOptions(
                MaxRootIconItems: 0,
                MaxCategoryCount: 1,
                MaxIconItemsPerCategory: 1));

        Assert.True(bridge.Succeeded, bridge.Error.Message);
        Assert.NotNull(bridge.Value);
        Assert.Equal(IconMenuState.Truncated, bridge.Value.MenuState);
        Assert.Equal(ShellSelectionStatus.NoSelection, bridge.Value.Selection.Status);
        Assert.Equal(3, bridge.Value.TotalIconCount);
        Assert.Equal(1, bridge.Value.VisibleIconCommandCount);
        Assert.Equal(2, bridge.Value.OmittedIconCount);
        Assert.Contains(bridge.Value.Commands, command => command.Kind == IconMenuCommandKind.OpenApp && command.CanInvoke);
        Assert.All(
            bridge.Value.Commands.Where(command => command.RequiresTarget),
            command => Assert.False(command.CanInvoke));
        Assert.Equal(1, bridge.Value.InvocableCommandCount);
    }

    [Fact]
    public void BuildBridgeReportsUnavailableMenuWithoutThrowing()
    {
        using var temp = new TempDirectory();
        var userRoot = temp.PathFor("user");
        Directory.CreateDirectory(userRoot);
        File.WriteAllText(Path.Combine(userRoot, IconLibraryPaths.LibraryFolderName), "not a directory");
        var paths = IconLibraryPaths.FromRoots(userRoot, temp.PathFor("appdata")).Value!;

        var bridge = new ShellExtensionBridgeService().BuildBridge(paths);

        Assert.True(bridge.Succeeded, bridge.Error.Message);
        Assert.NotNull(bridge.Value);
        Assert.Equal(IconMenuState.Unavailable, bridge.Value.MenuState);
        Assert.False(bridge.Value.CanShowContextMenu);
        Assert.NotEqual(ErrorCode.None, bridge.Value.Error.Code);
        Assert.Contains(bridge.Value.Commands, command => command.Kind == IconMenuCommandKind.ChangeIcon);
        Assert.Contains(bridge.Value.Commands, command => command.Kind == IconMenuCommandKind.OpenApp && command.CanInvoke);
    }
}
