using IconReplacer.AppModel;
using IconReplacer.Core;

namespace IconReplacer.Core.Tests;

public sealed class AppActionRequestServiceTests
{
    [Fact]
    public void CreateRequestMapsImportIconsWhenLibraryIsEmpty()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var request = new AppActionRequestService().CreateRequest(SetupActionIds.ImportIcons, paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanExecute);
        Assert.Equal(AppActionKind.ImportIcons, request.Value.Kind);
        Assert.Equal("import-icons", request.Value.NavigationTarget);
        Assert.Null(request.Value.HistoryFilter);
    }

    [Fact]
    public void CreateRequestMapsMissingTargetsToStaleHistory()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        var record = RestoreRecord.CreatePending(
            new TargetItem(TargetKind.Folder, temp.PathFor("missing-folder")),
            temp.PathFor("icons", "missing.ico"),
            new FolderRestoreSnapshot(
                DesktopIniExisted: false,
                new Dictionary<string, string?>(),
                FileAttributes.Directory,
                null)) with { Status = RestoreRecordStatus.Applied };
        Assert.True(new RestoreRecordStore(paths.RestoreStateFile).Upsert(record).Succeeded);

        var request = new AppActionRequestService().CreateRequest(SetupActionIds.ReviewMissingTargets, paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanExecute);
        Assert.Equal(AppActionKind.ShowRestoreHistory, request.Value.Kind);
        Assert.Equal("history", request.Value.NavigationTarget);
        Assert.Equal(RestoreHistoryFilter.Stale, request.Value.HistoryFilter);
    }

    [Fact]
    public void CreateRequestMapsConfigureShellIntegrationToShellPlan()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var request = new AppActionRequestService().CreateRequest(SetupActionIds.ConfigureShellIntegration, paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanExecute);
        Assert.Equal(AppActionKind.ShowShellIntegrationPlan, request.Value.Kind);
        Assert.Equal("shell-plan", request.Value.NavigationTarget);
    }

    [Fact]
    public void CreateRequestMapsReviewPackagePlanToPackagePlanRoute()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var request = new AppActionRequestService().CreateRequest(
            SetupActionIds.ReviewPackagePlan,
            paths,
            PackagingPlanInputs.FromTooling(MissingWinAppTooling()));

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.True(request.Value.CanExecute);
        Assert.Equal(AppActionKind.ShowPackagePlan, request.Value.Kind);
        Assert.Equal("package-plan", request.Value.NavigationTarget);
    }


    [Fact]
    public void CreateRequestDisablesKnownActionThatIsNotCurrentlyAvailable()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;
        TestIconFactory.WriteValidIcon(Path.Combine(paths.LibraryRoot, "Work", "blue.ico"));

        var request = new AppActionRequestService().CreateRequest(SetupActionIds.ImportIcons, paths);

        Assert.True(request.Succeeded, request.Error.Message);
        Assert.NotNull(request.Value);
        Assert.False(request.Value.CanExecute);
        Assert.Equal(AppActionKind.ImportIcons, request.Value.Kind);
        Assert.Equal(ErrorCode.InvalidArgument, request.Value.Error.Code);
    }

    [Fact]
    public void CreateRequestRejectsUnknownAction()
    {
        using var temp = new TempDirectory();
        var paths = IconLibraryPaths.FromRoots(temp.PathFor("user"), temp.PathFor("appdata")).Value!;

        var request = new AppActionRequestService().CreateRequest("not-real", paths);

        Assert.False(request.Succeeded);
        Assert.Equal(ErrorCode.InvalidArgument, request.Error.Code);
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
}
