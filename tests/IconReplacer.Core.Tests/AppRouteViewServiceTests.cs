using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppRouteViewServiceTests
{
    [Fact]
    public void GetViewForHomeComposesHomeContentAndCommands()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);

        var view = new AppRouteViewService().GetView([], paths, winUiTooling: ReadyTooling());

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppNavigationRouteIds.Home, view.Value.Route.RouteId);
        Assert.Equal(AppRouteContentKind.Home, view.Value.ContentKind);
        Assert.True(view.Value.IsContentReady);
        Assert.NotNull(view.Value.Home);
        Assert.Equal(4, view.Value.Commands.CommandCount);
    }

    [Fact]
    public void GetViewForIconBrowserAppliesSearchAndCategoryOptions()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "green.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Games", "blue-game.ico"));

        var view = new AppRouteViewService().GetView(
            [],
            paths,
            AppNavigationRouteIds.IconBrowser,
            ReadyTooling(),
            browserOptions: new IconBrowserOptions("blue", "Work", 5));

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.IconBrowser, view.Value.ContentKind);
        Assert.NotNull(view.Value.Browser);
        Assert.Equal("blue", view.Value.Browser.SearchText);
        Assert.Equal("Work", view.Value.Browser.CategoryName);
        Assert.Equal(3, view.Value.Browser.TotalIconCount);
        Assert.Equal(1, view.Value.Browser.MatchedIconCount);
        var item = Assert.Single(view.Value.Browser.Items);
        Assert.Equal("blue", item.DisplayName);
        Assert.Equal("Work", item.CategoryName);
    }

    [Fact]
    public void GetViewForIconBrowserReportsCappedVisibleItems()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "green.ico"));
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "red.ico"));

        var view = new AppRouteViewService().GetView(
            [],
            paths,
            AppNavigationRouteIds.IconBrowser,
            ReadyTooling(),
            browserOptions: new IconBrowserOptions(CategoryName: "Work", MaxItems: 2));

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.IconBrowser, view.Value.ContentKind);
        Assert.NotNull(view.Value.Browser);
        Assert.Equal(3, view.Value.Browser.MatchedIconCount);
        Assert.Equal(2, view.Value.Browser.VisibleIconCount);
        Assert.Equal(1, view.Value.Browser.OmittedIconCount);
        Assert.True(view.Value.Browser.IsTruncated);
        Assert.Contains("2/3", view.Value.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetViewForDiagnosticsReflectsToolingBlockers()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var packagingInputs = PackagingPlanInputs.FromTooling(
            MissingWinAppTooling(),
            MissingNativeTooling());

        var view = new AppRouteViewService().GetView(
            [],
            paths,
            AppNavigationRouteIds.Diagnostics,
            MissingWinAppTooling(),
            packagingInputs: packagingInputs);

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.Diagnostics, view.Value.ContentKind);
        Assert.NotNull(view.Value.Diagnostics);
        Assert.Equal(2, view.Value.Diagnostics.BlockingCount);
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "review-blockers" &&
            command.IsEnabled);
        Assert.Contains(view.Value.Diagnostics.Checks, check =>
            check.Id == "native-build-tools" &&
            check.Status == AppDiagnosticStatus.Blocking);
    }

    [Fact]
    public void GetViewForPackagePlanUsesPackagingInputsWhenProvided()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var packagingInputs = PackagingPlanInputs.FromTooling(
            MissingWinAppTooling(),
            MissingNativeTooling());

        var view = new AppRouteViewService().GetView(
            [],
            paths,
            AppNavigationRouteIds.PackagePlan,
            MissingWinAppTooling(),
            packagingInputs: packagingInputs);

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.PackagePlan, view.Value.ContentKind);
        Assert.NotNull(view.Value.PackagePlan);
        Assert.Equal(6, view.Value.PackagePlan.BlockingCount);
        Assert.Contains("6 blockers", view.Value.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(view.Value.PackagePlan.Items, item =>
            item.Id == "native-build-tools" &&
            item.Status == AppDiagnosticStatus.Blocking);
    }

    [Fact]
    public void GetViewForShellBridgeComposesSupportedTargetWithoutMutation()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var folder = temp.PathFor("Project");
        Directory.CreateDirectory(folder);

        var view = new AppRouteViewService().GetView(
            [],
            paths,
            AppNavigationRouteIds.ShellBridge,
            ReadyTooling(),
            shellTargetPath: folder);

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.ShellBridge, view.Value.ContentKind);
        Assert.True(view.Value.IsContentReady);
        Assert.NotNull(view.Value.ShellBridge);
        Assert.Equal(ShellSelectionStatus.Supported, view.Value.ShellBridge.Selection.Status);
        Assert.True(view.Value.ShellBridge.HasSupportedTarget);
        Assert.Equal(2, view.Value.ShellBridge.CommandCount);
        Assert.Equal(2, view.Value.ShellBridge.InvocableCommandCount);
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "review-resolved-commands" &&
            command.IsEnabled);
        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void GetViewForCollectionsComposesCollectionContentAndCommands()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        Directory.CreateDirectory(Path.Combine(paths.LibraryRoot, "Empty"));

        var view = new AppRouteViewService().GetView(
            [],
            paths,
            AppNavigationRouteIds.Collections,
            ReadyTooling());

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.Collections, view.Value.ContentKind);
        Assert.True(view.Value.IsContentReady);
        Assert.NotNull(view.Value.Collections);
        Assert.Contains(view.Value.Collections, collection =>
            collection.Name == "Work" &&
            collection.IconCount == 1);
        Assert.Contains(view.Value.Collections, collection =>
            collection.Name == "Empty" &&
            collection.IconCount == 0);
        Assert.Contains(view.Value.Collections, collection => collection.IsImportedCollection);
        Assert.Equal(3, view.Value.Commands.CommandCount);
    }

    [Fact]
    public void GetViewForImportIconsComposesPickerRequestAndCommands()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);

        var view = new AppRouteViewService().GetView(
            [],
            paths,
            AppNavigationRouteIds.ImportIcons,
            ReadyTooling());

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.ImportIcons, view.Value.ContentKind);
        Assert.True(view.Value.IsContentReady);
        Assert.NotNull(view.Value.ImportPickerRequest);
        Assert.True(view.Value.ImportPickerRequest.CanOpenPicker);
        Assert.Equal("Imported", view.Value.ImportPickerRequest.DestinationCollectionName);
        Assert.Equal(paths.ImportedRoot, view.Value.ImportPickerRequest.DestinationDirectory);
        Assert.True(view.Value.ImportPickerRequest.AllowMultiple);
        Assert.Contains(".ico", view.Value.ImportPickerRequest.FileExtensions);
        Assert.True(Directory.Exists(paths.ImportedRoot));
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "choose-ico-files" &&
            command.IsEnabled);
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "review-import-results" &&
            !command.IsEnabled);
    }

    [Fact]
    public void GetViewForImportIconsWithCollectionUsesCollectionDestination()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);

        var view = new AppRouteViewService().GetView(
            [],
            paths,
            AppNavigationRouteIds.ImportIcons,
            ReadyTooling(),
            importCollectionName: "Design Tools");

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.ImportIcons, view.Value.ContentKind);
        Assert.NotNull(view.Value.ImportPickerRequest);
        Assert.Equal("Design Tools", view.Value.ImportPickerRequest.DestinationCollectionName);
        Assert.Equal(Path.Combine(paths.LibraryRoot, "Design Tools"), view.Value.ImportPickerRequest.DestinationDirectory);
        Assert.True(Directory.Exists(view.Value.ImportPickerRequest.DestinationDirectory));
    }

    [Fact]
    public void GetViewForIconDetailsComposesSelectedIconDetailsAndCommands()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var icon = Path.Combine(paths.LibraryRoot, "Work", "blue.ico");

        var view = new AppRouteViewService().GetView(
            [],
            paths,
            AppNavigationRouteIds.IconDetails,
            ReadyTooling(),
            selectedIconPath: icon);

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.IconDetails, view.Value.ContentKind);
        Assert.True(view.Value.IsContentReady);
        Assert.NotNull(view.Value.IconDetails);
        Assert.Equal("blue", view.Value.IconDetails.DisplayName);
        Assert.Equal("Work", view.Value.IconDetails.CategoryName);
        Assert.True(view.Value.IconDetails.IsInIconLibrary);
        Assert.True(view.Value.IconDetails.ImageCount > 0);
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "copy-icon-path" &&
            command.IsEnabled);
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "use-selected-icon" &&
            !command.IsEnabled);
    }

    [Fact]
    public void GetViewForIconDetailsWithoutSelectedIconReturnsPlaceholder()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);

        var view = new AppRouteViewService().GetView(
            [],
            paths,
            AppNavigationRouteIds.IconDetails,
            ReadyTooling());

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.WorkflowPlaceholder, view.Value.ContentKind);
        Assert.False(view.Value.IsContentReady);
        Assert.Null(view.Value.IconDetails);
        Assert.Contains("Select an icon", view.Value.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetViewForChangeIconComposesNeedIconWorkflow()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var folder = temp.PathFor("Project");
        Directory.CreateDirectory(folder);

        var view = new AppRouteViewService().GetView(
            [
                "change-icon",
                "--target",
                folder,
                "--target-kind",
                "folder"
            ],
            paths,
            winUiTooling: ReadyTooling());

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppNavigationRouteIds.ChangeIcon, view.Value.Route.RouteId);
        Assert.Equal(AppRouteContentKind.ChangeIconWorkflow, view.Value.ContentKind);
        Assert.True(view.Value.IsContentReady);
        Assert.NotNull(view.Value.ChangeIconWorkflow);
        Assert.Equal(AppChangeIconWorkflowStep.NeedIcon, view.Value.ChangeIconWorkflow.Step);
        Assert.True(view.Value.ChangeIconWorkflow.CanOpenPicker);
        Assert.False(view.Value.ChangeIconWorkflow.CanApply);
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "choose-icon" &&
            command.IsEnabled);
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "apply-change" &&
            !command.IsEnabled);
        Assert.Equal(3, view.Value.Commands.CommandCount);
    }

    [Fact]
    public void GetViewForChangeIconWithSelectedIconEnablesApplyWithoutMutation()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var folder = temp.PathFor("Project");
        var icon = temp.PathFor("icons", "green.ico");
        Directory.CreateDirectory(folder);
        TestIconFactory.WriteValidIcon(icon);

        var view = new AppRouteViewService().GetView(
            [
                "change-icon",
                "--target",
                folder,
                "--target-kind",
                "folder"
            ],
            paths,
            winUiTooling: ReadyTooling(),
            selectedIconPath: icon);

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.ChangeIconWorkflow, view.Value.ContentKind);
        Assert.True(view.Value.IsContentReady);
        Assert.NotNull(view.Value.ChangeIconWorkflow);
        Assert.Equal(AppChangeIconWorkflowStep.ReadyToApply, view.Value.ChangeIconWorkflow.Step);
        Assert.True(view.Value.ChangeIconWorkflow.CanPreview);
        Assert.True(view.Value.ChangeIconWorkflow.CanApply);
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "preview-change" &&
            command.IsEnabled);
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "apply-change" &&
            command.IsEnabled);
        Assert.False(File.Exists(Path.Combine(folder, "desktop.ini")));
        Assert.False(File.Exists(paths.RestoreStateFile));
    }

    [Fact]
    public void GetViewForRestorePreviewWithoutRecordComposesNeedRecordWorkflow()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var record = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Applied);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);

        var view = new AppRouteViewService().GetView(
            [],
            paths,
            AppNavigationRouteIds.RestorePreview,
            ReadyTooling());

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.RestoreWorkflow, view.Value.ContentKind);
        Assert.True(view.Value.IsContentReady);
        Assert.NotNull(view.Value.RestoreWorkflow);
        Assert.Equal(AppRestoreWorkflowStep.NeedRecord, view.Value.RestoreWorkflow.Step);
        Assert.Single(view.Value.RestoreWorkflow.History.Records);
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "confirm-restore" &&
            !command.IsEnabled);
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "cancel-restore" &&
            command.IsEnabled);
    }

    [Fact]
    public void GetViewForRestorePreviewWithAppliedRecordEnablesConfirmRestore()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);
        var record = CreateRecord(temp, TargetKind.Folder, "folder", "icon.ico", RestoreRecordStatus.Applied);
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);

        var view = new AppRouteViewService().GetView(
            [],
            paths,
            AppNavigationRouteIds.RestorePreview,
            ReadyTooling(),
            record.Id);

        Assert.True(view.Succeeded, view.Error.Message);
        Assert.NotNull(view.Value);
        Assert.Equal(AppRouteContentKind.RestoreWorkflow, view.Value.ContentKind);
        Assert.True(view.Value.IsContentReady);
        Assert.NotNull(view.Value.RestoreWorkflow);
        Assert.Equal(AppRestoreWorkflowStep.ReadyToRestore, view.Value.RestoreWorkflow.Step);
        Assert.True(view.Value.RestoreWorkflow.CanRestore);
        Assert.Contains(view.Value.Commands.Commands, command =>
            command.Id == "confirm-restore" &&
            command.IsEnabled);
    }

    [Fact]
    public void GetViewRejectsUnknownRoute()
    {
        using var temp = new TempDirectory();
        var paths = CreatePathsWithIcon(temp);

        var view = new AppRouteViewService().GetView([], paths, "missing", ReadyTooling());

        Assert.False(view.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, view.Error.Code);
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

    private static WinUiToolingSnapshot MissingWinAppTooling()
    {
        return new WinUiToolingSnapshot(
            IsChecked: true,
            WinUiTemplatesAvailable: true,
            WinAppAvailable: false,
            "WinUI templates are available.",
            "winapp CLI is missing.");
    }

    private static NativeToolingSnapshot MissingNativeTooling()
    {
        return new NativeToolingSnapshot(
            IsChecked: true,
            CompilerAvailable: false,
            MsBuildAvailable: false,
            CMakeAvailable: true,
            "cl.exe is missing.",
            "Visual Studio MSBuild is missing.",
            "CMake is available.");
    }
}
